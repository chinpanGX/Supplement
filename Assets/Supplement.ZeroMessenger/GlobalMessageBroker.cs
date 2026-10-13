using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using ZeroMessenger;
using AsyncPublishStrategy = Supplement.Core.AsyncPublishStrategy;
using AsyncSubscribeStrategy = Supplement.Core.AsyncSubscribeStrategy;
using ZeroAsyncPublishStrategy = ZeroMessenger.AsyncPublishStrategy;
using ZeroAsyncSubscribeStrategy = ZeroMessenger.AsyncSubscribeStrategy;

namespace Supplement.ZeroMessenger
{
    /// <summary>
    /// ZeroMessengerの<c>MessageBroker&lt;T&gt;.Default</c>で、アプリ全体で共有するメッセージを送受信する。
    /// </summary>
    /// <remarks>
    /// 破棄すると、このインスタンスを通じた購読をすべて解除する。ほかのインスタンスやZeroMessengerを直接使った購読は解除しない。
    /// ハンドラーが投げた例外の扱いは、ZeroMessengerに従う(<see cref="Supplement.Core.MessageBroker"/>のように
    /// <see cref="AggregateException"/>にまとめはしない)。
    /// </remarks>
    public class GlobalMessageBroker : IMessageBroker
    {
        private readonly object gate = new object();
        // このインスタンスを通じた購読。破棄のときにまとめて解除するために持つ。
        private readonly HashSet<TrackedSubscription> subscriptions = new HashSet<TrackedSubscription>();
        private volatile bool disposed;

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public void Publish<T>(T message) where T : struct
        {
            ThrowIfDisposed();
            MessageBroker<T>.Default.Publish(message);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="publishStrategy"/>が定義されていない値。</exception>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public UniTask PublishAsync<T>(T message, AsyncPublishStrategy publishStrategy = AsyncPublishStrategy.Parallel,
            CancellationToken cancellationToken = default) where T : struct
        {
            ThrowIfDisposed();
            var strategy = publishStrategy switch
            {
                AsyncPublishStrategy.Parallel => ZeroAsyncPublishStrategy.Parallel,
                AsyncPublishStrategy.Sequential => ZeroAsyncPublishStrategy.Sequential,
                _ => throw new ArgumentOutOfRangeException(nameof(publishStrategy), publishStrategy, null),
            };
            return MessageBroker<T>.Default.PublishAsync(message, strategy, cancellationToken).AsUniTask();
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();
            return Track(MessageBroker<T>.Default.Subscribe(handler));
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="subscribeStrategy"/>が定義されていない値。</exception>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public IDisposable SubscribeAwait<T>(Func<T, CancellationToken, UniTask> handler,
            AsyncSubscribeStrategy subscribeStrategy = AsyncSubscribeStrategy.Sequential) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();
            var strategy = subscribeStrategy switch
            {
                AsyncSubscribeStrategy.Sequential => ZeroAsyncSubscribeStrategy.Sequential,
                AsyncSubscribeStrategy.Parallel => ZeroAsyncSubscribeStrategy.Parallel,
                AsyncSubscribeStrategy.Switch => ZeroAsyncSubscribeStrategy.Switch,
                AsyncSubscribeStrategy.Drop => ZeroAsyncSubscribeStrategy.Drop,
                _ => throw new ArgumentOutOfRangeException(nameof(subscribeStrategy), subscribeStrategy, null),
            };
            return Track(MessageBroker<T>.Default.SubscribeAwait((x, ct) => handler(x, ct).AsValueTask(), strategy));
        }

        /// <summary>
        /// このインスタンスを通じた購読をすべて解除し、以後の発行・購読を受け付けないようにする。
        /// </summary>
        public void Dispose()
        {
            TrackedSubscription[] removed;
            int count;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                count = subscriptions.Count;
                removed = ArrayPool<TrackedSubscription>.Shared.Rent(count);
                subscriptions.CopyTo(removed);
                subscriptions.Clear();
            }

            try
            {
                for (var i = 0; i < count; i++)
                {
                    removed[i].Dispose();
                }
            }
            finally
            {
                Array.Clear(removed, 0, count);
                ArrayPool<TrackedSubscription>.Shared.Return(removed);
            }
        }

        private IDisposable Track(IDisposable inner)
        {
            var subscription = new TrackedSubscription(this, inner);
            lock (gate)
            {
                if (!disposed)
                {
                    subscriptions.Add(subscription);
                    return subscription;
                }
            }

            inner.Dispose();
            throw new ObjectDisposedException(nameof(GlobalMessageBroker));
        }

        private void Untrack(TrackedSubscription subscription)
        {
            lock (gate)
            {
                subscriptions.Remove(subscription);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(GlobalMessageBroker));
        }

        private sealed class TrackedSubscription : IDisposable
        {
            private readonly GlobalMessageBroker owner;
            private readonly IDisposable inner;
            private int disposed;

            public TrackedSubscription(GlobalMessageBroker owner, IDisposable inner)
            {
                this.owner = owner;
                this.inner = inner;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0) return;

                owner.Untrack(this);
                inner.Dispose();
            }
        }
    }
}
