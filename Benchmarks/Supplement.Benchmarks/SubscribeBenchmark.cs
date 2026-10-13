using System;
using BenchmarkDotNet.Attributes;
using Supplement.ZeroMessenger;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;

namespace Supplement.Benchmarks
{
    // 購読して、すぐに解除する。画面を開くたびに起きる程度の頻度の処理で、参考として測る。
    // Supplementは購読者の配列を登録・解除のたびに作り直す(発行を速くするため)ので、購読者が多いほど不利になる。
    [MemoryDiagnoser]
    public class SubscribeBenchmark
    {
        private static readonly Action<Message> Handler = message => Sink.Sum += message.Value;

        private SupplementMessageBroker supplement;
        private MessageBroker<Message> zeroMessenger;
        private GlobalMessageBroker globalMessageBroker;
        private Action<Message> csharpEvent;

        // 計測の前から登録されている購読者の数。
        [Params(0, 100)]
        public int ExistingSubscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            supplement = new SupplementMessageBroker();
            zeroMessenger = new MessageBroker<Message>();
            globalMessageBroker = new GlobalMessageBroker();
            for (var i = 0; i < ExistingSubscribers; i++)
            {
                supplement.Subscribe(Handler);
                zeroMessenger.Subscribe(Handler);
                globalMessageBroker.Subscribe(Handler);
                csharpEvent += Handler;
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            supplement.Dispose();
            zeroMessenger.Dispose();
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
