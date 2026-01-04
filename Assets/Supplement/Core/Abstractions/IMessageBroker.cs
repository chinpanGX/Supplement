using System;

namespace Supplement.Core
{
    public interface IMessageBroker
    {
        void Publish<T>(T message) where T : struct;
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }
}