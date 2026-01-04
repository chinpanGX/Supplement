using System;
using Supplement.Core;
using ZeroMessenger;

namespace Supplement.ZeroMessenger
{
    public class GlobalMessageBroker : IMessageBroker
    {
        public void Publish<T>(T message) where T : struct
        {
            MessageBroker<T>.Default.Publish(message);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            return MessageBroker<T>.Default.Subscribe(handler);
        }
    }
}