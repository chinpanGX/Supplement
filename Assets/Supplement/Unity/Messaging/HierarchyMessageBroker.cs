using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// 自身より下の階層のコンポーネントが発行したメッセージを、型ごとに購読者へ届ける。
    /// </summary>
    /// <remarks>
    /// <see cref="Publish{T}(Component,T)"/>は呼ぶたびに親を辿ってブローカーを探す。頻繁に発行する場合は、
    /// 探したブローカーを持っておいて<see cref="Publish{T}(T)"/>を呼ぶ。
    /// </remarks>
    public class HierarchyMessageBroker : MonoBehaviour
    {
        private static int lastTypeIndex = -1;

        // 型ごとの購読者。添字はTypeIndex<T>.Valueで、要素はHandlerList<T>。
        private object[] handlerLists = Array.Empty<object>();

        /// <summary>
        /// <paramref name="component"/>から親を辿って最も近い<see cref="HierarchyMessageBroker"/>(非アクティブのものを含む)を探し、
        /// <paramref name="message"/>を発行する。見つからなければ何もしない。
        /// </summary>
        /// <param name="component">発行元のコンポーネント。</param>
        /// <param name="message">発行するメッセージ。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="component"/>がnull。</exception>
        public static void Publish<T>(Component component, T message) where T : struct
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            var broker = component.GetComponentInParent<HierarchyMessageBroker>(true);
            if (broker == null) return;

            broker.Publish(message);
        }

        /// <summary>
        /// 購読者を登録順に呼ぶ。購読者が例外を投げても残りの購読者は呼び、最後にまとめて<see cref="AggregateException"/>で投げる。
        /// </summary>
        public void Publish<T>(T message) where T : struct
        {
            GetHandlerList<T>(false)?.Invoke(message);
        }

        /// <summary>
        /// ハンドラーを登録する。返した<see cref="IDisposable"/>を破棄すると登録を解除する。
        /// </summary>
        /// <remarks>
        /// 同じデリゲートのインスタンスを重ねて登録しても、発行時に呼ぶのは1回だけ。登録の回数は数えていて、
        /// すべての戻り値が破棄されるまで登録は残る。
        /// </remarks>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            GetHandlerList<T>(true).Add(handler);
            return new Subscription<T>(this, handler);
        }

        private void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            GetHandlerList<T>(false)?.Remove(handler);
        }

        private HandlerList<T> GetHandlerList<T>(bool createIfMissing) where T : struct
        {
            var index = TypeIndex<T>.Value;
            if (index < handlerLists.Length && handlerLists[index] is HandlerList<T> existing)
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return null;
            }

            if (index >= handlerLists.Length)
            {
                Array.Resize(ref handlerLists, Math.Max(index + 1, handlerLists.Length * 2));
            }

            var created = new HandlerList<T>();
            handlerLists[index] = created;
            return created;
        }

        // メッセージの型ごとに0から振る連番。Dictionaryを引かずに、配列の添字で購読者を取り出すために使う。
        private static class TypeIndex<T>
        {
            public static readonly int Value = Interlocked.Increment(ref lastTypeIndex);
        }

        // 購読者の配列は登録・解除のたびに作り直し、発行時は開始時点の配列をそのまま回す。発行中に登録・解除が
        // 起きても添字がずれず、発行のたびの配列のコピーも要らない。登録・解除より発行の方がずっと多い前提。
        private sealed class HandlerList<T> where T : struct
        {
            private Action<T>[] handlers = Array.Empty<Action<T>>();
            // handlersと同じ添字で、そのデリゲートが登録された回数。発行では使わないため、作り直さずにその場で増減する。
            private int[] subscriptionCounts = Array.Empty<int>();

            public void Add(Action<T> handler)
            {
                var index = IndexOf(handler);
                if (index >= 0)
                {
                    subscriptionCounts[index]++;
                    return;
                }

                var newHandlers = new Action<T>[handlers.Length + 1];
                handlers.CopyTo(newHandlers, 0);
                newHandlers[handlers.Length] = handler;
                var newCounts = new int[subscriptionCounts.Length + 1];
                subscriptionCounts.CopyTo(newCounts, 0);
                newCounts[subscriptionCounts.Length] = 1;
                handlers = newHandlers;
                subscriptionCounts = newCounts;
            }

            public void Remove(Action<T> handler)
            {
                var index = IndexOf(handler);
                if (index < 0)
                {
                    return;
                }

                if (--subscriptionCounts[index] > 0)
                {
                    return;
                }

                handlers = RemoveAt(handlers, index);
                subscriptionCounts = RemoveAt(subscriptionCounts, index);
            }

            private static TElement[] RemoveAt<TElement>(TElement[] source, int index)
            {
                if (source.Length == 1)
                {
                    return Array.Empty<TElement>();
                }

                var result = new TElement[source.Length - 1];
                Array.Copy(source, 0, result, 0, index);
                Array.Copy(source, index + 1, result, index, source.Length - index - 1);
                return result;
            }

            public void Invoke(T message)
            {
                List<Exception> exceptions = null;
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler(message);
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

            // デリゲートのEqualsは同じ対象・同じメソッドなら別インスタンスでも等しいとみなすため、参照で探す。
            private int IndexOf(Action<T> handler)
            {
                for (var i = 0; i < handlers.Length; i++)
                {
                    if (ReferenceEquals(handlers[i], handler))
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        private sealed class Subscription<T> : IDisposable where T : struct
        {
            private readonly HierarchyMessageBroker owner;
            private readonly Action<T> handler;
            private bool disposed;

            public Subscription(HierarchyMessageBroker owner, Action<T> handler)
            {
                this.owner = owner;
                this.handler = handler;
            }

            // 2回目以降を無視しないと、同じデリゲートのほかの登録の分まで回数を減らしてしまう。
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                owner.Unsubscribe(handler);
            }
        }
    }
}