using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;
using SupplementSubscribeStrategy = Supplement.Core.AsyncSubscribeStrategy;

namespace Supplement.Benchmarks
{
    // 非同期の購読(SubscribeAwait)のハンドラーへのPublish。ストラテジーは両方にある4つすべてを比べる。
    // ハンドラーが同期的に完了する場合と、本当に待つ場合(発行の後で完了させる)を測る。
    // 本当に待つ場合は、ハンドラーを完了させるところまでを1回として測る。
    [MemoryDiagnoser]
    public class SubscribeAwaitBenchmark
    {
        private IMessageBroker supplement;
        private IMessagePublisher<Message> zeroMessenger;
        private IDisposable cleanup;
        private UniTaskSignal[] supplementSignals;
        private ValueTaskSignal[] zeroSignals;

        [ParamsAllValues]
        public SupplementSubscribeStrategy SubscribeStrategy { get; set; }

        [ParamsAllValues]
        public HandlerCompletion Completion { get; set; }

        [Params(1, 10)]
        public int Subscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var supplementBroker = new SupplementMessageBroker();
            var zeroBroker = new MessageBroker<Message>();
            var zeroStrategy = Strategies.ToZero(SubscribeStrategy);
            var asynchronous = Completion == HandlerCompletion.Asynchronous;
            supplementSignals = new UniTaskSignal[asynchronous ? Subscribers : 0];
            zeroSignals = new ValueTaskSignal[asynchronous ? Subscribers : 0];

            for (var i = 0; i < Subscribers; i++)
            {
                if (asynchronous)
                {
                    var supplementSignal = supplementSignals[i] = new UniTaskSignal();
                    var zeroSignal = zeroSignals[i] = new ValueTaskSignal();
                    supplementBroker.SubscribeAwait<Message>((message, _) =>
                    {
                        Sink.Sum += message.Value;
                        return supplementSignal.Wait();
                    }, SubscribeStrategy);
                    zeroBroker.SubscribeAwait((message, _) =>
                    {
                        Sink.Sum += message.Value;
                        return zeroSignal.Wait();
                    }, zeroStrategy);
                }
                else
                {
                    supplementBroker.SubscribeAwait<Message>(HandleUniTaskAsync, SubscribeStrategy);
                    zeroBroker.SubscribeAwait(HandleValueTaskAsync, zeroStrategy);
                }
            }

            supplement = supplementBroker;
            zeroMessenger = zeroBroker;
            cleanup = zeroBroker;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            supplement.Dispose();
            cleanup.Dispose();
        }

        [Benchmark(Baseline = true, Description = "Supplement MessageBroker")]
        public void SupplementPublish()
        {
            supplement.Publish(new Message { Value = 1 });
            foreach (var signal in supplementSignals)
            {
                signal.Complete();
            }
        }

        [Benchmark(Description = "ZeroMessenger")]
        public void ZeroMessengerPublish()
        {
            zeroMessenger.Publish(new Message { Value = 1 });
            foreach (var signal in zeroSignals)
            {
                signal.Complete();
            }
        }

        private static UniTask HandleUniTaskAsync(Message message, CancellationToken cancellationToken)
        {
            Sink.Sum += message.Value;
            return UniTask.CompletedTask;
        }

        private static ValueTask HandleValueTaskAsync(Message message, CancellationToken cancellationToken)
        {
            Sink.Sum += message.Value;
            return default;
        }
    }
}
