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

        private readonly MessageChannel<T> channel;
        private int disposed;

        protected MessageSubscriber(MessageChannel<T> channel)
        {
            this.channel = channel;
        }

        public bool IsDisposed => Volatile.Read(ref disposed) != 0;

        // Publishから呼ぶ。非同期のハンドラーは開始するだけで、完了を待たない。
        public abstract void Invoke(T message);

        // PublishAsyncから呼ぶ。戻り値はハンドラーが完了すると完了する。
        public abstract UniTask InvokeAsync(T message, CancellationToken cancellationToken);

        // 2回目以降は何もしない。
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;

            channel.Remove(this);
            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
        }
    }

    internal sealed class SyncMessageSubscriber<T> : MessageSubscriber<T> where T : struct
    {
        private readonly Action<T> handler;

        public SyncMessageSubscriber(MessageChannel<T> channel, Action<T> handler) : base(channel)
        {
            this.handler = handler;
        }

        public override void Invoke(T message)
        {
            handler(message);
        }

        public override UniTask InvokeAsync(T message, CancellationToken cancellationToken)
        {
            handler(message);
            return UniTask.CompletedTask;
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

        public override void Invoke(T message)
        {
            InvokeAsync(message, CancellationToken.None).Forget();
        }

        protected override void OnDisposed()
        {
            lifetime.Cancel();
        }

        // ハンドラーを呼ぶ。tokenの取り消し(購読の解除・Switchでの切り替え)で終わった場合は、正常に終わったものとして扱う。
        // ハンドラーが同期的に完了した場合はasyncメソッドを通さない。エディタ(Debugビルド)ではasyncメソッドの
        // ステートマシンがクラスになり、同期的に完了しても呼ぶたびに確保されるため。
        protected UniTask RunAsync(T message, CancellationToken token, CancellationToken publishToken)
        {
            if (publishToken.CanBeCanceled)
            {
                return RunLinkedAsync(message, token, publishToken);
            }

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

        // PublishAsyncのトークンが取り消せる場合。つないだトークンを作るため、どのみち確保が起きる。
        private async UniTask RunLinkedAsync(T message, CancellationToken token, CancellationToken publishToken)
        {
            publishToken.ThrowIfCancellationRequested();
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, publishToken);
                await handler(message, linked.Token);
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

        public override UniTask InvokeAsync(T message, CancellationToken cancellationToken)
        {
            return RunAsync(message, LifetimeToken, cancellationToken);
        }
    }

    internal sealed class DropAsyncMessageSubscriber<T> : AsyncMessageSubscriber<T> where T : struct
    {
        private int running;

        public DropAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override UniTask InvokeAsync(T message, CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                return UniTask.CompletedTask;
            }

            var task = RunAsync(message, LifetimeToken, cancellationToken);
            // 同期的に完了した場合はasyncメソッドを通さない(RunAsyncと同じ理由)。
            // このときのtaskはプールした元を持たないので、呼び出し元がそのまま待ってよい。
            if (!task.Status.IsCompleted()) 
                return ReleaseAfterAsync(task);

            Volatile.Write(ref running, 0);
            return task;
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

        public override UniTask InvokeAsync(T message, CancellationToken cancellationToken)
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource previous;
            lock (gate)
            {
                if (IsDisposed)
                {
                    return UniTask.CompletedTask;
                }
                previous = current;
                current = source;
            }

            previous?.Cancel();
            return RunAndReleaseAsync(message, source, cancellationToken);
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

        private async UniTask RunAndReleaseAsync(T message, CancellationTokenSource source,
            CancellationToken cancellationToken)
        {
            try
            {
                await RunAsync(message, source.Token, cancellationToken);
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
        private readonly Queue<Entry> queue = new Queue<Entry>();
        private bool running;

        public SequentialAsyncMessageSubscriber(MessageChannel<T> channel, Func<T, CancellationToken, UniTask> handler)
            : base(channel, handler)
        {
        }

        public override void Invoke(T message)
        {
            Enqueue(new Entry(message, CancellationToken.None, null));
        }

        public override UniTask InvokeAsync(T message, CancellationToken cancellationToken)
        {
            var pending = new PendingPublish(cancellationToken);
            Enqueue(new Entry(message, cancellationToken, pending));
            return pending.Task;
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            // まだ始まっていない分は呼ばずに完了にする。発行側がPublishAsyncで待ち続けないようにするため。
            while (true)
            {
                Entry entry;
                lock (queue)
                {
                    if (queue.Count == 0) return;
                    entry = queue.Dequeue();
                }
                entry.Pending?.TrySetResult();
            }
        }

        private void Enqueue(Entry entry)
        {
            bool accepted;
            var start = false;
            lock (queue)
            {
                accepted = !IsDisposed;
                if (accepted)
                {
                    queue.Enqueue(entry);
                    start = !running;
                    running = true;
                }
            }

            if (!accepted)
            {
                entry.Pending?.TrySetResult();
                return;
            }

            if (start)
            {
                Pump();
            }
        }

        // 待っているメッセージを1つずつ処理する。処理中に届いたメッセージはEnqueueがキューに足すだけで、ここで続けて処理する。
        // 同期的に完了する間はこのループで回し、asyncメソッドは処理が終わっていないときだけ通す(RunAsyncと同じ理由)。
        private void Pump()
        {
            while (true)
            {
                Entry entry;
                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        running = false;
                        return;
                    }
                    entry = queue.Dequeue();
                }

                if (IsDisposed)
                {
                    entry.Pending?.TrySetResult();
                    continue;
                }

                // キューで待っている間にPublishAsyncのトークンが取り消され、もう完了にした分は呼ばない。
                if (entry.Pending is { IsCompleted: true })
                {
                    continue;
                }

                var task = RunAsync(entry.Message, LifetimeToken, entry.PublishToken);
                var awaiter = task.GetAwaiter();
                if (!awaiter.IsCompleted)
                {
                    // 完了を待ってから続きを処理する。runningは立てたままなので、その間に届いたメッセージはキューに溜まる。
                    ContinuePumpAsync(task, entry).Forget();
                    return;
                }

                Exception error = null;
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception e)
                {
                    error = e;
                }
                Complete(entry, error);
            }
        }

        private async UniTaskVoid ContinuePumpAsync(UniTask task, Entry entry)
        {
            Exception error = null;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                error = e;
            }
            Complete(entry, error);
            Pump();
        }

        private static void Complete(Entry entry, Exception error)
        {
            if (entry.Pending == null)
            {
                if (error != null)
                {
                    // ほかの戦略のForgetと同じく、UniTaskScheduler.UnobservedTaskExceptionに流す。
                    UniTask.FromException(error).Forget();
                }
                return;
            }

            if (error == null)
            {
                entry.Pending.TrySetResult();
            }
            else if (error is OperationCanceledException canceled)
            {
                entry.Pending.TrySetCanceled(canceled.CancellationToken);
            }
            else
            {
                entry.Pending.TrySetException(error);
            }
        }

        private readonly struct Entry
        {
            public readonly T Message;
            public readonly CancellationToken PublishToken;
            // PublishAsyncで待っている場合だけ持つ。Publishから届いたメッセージはnull。
            public readonly PendingPublish Pending;

            public Entry(T message, CancellationToken publishToken, PendingPublish pending)
            {
                Message = message;
                PublishToken = publishToken;
                Pending = pending;
            }
        }

        // PublishAsyncで待っている発行。トークンが取り消されたら、キューで待っている間でもその場で取り消しにする。
        // 取り消し・処理の完了・購読の解除が重なって何度完了にしても安全なよう、プールしない完了ソースを使う。
        private sealed class PendingPublish
        {
            private readonly UniTaskCompletionSource source = new UniTaskCompletionSource();
            private readonly CancellationToken token;
            private readonly CancellationTokenRegistration registration;

            public PendingPublish(CancellationToken token)
            {
                this.token = token;
                if (token.CanBeCanceled)
                {
                    registration = token.Register(static state =>
                    {
                        var self = (PendingPublish)state;
                        self.source.TrySetCanceled(self.token);
                    }, this);
                }
            }

            public UniTask Task => source.Task;

            public bool IsCompleted => source.Task.Status.IsCompleted();

            public void TrySetResult()
            {
                source.TrySetResult();
                registration.Dispose();
            }

            public void TrySetCanceled(CancellationToken cancellationToken)
            {
                source.TrySetCanceled(cancellationToken);
                registration.Dispose();
            }

            public void TrySetException(Exception exception)
            {
                source.TrySetException(exception);
                registration.Dispose();
            }
        }
    }
}
