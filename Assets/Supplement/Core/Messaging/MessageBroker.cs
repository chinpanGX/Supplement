using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// サードパーティのライブラリに依存しない<see cref="IMessageBroker"/>の実装。
    /// アプリ全体で共有する場合は<see cref="Default"/>を使う。
    /// </summary>
    /// <remarks>
    /// 発行・購読・解除は、どのスレッドから呼んでもよい。発行中に登録・解除しても、その発行は開始時点の購読者に届ける。
    /// ただし、解除した購読者には、発行中であってもそれ以降は届けない。
    /// </remarks>
    public sealed class MessageBroker : IMessageBroker
    {
        private static int lastTypeIndex = -1;

        /// <summary>
        /// アプリ全体で共有するブローカー。DIのスコープとは関係なく、いつでも使える。
        /// </summary>
        /// <remarks>
        /// 破棄すると以後アプリ全体で使えなくなるため、破棄しない。DIに登録するときは、コンテナが破棄しないよう
        /// <c>RegisterInstance</c>で登録する。
        /// </remarks>
        public static MessageBroker Default { get; } = new();

        private readonly object gate = new();
        // 型ごとのチャンネル。添字はTypeIndex<T>.Valueで、要素はMessageChannel<T>。
        private object[] channels = Array.Empty<object>();
        private volatile bool disposed;

        /// <inheritdoc/>
        /// <remarks>
        /// 同期のハンドラーは、この呼び出しの中で登録順に呼ぶ。非同期のハンドラー(<see cref="SubscribeAwait{T}"/>)は
        /// 開始するだけで完了を待たない。
        /// ハンドラーが例外を投げても残りのハンドラーは呼び、例外は呼び出し元に投げずに
        /// <c>UniTaskScheduler.UnobservedTaskException</c>に流す(非同期のハンドラーの例外と同じ)。
        /// </remarks>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public void Publish<T>(T message) where T : struct
        {
            ThrowIfDisposed();
            GetChannel<T>()?.Publish(message);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 同じデリゲートを重ねて登録すると、登録した回数だけ呼ぶ。
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var channel = GetOrCreateChannel<T>();
            return channel.Add(new SyncMessageSubscriber<T>(channel, handler));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="Publish{T}"/>で開始したハンドラーが投げた例外は、<c>UniTaskScheduler.UnobservedTaskException</c>に流す。
        /// 購読の解除・<see cref="AsyncSubscribeStrategy.Switch"/>での切り替えで取り消された場合の
        /// <see cref="OperationCanceledException"/>は、例外として扱わない。
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="subscribeStrategy"/>が定義されていない値。</exception>
        /// <exception cref="ObjectDisposedException">このブローカーは破棄されている。</exception>
        public IDisposable SubscribeAwait<T>(Func<T, CancellationToken, UniTask> handler,
            AsyncSubscribeStrategy subscribeStrategy = AsyncSubscribeStrategy.Sequential) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var channel = GetOrCreateChannel<T>();
            MessageSubscriber<T> subscriber = subscribeStrategy switch
            {
                AsyncSubscribeStrategy.Sequential => new SequentialAsyncMessageSubscriber<T>(channel, handler),
                AsyncSubscribeStrategy.Parallel => new ParallelAsyncMessageSubscriber<T>(channel, handler),
                AsyncSubscribeStrategy.Switch => new SwitchAsyncMessageSubscriber<T>(channel, handler),
                AsyncSubscribeStrategy.Drop => new DropAsyncMessageSubscriber<T>(channel, handler),
                AsyncSubscribeStrategy.ThrottleFirstLast => new ThrottleFirstLastAsyncMessageSubscriber<T>(channel, handler),
                _ => throw new ArgumentOutOfRangeException(nameof(subscribeStrategy), subscribeStrategy, null),
            };
            return channel.Add(subscriber);
        }

        /// <summary>
        /// 購読をすべて解除し、以後の発行・購読を受け付けないようにする。処理中の非同期のハンドラーに渡したトークンは取り消す。
        /// </summary>
        public void Dispose()
        {
            object[] closing;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                closing = channels;
                channels = Array.Empty<object>();
            }

            foreach (var channel in closing)
            {
                ((IMessageChannel)channel)?.Close();
            }
        }

        // 購読をすべて解除する。ブローカーは引き続き使える。ドメインリロードを無効にしたときに、前回のプレイの購読を消すために使う。
        internal void UnsubscribeAll()
        {
            foreach (var channel in Volatile.Read(ref channels))
            {
                ((IMessageChannel)channel)?.Clear();
            }
        }

        private MessageChannel<T> GetChannel<T>() where T : struct
        {
            var index = TypeIndex<T>.Value;
            var current = Volatile.Read(ref channels);
            // その添字にはMessageChannel<T>しか入れないので、型を確かめるキャストを省く。
            return index < current.Length ? Unsafe.As<MessageChannel<T>>(Volatile.Read(ref current[index])) : null;
        }

        private MessageChannel<T> GetOrCreateChannel<T>() where T : struct
        {
            var existing = GetChannel<T>();
            if (existing != null)
            {
                ThrowIfDisposed();
                return existing;
            }

            lock (gate)
            {
                ThrowIfDisposed();

                var index = TypeIndex<T>.Value;
                if (index < channels.Length && channels[index] is MessageChannel<T> created)
                {
                    return created;
                }

                created = new MessageChannel<T>();
                if (index < channels.Length)
                {
                    Volatile.Write(ref channels[index], created);
                    return created;
                }

                // 発行は配列を読むだけでロックを取らないため、大きくするときは作り直してから差し替える。
                var resized = new object[Math.Max(index + 1, channels.Length * 2)];
                channels.CopyTo(resized, 0);
                resized[index] = created;
                Volatile.Write(ref channels, resized);
                return created;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(MessageBroker));
        }

        // メッセージの型ごとに0から振る連番。Dictionaryを引かずに、配列の添字でチャンネルを取り出すために使う。
        private static class TypeIndex<T>
        {
            public static readonly int Value = Interlocked.Increment(ref lastTypeIndex);
        }
    }
}
