using System;
using BenchmarkDotNet.Attributes;
using Supplement.ZeroMessenger;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;

namespace Supplement.Benchmarks
{
    // 同期のハンドラーへの発行。毎フレーム呼ばれうる、いちばん重視する計測。
    [MemoryDiagnoser]
    public class PublishBenchmark
    {
        private SupplementMessageBroker supplement;
        private MessageBroker<Message> zeroMessenger;
        private GlobalMessageBroker globalMessageBroker;
        private Action<Message> csharpEvent;

        [Params(0, 1, 10, 100)]
        public int Subscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            supplement = new SupplementMessageBroker();
            zeroMessenger = new MessageBroker<Message>();
            globalMessageBroker = new GlobalMessageBroker();
            for (var i = 0; i < Subscribers; i++)
            {
                supplement.Subscribe<Message>(Handle);
                zeroMessenger.Subscribe(Handle);
                globalMessageBroker.Subscribe<Message>(Handle);
                csharpEvent += Handle;
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
        public void CSharpEventInvoke()
        {
            csharpEvent?.Invoke(new Message { Value = 1 });
        }

        [Benchmark(Description = "Supplement MessageBroker")]
        public void SupplementPublish()
        {
            supplement.Publish(new Message { Value = 1 });
        }

        [Benchmark(Description = "ZeroMessenger")]
        public void ZeroMessengerPublish()
        {
            zeroMessenger.Publish(new Message { Value = 1 });
        }

        [Benchmark(Description = "GlobalMessageBroker (ZeroMessenger)")]
        public void GlobalMessageBrokerPublish()
        {
            globalMessageBroker.Publish(new Message { Value = 1 });
        }

        private static void Handle(Message message)
        {
            Sink.Sum += message.Value;
        }
    }
}
