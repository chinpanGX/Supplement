using System;
using Supplement.Core;
using ZeroMessenger;

namespace Supplement.ZeroMessenger
{
    /// <summary>
    /// ZeroMessengerの<c>MessageBroker&lt;T&gt;.Default</c>で、アプリ全体で共有するメッセージを送受信する。
    /// </summary>
    public class GlobalMessageBroker : IMessageBroker
    {
        /// <inheritdoc/>
        public void Publish<T>(T message) where T : struct
        {
            MessageBroker<T>.Default.Publish(message);
        }

        /// <inheritdoc/>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            return MessageBroker<T>.Default.Subscribe(handler);
        }
    }
}