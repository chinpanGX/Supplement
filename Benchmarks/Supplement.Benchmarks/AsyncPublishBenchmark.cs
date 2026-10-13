using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cysharp.Threading.Tasks;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;
using SupplementStrategy = Supplement.Core.AsyncSubscribeStrategy;
using ZeroStrategy = ZeroMessenger.AsyncSubscribeStrategy;

namespace Supplement.Benchmarks
{
    // 非同期の購読(SubscribeAwait)のハンドラーへのPublish。ハンドラーは同期的に完了する(キャッシュの更新などの軽い処理を想定)。
    // 両方にあるSequentialとParallelを比べる。
    [MemoryDiagnoser]
    public class AsyncPublishBenchmark
    {
        public enum Strategy
        {
            Sequential,
            Parallel,
        }

        private SupplementMessageBroker supplement;
        private MessageBroker<Message> zeroMessenger;

        [Params(Strategy.Sequential, Strategy.Parallel)]
        public Strategy SubscribeStrategy { get; set; }

        [Params(1, 10)]
        public int Subscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            supplement = new SupplementMessageBroker();
            zeroMessenger = new MessageBroker<Message>();
            var supplementStrategy = SubscribeStrategy == Strategy.Sequential
                ? SupplementStrategy.Sequential
                : SupplementStrategy.Parallel;
            var zeroStrategy = SubscribeStrategy == Strategy.Sequential
                ? ZeroStrategy.Sequential
                : ZeroStrategy.Parallel;
            for (var i = 0; i < Subscribers; i++)
            {
                supplement.SubscribeAwait<Message>(HandleUniTaskAsync, supplementStrategy);
                zeroMessenger.SubscribeAwait(HandleValueTaskAsync, zeroStrategy);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            supplement.Dispose();
            zeroMessenger.Dispose();
        }

        [Benchmark(Baseline = true, Description = "Supplement MessageBroker")]
        public void SupplementPublish()
        {
            supplement.Publish(new Message { Value = 1 });
        }

        [Benchmark(Description = "ZeroMessenger")]
        public void ZeroMessengerPublish()
        {
            zeroMessenger.Publish(new Message { Value = 1 });
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
