using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Supplement
{
    /// <summary>
    /// 値比較が可能な読み取り専用リスト。
    /// </summary>
    public sealed class EquatableReadOnlyList<T> :
        IReadOnlyList<T>,
        IEquatable<EquatableReadOnlyList<T>>
    {
        private readonly List<T> list;

        public EquatableReadOnlyList()
        {
            list = new();
        }

        public EquatableReadOnlyList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            list = new(capacity);
        }

        public EquatableReadOnlyList(IEnumerable<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            list = new List<T>(collection);
        }

        /// <summary>
        /// 読み取り専用インデクサ。
        /// </summary>
        public T this[int index] => list[index];

        public int Count => list.Count;

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 値としての等価判定。
        /// 同じ順序・同じ要素の並びであれば等しい。
        /// </summary>
        public bool Equals(EquatableReadOnlyList<T> other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;

            return list.SequenceEqual(other.list);
        }

        public override bool Equals(object obj) =>
            obj is EquatableReadOnlyList<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var value in list)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", list)}]";
        }

        public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
        {
            return !(left == right);
        }
    }

    #region Extensions

    #endregion Extensions
}