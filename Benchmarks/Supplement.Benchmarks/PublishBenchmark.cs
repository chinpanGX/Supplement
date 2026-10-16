// GlobalMessageBrokerは非推奨だが、ZeroMessengerとの比較のために測り続ける。
#pragma warning disable CS0618
using System;
using BenchmarkDotNet.Attributes;
using Supplement.Core;
using Supplement.ZeroMessenger;
using ZeroMessenger;
using SupplementMessageBroker = Supplement.Core.MessageBroker;

namespace Supplement.Benchmarks
{
    // 同期のハンドラーへの発行。毎フレーム呼ばれうる、いちばん重視する計測。
    // 利用者はDIで受け取ったインターフェースを通して呼ぶので、発行もインターフェースを通す。
    [MemoryDiagnoser]
    public class PublishBenchmark
    {
        private IMessageBroker supplement;
        private IMessagePublisher<Message> zeroMessenger;
        private IMessageBroker globalMessageBroker;
        private Action<Message> csharpEvent;
        private IDisposable cleanup;

        [Params(0, 1, 10, 100)]
        public int Subscribers { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var supplementBroker = new SupplementMessageBroker();
            var zeroBroker = new MessageBroker<Message>();
            var global = new GlobalMessageBroker();

            // 実際のゲームでは、購読者が0でも、一度購読されて型ごとのチャンネルができていることが多い。
            // チャンネルが無いときだけの近道を測らないよう、購読してすぐに解除しておく。
            supplementBroker.Subscribe<Message>(Handle).Dispose();

            for (var i = 0; i < Subscribers; i++)
            {
                supplementBroker.Subscribe<Message>(Handle);
                zeroBroker.Subscribe(Handle);
                global.Subscribe<Message>(Handle);
                csharpEvent += Handle;
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
