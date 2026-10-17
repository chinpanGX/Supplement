using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    // 購読者。チャンネルの双方向リストのノードと、購読を解除するIDisposableを兼ね、購読のたびの確保を1つにする。
    internal abstract class MessageSubscriber<T> : IDisposable where T : struct
    {
        // 以下のノードの情報は、MessageChannelがgateのlockの中で書き換える。
        // 次のノード。発行はlockを取らずにこれを辿る。リストから外しても、辿っている途中の発行が先へ進めるよう消さない。
        internal MessageSubscriber<T> Next;
        // 前のノード。先頭のノードでは末尾のノードを指す(末尾への追加を定数時間にするため)。発行では使わない。
        internal MessageSubscriber<T> Previous;
        // 追加したときのチャンネルの版。発行は開始時の版より新しいノードに届けない。
        internal long Version;
        // リストにつながっているか。解除が2回走ったり、Clearの後に解除が走ったりしても、つなぎ替えを1回にするため。
        internal bool IsLinked;
        // 同期の購読者のハンドラー。非同期の購読者ではnull。発行が仮想呼び出しを通さずに直接呼ぶために持つ。
        internal readonly Action<T> SyncHandler;

        private readonly MessageChannel<T> channel;
        // 解除済みか。MessageChannelがgateのlockの中で立てる。発行はlockを取らずに読む。
        private bool disposed;

        protected MessageSubscriber(MessageChannel<T> channel, Action<T> syncHandler = null)
        {
            this.channel = channel;
            SyncHandler = syncHandler;
        }

        public bool IsDisposed => Volatile.Read(ref disposed);

        // Publishから呼ぶ。非同期のハンドラーは開始するだけで、完了を待たない。
        public abstract void Invoke(T message);

        // 2回目以降は何もしない。解除済みの印はチャンネルのlockの中で立てる。
        // 解除はどのみちlockを取るので、Interlockedで別に印を立てるより、アトミックな操作が1回少なくて済む。
        public void Dispose()
        {
            if (IsDisposed || !channel.Remove(this)) return;

            OnDisposed();
        }

        // MessageChannelがgateのlockの中で呼ぶ。すでに解除済みならfalseを返す。
        internal bool MarkDisposed()
        {
            if (disposed) return false;

            Volatile.Write(ref disposed, true);
            return true;
        }

        protected virtual void OnDisposed()
        {
        }
    }

    internal sealed class SyncMessageSubscriber<T> : MessageSubscriber<T> where T : struct
    {
        public SyncMessageSubscriber(MessageChannel<T> channel, Action<T> handler) : base(channel, handler)
        {
        }

        // 発行はSyncHandlerを直接呼ぶので、ここは通らない。
        public override void Invoke(T message)
        {
            SyncHandler(message);
        }
    }


    // 非同期の購読者の基底。ハンドラーが完了していなければ、各ストラテジーのasync UniTaskVoidメソッドで1段だけ待つ。
    // async UniTaskのメソッドで待ってからForgetすると、メッセージごとにプールからの出し入れと継続の登録が段の数だけ増えるため。
    internal abstract class AsyncMessageSubscriber<T> : MessageSubscriber<T> where T : struct
    {
        private readonly Func<T, CancellationToken, UniTask> handler;
        // 購読を解除すると取り消す。タイマーもWaitHandleも使わないため、Disposeは要らない。
        private readonly CancellationTokenSource lifetime = new();

        protected AsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel)
        {
            this.handler = handler;
        }

        protected CancellationToken LifetimeToken => lifetime.Token;

        protected override void OnDisposed()
        {
            lifetime.Cancel();
        }

        // ハンドラーを呼ぶ。同期的に完了した(例外を含む)ら、ここで片付けてtrueを返す。完了していなければ、そのタスクを
        // pendingに入れてfalseを返す。呼び出し側は、pendingを待って例外をReportに渡す。
        // 同期的に完了した場合にasyncメソッドを通さないのは、エディタ(Debugビルド)ではasyncメソッドのステートマシンが
        // クラスになり、同期的に完了しても呼ぶたびに確保されるため。
        protected bool TryRunSynchronously(T message, CancellationToken token, out UniTask pending)
        {
            pending = default;
            UniTask task;
            try
            {
                task = handler(message, token);
            }
            catch (Exception e)
            {
                Report(e, token);
                return true;
            }

            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                pending = task;
                return false;
            }

            try
            {
                awaiter.GetResult();
            }
            catch (Exception e)
            {
                Report(e, token);
            }
            return true;
        }

        // ハンドラーの例外をUniTaskScheduler.UnobservedTaskExceptionに流す。tokenの取り消し(購読の解除・Switchでの切り替え)で
        // 終わった場合のOperationCanceledExceptionは、正常に終わったものとして捨てる。
        // UniTaskScheduler.PublishUnobservedTaskExceptionはinternalなので、例外で完了したUniTaskのForgetを通す(例外のときだけ確保する)。
        protected static void Report(Exception exception, CancellationToken token)
        {
            if (exception is OperationCanceledException && token.IsCancellationRequested) return;

            UniTask.FromException(exception).Forget();
        }

        protected static async UniTaskVoid AwaitAsync(UniTask task, CancellationToken token)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Report(e, token);
            }
        }
    }

    internal sealed class ParallelAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        public ParallelAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            var token = LifetimeToken;
            if (!TryRunSynchronously(message, token, out var pending))
            {
                AwaitAsync(pending, token).Forget();
            }
        }
    }

    internal sealed class DropAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        private int running;

        public DropAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                return;
            }

            var token = LifetimeToken;
            if (TryRunSynchronously(message, token, out var pending))
            {
                Volatile.Write(ref running, 0);
                return;
            }

            ReleaseAfterAsync(pending, token).Forget();
        }

        private async UniTaskVoid ReleaseAfterAsync(UniTask task, CancellationToken token)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Report(e, token);
            }
            finally
            {
                Volatile.Write(ref running, 0);
            }
        }
    }

    internal sealed class SwitchAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        private readonly object gate = new object();
        // 処理中のハンドラーに渡したトークンの元。次のメッセージが届くか、購読を解除すると取り消す。
        // 取り消しが処理の終了と重なっても例外にならないよう、Disposeはしない(タイマーもWaitHandleも使わない)。
        private CancellationTokenSource current;

        public SwitchAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource previous;
            lock (gate)
            {
                if (IsDisposed)
                {
                    return;
                }
                previous = current;
                current = source;
            }

            previous?.Cancel();
            if (TryRunSynchronously(message, source.Token, out var pending))
            {
                Release(source);
                return;
            }

            ReleaseAfterAsync(pending, source).Forget();
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            CancellationTokenSource source;
            lock (gate)
            {
                source = current;
                current = null;
            }
            source?.Cancel();
        }

        private async UniTaskVoid ReleaseAfterAsync(UniTask task, CancellationTokenSource source)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Report(e, source.Token);
            }
            finally
            {
                Release(source);
            }
        }

        // 終わった処理のトークンの元を外す。すでに次のメッセージに切り替わっていれば何もしない。
        private void Release(CancellationTokenSource source)
        {
            lock (gate)
            {
                if (current == source) current = null;
            }
        }
    }

    internal sealed class SequentialAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        // 処理を待っているメッセージ。lockの対象も兼ねる。
        private readonly Queue<T> queue = new Queue<T>();
        private bool running;

        public SequentialAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            bool start;
            lock (queue)
            {
                if (IsDisposed) return;

                queue.Enqueue(message);
                start = !running;
                running = true;
            }

            if (start)
            {
                Pump();
            }
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            // まだ始まっていない分は呼ばずに捨てる。
            lock (queue)
            {
                queue.Clear();
            }
        }

        // 待っているメッセージを1つずつ処理する。処理中に届いたメッセージはInvokeがキューに足すだけで、ここで続けて処理する。
        // 同期的に完了する間はこのループで回し、asyncメソッドは処理が終わっていないときだけ通す。
        private void Pump()
        {
            while (true)
            {
                T message;
                lock (queue)
                {
                    if (queue.Count == 0 || IsDisposed)
                    {
                        queue.Clear();
                        running = false;
                        return;
                    }
                    message = queue.Dequeue();
                }

                var token = LifetimeToken;
                if (!TryRunSynchronously(message, token, out var pending))
                {
                    // 完了を待ってから続きを処理する。runningは立てたままなので、その間に届いたメッセージはキューに溜まる。
                    ContinuePumpAsync(pending, token).Forget();
                    return;
                }
            }
        }

        private async UniTaskVoid ContinuePumpAsync(UniTask task, CancellationToken token)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Report(e, token);
            }
            Pump();
        }
    }

    // Sequentialのキューを1件の枠にしたもの。処理中に届いたメッセージは枠を上書きし、最後の1件だけを後で処理する。
    internal sealed class ThrottleFirstLastAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        private readonly object gate = new object();
        // 処理を待っているメッセージ。hasPendingがtrueのときだけ意味を持つ。
        private T pendingMessage;
        private bool hasPending;
        private bool running;

        public ThrottleFirstLastAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            lock (gate)
            {
                if (IsDisposed) return;

                if (running)
                {
                    pendingMessage = message;
                    hasPending = true;
                    return;
                }
                running = true;
            }

            Pump(message);
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            // まだ始まっていない分は呼ばずに捨てる。
            lock (gate)
            {
                pendingMessage = default;
                hasPending = false;
            }
        }

        // messageを処理し、その間に届いた最後の1件があれば続けて処理する。
        // 同期的に完了する間はこのループで回し、asyncメソッドは処理が終わっていないときだけ通す。
        private void Pump(T message)
        {
            while (true)
            {
                var token = LifetimeToken;
                if (!TryRunSynchronously(message, token, out var pending))
                {
                    // 完了を待ってから続きを処理する。runningは立てたままなので、その間に届いたメッセージは枠を上書きする。
                    ContinuePumpAsync(pending, token).Forget();
                    return;
                }

                if (!TryTakePending(out message)) return;
            }
        }

        // 待っているメッセージを取り出す。無ければ処理中の印を下ろしてfalseを返す。
        private bool TryTakePending(out T message)
        {
            lock (gate)
            {
                if (!hasPending || IsDisposed)
                {
                    pendingMessage = default;
                    hasPending = false;
                    running = false;
                    message = default;
                    return false;
                }

                message = pendingMessage;
                pendingMessage = default;
                hasPending = false;
                return true;
            }
        }

        private async UniTaskVoid ContinuePumpAsync(UniTask task, CancellationToken token)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Report(e, token);
            }

            if (TryTakePending(out var message))
            {
                Pump(message);
            }
        }
    }
}
