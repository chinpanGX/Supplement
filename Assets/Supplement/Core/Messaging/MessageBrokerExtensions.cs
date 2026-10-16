using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="IMessageBroker"/>の拡張メソッド。
    /// </summary>
    public static class MessageBrokerExtensions
    {
        /// <summary>
        /// 次に発行された<typeparamref name="T"/>のメッセージを1件だけ待つ。受け取ったら購読を解除する。
        /// </summary>
        /// <remarks>
        /// 呼んだ時点から購読するため、それより前に発行されたメッセージは受け取らない。
        /// ブローカーを破棄しても完了しないので、待つのをやめるときは<paramref name="cancellationToken"/>で取り消す。
        /// </remarks>
        /// <param name="broker">購読するブローカー。</param>
        /// <param name="cancellationToken">取り消すと購読を解除し、<see cref="OperationCanceledException"/>で完了する。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>受け取ったメッセージで完了する。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="broker"/>がnull。</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="broker"/>は破棄されている。</exception>
        public static UniTask<T> FirstAsync<T>(this IMessageBroker broker, CancellationToken cancellationToken = default)
            where T : struct
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<T>(cancellationToken);

            var promise = new FirstMessagePromise<T>(cancellationToken);
            promise.Start(broker);
            return promise.Task;
        }

        // FirstAsyncの待ちの1回分。メッセージの受信と取り消しのうち、先に来た方で完了し、購読と取り消しの登録を片付ける。
        // 発行はどのスレッドからも来るので、完了と片付けはgateのlockの中で1回だけにする。
        private sealed class FirstMessagePromise<T> where T : struct
        {
            private readonly object gate = new();
            private readonly UniTaskCompletionSource<T> source = new();
            private readonly CancellationToken cancellationToken;
            private IDisposable subscription;
            private CancellationTokenRegistration registration;
            private bool finished;

            public FirstMessagePromise(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public UniTask<T> Task => source.Task;

            public void Start(IMessageBroker broker)
            {
                var newSubscription = broker.Subscribe<T>(OnMessage);
                var newRegistration = cancellationToken.CanBeCanceled
                    ? cancellationToken.RegisterWithoutCaptureExecutionContext(
                        static x => ((FirstMessagePromise<T>)x).OnCanceled(), this)
                    : default(CancellationTokenRegistration);

                // 登録が済む前に、別のスレッドからの発行か取り消しで完了していることがある。その場合は自分で片付ける。
                lock (gate)
                {
                    if (!finished)
                    {
                        subscription = newSubscription;
                        registration = newRegistration;
                        return;
                    }
                }

                newSubscription.Dispose();
                newRegistration.Dispose();
            }

            private void OnMessage(T message)
            {
                if (!TryFinish(true)) return;

                source.TrySetResult(message);
            }

            private void OnCanceled()
            {
                // 取り消しのコールバックの中なので、登録は解除しない(済んでいる)。
                if (!TryFinish(false)) return;

                source.TrySetCanceled(cancellationToken);
            }

            // 最初の1回だけtrueを返し、購読(と、disposeRegistrationなら取り消しの登録)を解除する。
            private bool TryFinish(bool disposeRegistration)
            {
                IDisposable finishedSubscription;
                CancellationTokenRegistration finishedRegistration;
                lock (gate)
                {
                    if (finished) return false;

                    finished = true;
                    finishedSubscription = subscription;
                    finishedRegistration = registration;
                    subscription = null;
                    registration = default;
                }

                finishedSubscription?.Dispose();
                if (disposeRegistration)
                {
                    finishedRegistration.Dispose();
                }
                return true;
            }
        }
    }
}
