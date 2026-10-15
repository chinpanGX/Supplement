using System;
using BenchmarkDotNet.Attributes;
using Supplement.Core;
using Supplement.ZeroMessenger;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;

namespace Supplement.Benchmarks
{
    // 購読して、すぐに解除する。画面を開くたびに起きる程度の頻度の処理で、参考として測る。
    // 既存の購読者の数で、登録・解除の時間や確保が変わるかを見る。
    [MemoryDiagnoser]
    public class SubscribeBenchmark
    {
        private static readonly Action<Message> Handler = message => Sink.Sum += message.Value;

        private IMessageBroker supplement;
        private IMessageSubscriber<Message> zeroMessenger;
        private IMessageBroker globalMessageBroker;
        private Action<Message> csharpEvent;
        private IDisposable cleanup;

        // 計測の前から登録されている購読者の数。
        [Params(0, 100)]
        public int ExistingSubscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var supplementBroker = new SupplementMessageBroker();
            var zeroBroker = new MessageBroker<Message>();
            var global = new GlobalMessageBroker();
            for (var i = 0; i < ExistingSubscribers; i++)
            {
                supplementBroker.Subscribe(Handler);
                zeroBroker.Subscribe(Handler);
                global.Subscribe(Handler);
                csharpEvent += Handler;
            }

            supplement = supplementBroker;
            zeroMessenger = zeroBroker;
            globalMessageBroker = global;
            cleanup = zeroBroker;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            supplement.Dispose();
            cleanup.Dispose();
            globalMessageBroker.Dispose();
            csharpEvent = null;
        }

        [Benchmark(Baseline = true, Description = "C# event")]
        public void CSharpEventAddRemove()
        {
            csharpEvent += Handler;
            csharpEvent -= Handler;
        }

        [Benchmark(Description = "Supplement MessageBroker")]
        public void SupplementSubscribeDispose()
        {
            supplement.Subscribe(Handler).Dispose();
        }

        [Benchmark(Description = "ZeroMessenger")]
        public void ZeroMessengerSubscribeDispose()
        {
            zeroMessenger.Subscribe(Handler).Dispose();
        }

        [Benchmark(Description = "GlobalMessageBroker (ZeroMessenger)")]
        public void GlobalMessageBrokerSubscribeDispose()
        {
            globalMessageBroker.Subscribe(Handler).Dispose();
        }
    }
}
