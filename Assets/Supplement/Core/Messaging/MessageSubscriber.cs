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

        // ハンドラーを呼ぶ。tokenの取り消し(購読の解除・Switchでの切り替え)で終わった場合は、正常に終わったものとして扱う。
        // ハンドラーが同期的に完了した場合はasyncメソッドを通さない。エディタ(Debugビルド)ではasyncメソッドの
        // ステートマシンがクラスになり、同期的に完了しても呼ぶたびに確保されるため。
        protected UniTask RunAsync(T message, CancellationToken token)
        {
            UniTask task;
            try
            {
                task = handler(message, token);
            }
            catch (Exception e)
            {
                return FromError(e, token);
            }

            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                return AwaitAsync(task, token);
            }

            try
            {
                awaiter.GetResult();
                return UniTask.CompletedTask;
            }
            catch (Exception e)
            {
                return FromError(e, token);
            }
        }

        private static UniTask FromError(Exception exception, CancellationToken token)
        {
            return exception is OperationCanceledException && token.IsCancellationRequested
                ? UniTask.CompletedTask
                : UniTask.FromException(exception);
        }

        private static async UniTask AwaitAsync(UniTask task, CancellationToken token)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
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
            RunAsync(message, LifetimeToken).Forget();
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

            var task = RunAsync(message, LifetimeToken);
            // 同期的に完了した場合はasyncメソッドを通さない(RunAsyncと同じ理由)。
            if (!task.Status.IsCompleted())
            {
                ReleaseAfterAsync(task).Forget();
                return;
            }

            Volatile.Write(ref running, 0);
            // 例外で完了していれば、ほかのストラテジーと同じくUniTaskScheduler.UnobservedTaskExceptionに流す。
            task.Forget();
        }

        private async UniTask ReleaseAfterAsync(UniTask task)
        {
            try
            {
                await task;
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
            RunAndReleaseAsync(message, source).Forget();
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

        private async UniTask RunAndReleaseAsync(T message, CancellationTokenSource source)
        {
            try
            {
                await RunAsync(message, source.Token);
            }
            finally
            {
                lock (gate)
                {
                    if (current == source) current = null;
                }
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
        // 同期的に完了する間はこのループで回し、asyncメソッドは処理が終わっていないときだけ通す(RunAsyncと同じ理由)。
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

                var task = RunAsync(message, LifetimeToken);
                if (!task.Status.IsCompleted())
                {
                    // 完了を待ってから続きを処理する。runningは立てたままなので、その間に届いたメッセージはキューに溜まる。
                    ContinuePumpAsync(task).Forget();
                    return;
                }

                // 例外で完了していれば、ほかのストラテジーと同じくUniTaskScheduler.UnobservedTaskExceptionに流す。
                task.Forget();
            }
        }

        private async UniTaskVoid ContinuePumpAsync(UniTask task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                // ほかのストラテジーのForgetと同じく、UniTaskScheduler.UnobservedTaskExceptionに流す。
                UniTask.FromException(e).Forget();
            }
            Pump();
        }
    }
}
