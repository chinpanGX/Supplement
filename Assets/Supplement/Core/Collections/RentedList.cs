using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Supplement.Core
{
    /// <summary>
    /// awaitをまたいで使える一時リスト。インスタンスは<see cref="Rent"/>でプールから取り出し、<see cref="Dispose"/>で
    /// 要素の配列(<see cref="ArrayPool{T}"/>)ごとプールに返す。
    /// </summary>
    /// <remarks>
    /// Dispose後のインスタンスは次のRentで使い回されるため、Dispose後に参照を持ち続けて使わないこと。
    /// awaitをまたがないなら、フィールドに持てず漏れようがない<see cref="ScopedList{T}"/>の方が安全。
    /// </remarks>
    public sealed class RentedList<T> : IReadOnlyList<T>, IDisposable
    {
        private const int DefaultCapacity = 16;
        // プールに残しておくインスタンスの上限。これを超えて返されたものは捨てる。
        private const int MaxPoolSize = 32;

        private static readonly bool ClearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static readonly Stack<RentedList<T>> Pool = new();

        private T[] items = Array.Empty<T>();
        private int count;
        private bool isRented;

        private RentedList()
        {
        }

        /// <summary>
        /// プールからリストを取り出す。使い終わったら<see cref="Dispose"/>でプールに返す。
        /// </summary>
        /// <param name="capacity">最初に借りておく配列の長さ。0なら最初の<see cref="Add"/>で借りる。</param>
        public static RentedList<T> Rent(int capacity = 0)
        {
            RentedList<T> list = null;
            lock (Pool)
            {
                if (Pool.Count > 0)
                {
                    list = Pool.Pop();
                }
            }

            list ??= new RentedList<T>();
            list.isRented = true;
            if (capacity > 0)
            {
                list.items = ArrayPool<T>.Shared.Rent(capacity);
            }
            return list;
        }

        /// <summary>
        /// 要素の数。
        /// </summary>
        public int Count => count;

        /// <summary>
        /// <paramref name="index"/>番目の要素。
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>が0未満か<see cref="Count"/>以上。</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));
                return items[index];
            }
            set
            {
                if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));
                items[index] = value;
            }
        }

        /// <summary>
        /// 末尾に要素を足す。配列が足りなければ、倍の長さの配列をプールから借りて移す。
        /// </summary>
        /// <param name="item">足す要素。</param>
        public void Add(T item)
        {
            if (count == items.Length)
            {
                Grow();
            }
            items[count++] = item;
        }

        /// <summary>
        /// 要素を<see cref="Span{T}"/>で返す。要素を足すと配列が入れ替わることがあるため、足した後は取り直す。
        /// </summary>
        public Span<T> AsSpan() => items.AsSpan(0, count);

        /// <summary>
        /// 要素を空にする。借りた配列はそのまま持ち続ける。
        /// </summary>
        public void Clear()
        {
            if (ClearOnReturn)
            {
                Array.Clear(items, 0, count);
            }
            count = 0;
        }

        /// <summary>
        /// 構造体の列挙子を返すため、この型の変数に対する<c>foreach</c>はGCアロケーションを起こさない。
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 配列とこのインスタンスをプールに返す。2回目以降は何もしない。
        /// </summary>
        public void Dispose()
        {
            if (!isRented)
            {
                return;
            }
            isRented = false;

            if (items.Length > 0)
            {
                ArrayPool<T>.Shared.Return(items, ClearOnReturn);
            }
            items = Array.Empty<T>();
            count = 0;

            lock (Pool)
            {
                if (Pool.Count < MaxPoolSize)
                {
                    Pool.Push(this);
                }
            }
        }

        private void Grow()
        {
            var newItems = ArrayPool<T>.Shared.Rent(items.Length == 0 ? DefaultCapacity : items.Length * 2);
            Array.Copy(items, newItems, count);
            if (items.Length > 0)
            {
                ArrayPool<T>.Shared.Return(items, ClearOnReturn);
            }
            items = newItems;
        }

        // ref structにしないのは、asyncメソッドの中のforeachで使えるようにするため。
        /// <summary>
        /// <see cref="RentedList{T}"/>の列挙子。
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly RentedList<T> list;
            private int index;

            internal Enumerator(RentedList<T> list)
            {
                this.list = list;
                index = -1;
            }

            /// <inheritdoc/>
            public T Current => list.items[index];

            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public bool MoveNext() => ++index < list.count;

            /// <inheritdoc/>
            public void Reset() => index = -1;

            /// <summary>
            /// 何もしない。
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}