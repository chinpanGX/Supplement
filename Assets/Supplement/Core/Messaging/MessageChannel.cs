using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    // MessageBrokerが型を知らずにチャンネルを片付けるためのインターフェース。
    internal interface IMessageChannel
    {
        // 購読をすべて解除する。チャンネルは引き続き使える。
        void Clear();

        // 購読をすべて解除し、以後の購読を受け付けない。
        void Close();
    }

    // 1つのメッセージの型の購読者を持ち、発行を届ける。
    // 購読者は、購読者自身をノードにした双方向リストで持つ。登録・解除はlockの中で前後をつなぎ替えるだけで、
    // 購読者の数によらず定数時間で、購読者1つのほかに確保もしない。
    // 発行はlockを取らずにリストを辿る。開始時の版より新しいノード(発行中に登録されたもの)に着いたら止め、
    // 解除済みのノードは飛ばす。解除したノードも次のノードへの参照を残すので、辿っている途中で解除されても先へ進める。
    internal sealed class MessageChannel<T> : IMessageChannel where T : struct
    {
        private readonly object gate = new object();
        private MessageSubscriber<T> root;
        // 最後に登録した購読者の版。64bitの読み書きを不可分にするため、Volatileで読み書きする。
        private long version;
        // つながっている購読者の数。PublishAsyncで、タスクを入れる配列を一度で足りる大きさで借りるために使う。
        private int count;
        private bool closed;

        public IDisposable Add(MessageSubscriber<T> subscriber)
        {
            lock (gate)
            {
                if (closed) throw new ObjectDisposedException(nameof(MessageBroker));

                var newVersion = version + 1;
                subscriber.Version = newVersion;
                subscriber.IsLinked = true;
                Volatile.Write(ref count, count + 1);

                // 版を進めてからつなぐ。発行が新しいノードを見つけたときには、必ずその版より古い版を控えている。
                Volatile.Write(ref version, newVersion);
                if (root == null)
                {
                    subscriber.Previous = subscriber;
                    Volatile.Write(ref root, subscriber);
                }
                else
                {
                    var last = root.Previous;
                    subscriber.Previous = last;
                    root.Previous = subscriber;
                    Volatile.Write(ref last.Next, subscriber);
                }
            }
            return subscriber;
        }

        public void Remove(MessageSubscriber<T> subscriber)
        {
            lock (gate)
            {
                if (!subscriber.IsLinked)
                {
                    return;
                }
                subscriber.IsLinked = false;
                Volatile.Write(ref count, count - 1);

                var next = subscriber.Next;
                if (subscriber == root)
                {
                    if (next != null)
                    {
                        next.Previous = subscriber.Previous;
                    }
                    Volatile.Write(ref root, next);
                }
                else
                {
                    var previous = subscriber.Previous;
                    Volatile.Write(ref previous.Next, next);
                    if (next != null)
                    {
                        next.Previous = previous;
                    }
                    else
                    {
                        // 末尾を外したので、先頭が指す末尾を1つ前に戻す。
                        root.Previous = previous;
                    }
                }

                // Nextは辿っている途中の発行のために残す。Previousは発行で使わないので、前のノードを生かし続けないよう外す。
                subscriber.Previous = null;
            }
        }

        public void Clear()
        {
            MessageSubscriber<T> removed;
            lock (gate)
            {
                removed = root;
                Volatile.Write(ref root, null);
                Volatile.Write(ref count, 0);
                for (var node = removed; node != null; node = node.Next)
                {
                    node.IsLinked = false;
                    node.Previous = null;
                }
            }

            // リストから外したあとで破棄するので、購読者からのRemoveは何もしない。
            while (removed != null)
            {
                var next = removed.Next;
                removed.Dispose();
                removed = next;
            }
        }

        public void Close()
        {
            lock (gate)
            {
                closed = true;
            }
            Clear();
        }

        public void Publish(T message)
        {
            // 版を先に読む。後から読むと、発行中に登録された購読者に届いてしまうことがある。
            var startVersion = Volatile.Read(ref version);
            List<Exception> exceptions = null;
            for (var node = Volatile.Read(ref root); node != null; node = Volatile.Read(ref node.Next))
            {
                // 登録順に並んでいるので、これより後ろもすべて発行中に登録されたもの。
                if (node.Version > startVersion) break;
                if (node.IsDisposed) continue;

                try
                {
                    node.Invoke(message);
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }

        public UniTask PublishAsync(T message, AsyncPublishStrategy strategy, CancellationToken cancellationToken)
        {
            var startVersion = Volatile.Read(ref version);
            var first = Volatile.Read(ref root);
            if (first == null)
            {
                return UniTask.CompletedTask;
            }

            return strategy switch
            {
                AsyncPublishStrategy.Parallel => PublishParallelAsync(first, startVersion, Volatile.Read(ref count), message,
                    cancellationToken),
                AsyncPublishStrategy.Sequential => PublishSequentialAsync(first, startVersion, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
            };
        }

        private static async UniTask PublishParallelAsync(MessageSubscriber<T> first, long startVersion, int capacity,
            T message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Exception> exceptions = null;
            var canceled = false;
            var count = 0;
            // 数えた後に登録が重なると足りなくなることがあるので、そのときだけ大きくし直す。
            var tasks = ArrayPool<UniTask>.Shared.Rent(Math.Max(capacity, 1));
            try
            {
                // すべて開始してから順に待つ。開始済みなので、待つ順序は完了の順序に影響しない。
                for (var node = first; node != null; node = Volatile.Read(ref node.Next))
                {
                    if (node.Version > startVersion) break;
                    if (node.IsDisposed) continue;

                    if (count == tasks.Length)
                    {
                        var grown = ArrayPool<UniTask>.Shared.Rent(count * 2);
                        Array.Copy(tasks, grown, count);
                        Array.Clear(tasks, 0, count);
                        ArrayPool<UniTask>.Shared.Return(tasks);
                        tasks = grown;
                    }
                    tasks[count++] = InvokeAsync(node, message, cancellationToken);
                }

                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        await tasks[i];
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        canceled = true;
                    }
                    catch (Exception e)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(e);
                    }
                }
            }
            finally
            {
                Array.Clear(tasks, 0, count);
                ArrayPool<UniTask>.Shared.Return(tasks);
            }

            ThrowIfFailed(exceptions, canceled, cancellationToken);
        }

        private static async UniTask PublishSequentialAsync(MessageSubscriber<T> first, long startVersion, T message,
            CancellationToken cancellationToken)
        {
            List<Exception> exceptions = null;
            var canceled = false;
            // 待っている間に解除されたノードも、次のノードへの参照を残しているので先へ進める。
            for (var node = first; node != null; node = Volatile.Read(ref node.Next))
            {
                if (node.Version > startVersion) break;

                if (cancellationToken.IsCancellationRequested)
                {
                    canceled = true;
                    break;
                }

                try
                {
                    await InvokeAsync(node, message, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    canceled = true;
                    break;
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            ThrowIfFailed(exceptions, canceled, cancellationToken);
        }

        // 同期のハンドラーはその場で例外を投げるため、ほかの購読者と同じく完了したタスクの例外として扱う。
        private static UniTask InvokeAsync(MessageSubscriber<T> subscriber, T message, CancellationToken cancellationToken)
        {
            if (subscriber.IsDisposed)
            {
                return UniTask.CompletedTask;
            }

            try
            {
                return subscriber.InvokeAsync(message, cancellationToken);
            }
            catch (Exception e)
            {
                return UniTask.FromException(e);
            }
        }

        private static void ThrowIfFailed(List<Exception> exceptions, bool canceled, CancellationToken cancellationToken)
        {
            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }

            if (canceled)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
