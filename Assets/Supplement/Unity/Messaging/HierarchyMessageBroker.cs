using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supplement.Unity
{
    public class HierarchyMessageBroker : MonoBehaviour
    {
        private const int InitialCapacity = 8;
        private readonly Dictionary<RuntimeTypeHandle, Delegate> handlers = new(InitialCapacity);

        public static void Publish<T>(Component component, T message) where T : struct
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            var broker = component.GetComponentInParent<HierarchyMessageBroker>(true);
            if (broker == null) return;

            broker.PublishToSubscribers(message);
        }

        private void PublishToSubscribers<T>(T message) where T : struct
        {
            if (!handlers.TryGetValue(typeof(T).TypeHandle, out var handlerDelegate))
            {
                return;
            }

            var typedHandlers = (Action<T>)handlerDelegate;
            var invocationList = typedHandlers.GetInvocationList();
            List<Exception> exceptions = null;

            foreach (var handler in invocationList)
            {
                try
                {
                    ((Action<T>)handler)(message);
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = typeof(T).TypeHandle;

            if (handlers.TryGetValue(key, out var existingHandlerDelegate))
            {
                var existingTypedHandlers = (Action<T>)existingHandlerDelegate;
                foreach (var existingHandler in existingTypedHandlers.GetInvocationList())
                {
                    if (ReferenceEquals(existingHandler, handler))
                    {
                        return;
                    }
                }

                handlers[key] = Delegate.Combine(existingHandlerDelegate, handler);
            }
            else
            {
                handlers.Add(key, handler);
            }
        }
    }
}