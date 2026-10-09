using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="ArrayPool{T}"/>の配列に要素を集める一時リスト。メソッドの中で集めて使い終わるまでの用途向けで、
    /// <see cref="Dispose"/>で配列をプールに返す。
    /// </summary>
    /// <remarks>
    /// ref structのため、フィールドに持ったりawaitをまたいだりできない(コンパイラが弾く)。awaitをまたぐ場合は
    /// <see cref="RentedList{T}"/>を使う。コピーを両方Disposeすると同じ配列を二重にプールへ返すため、
    /// 値渡しせず<c>using var</c>で受けた変数だけを使うこと。
    /// </remarks>
    public ref struct ScopedList<T>
    {
        private const int DefaultCapacity = 16;
        private static readonly bool ClearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

        private T[] items;
        private int count;

        /// <summary>
        /// 最初からcapacity分の配列を借りる。既定のコンストラクタでは、最初のAddで借りる。
        /// </summary>
        public ScopedList(int capacity)
        {
            items = capacity > 0 ? ArrayPool<T>.Shared.Rent(capacity) : null;
            count = 0;
        }

        /// <summary>
        /// 要素の数。
        /// </summary>
        public int Count => count;

        /// <summary>
        /// <paramref name="index"/>番目の要素への参照。
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>が0未満か<see cref="Count"/>以上。</exception>
        public ref T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));
                return ref items[index];
            }
        }

        /// <summary>
        /// 末尾に要素を足す。配列が足りなければ、倍の長さの配列をプールから借りて移す。
        /// </summary>
        /// <param name="item">足す要素。</param>
        public void Add(T item)
        {
            if (items is null || count == items.Length)
            {
                Grow();
            }
            items[count++] = item;
        }

        /// <summary>
        /// 要素を<see cref="Span{T}"/>で返す。要素を足すと配列が入れ替わることがあるため、足した後は取り直す。
        /// </summary>
        public Span<T> AsSpan() => items is null ? Span<T>.Empty : items.AsSpan(0, count);

        /// <summary>
        /// 要素の列挙子を返す。<c>foreach</c>はGCアロケーションを起こさない。
        /// </summary>
        public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        /// <summary>
        /// 要素を新しい配列にコピーして返す。返す配列はプールのものではない。
        /// </summary>
        public T[] ToArray() => AsSpan().ToArray();

        /// <summary>
        /// 要素を空にする。借りた配列はそのまま持ち続ける。
        /// </summary>
        public void Clear()
        {
            if (ClearOnReturn)
            {
                AsSpan().Clear();
            }
            count = 0;
        }

        /// <summary>
        /// 借りた配列をプールに返す。
        /// </summary>
        public void Dispose()
        {
            if (items is not null)
            {
                ArrayPool<T>.Shared.Return(items, ClearOnReturn);
            }
            items = null;
            count = 0;
        }

        private void Grow()
        {
            var newItems = ArrayPool<T>.Shared.Rent(items is null || items.Length == 0 ? DefaultCapacity : items.Length * 2);
            if (items is not null)
            {
                Array.Copy(items, newItems, count);
                ArrayPool<T>.Shared.Return(items, ClearOnReturn);
            }
            items = newItems;
        }
    }
}