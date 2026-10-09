using System;
using System.Collections;
using System.Collections.Generic;

namespace Supplement.Core
{
    /// <summary>
    /// 値比較が可能な読み取り専用リスト。
    /// </summary>
    /// <remarks>
    /// 要素の比較は<typeparamref name="T"/>の<see cref="IEquatable{T}"/>に任せる。参照比較のクラスや
    /// <see cref="IEquatable{T}"/>を持たない構造体(比較のたびにボックス化する)を要素にできないよう、制約で弾いている。
    /// 比較は要素の1段目だけなので、要素が配列やリストを持つ場合、そのメンバーは要素側の<see cref="IEquatable{T}"/>次第になる。
    /// </remarks>
    public sealed class EquatableReadOnlyList<T> :
        IReadOnlyList<T>,
        IEquatable<EquatableReadOnlyList<T>>
        where T : IEquatable<T>
    {
        private readonly List<T> list;

        /// <summary>
        /// 空のリストを作る。
        /// </summary>
        public EquatableReadOnlyList()
        {
            list = new List<T>();
        }

        /// <summary>
        /// 容量を指定して、空のリストを作る。
        /// </summary>
        /// <param name="capacity">最初に確保する容量。</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/>が負。</exception>
        public EquatableReadOnlyList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            list = new List<T>(capacity);
        }

        /// <summary>
        /// <paramref name="collection"/>の要素をコピーしたリストを作る。
        /// </summary>
        /// <param name="collection">コピーする要素。</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/>がnull。</exception>
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

        /// <summary>
        /// 要素の数。
        /// </summary>
        public int Count => list.Count;

        /// <summary>
        /// 構造体の列挙子を返すため、この型の変数に対する<c>foreach</c>はGCアロケーションを起こさない。
        /// <see cref="IEnumerable{T}"/>や<see cref="IReadOnlyList{T}"/>として列挙した場合はボックス化される。
        /// </summary>
        public List<T>.Enumerator GetEnumerator() => list.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

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
            if (list.Count != other.list.Count)
                return false;

            // LINQのSequenceEqualは列挙子を2つ確保するため、添字で比べる。
            // TがIEquatable<T>を持つため、Defaultはボックス化しない比較子になる。
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < list.Count; i++)
            {
                if (!comparer.Equals(list[i], other.list[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <paramref name="obj"/>が同じ要素の型の<see cref="EquatableReadOnlyList{T}"/>で、同じ順序・同じ要素の並びであれば等しい。
        /// </summary>
        public override bool Equals(object obj) =>
            obj is EquatableReadOnlyList<T> other && Equals(other);

        /// <summary>
        /// 要素の並びから計算したハッシュ値を返す。<see cref="Equals(EquatableReadOnlyList{T})"/>で等しいリストは同じ値になる。
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var value in list)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }

        /// <summary>
        /// 要素を<c>[a, b, c]</c>の形で並べた文字列を返す。
        /// </summary>
        public override string ToString()
        {
            return $"[{string.Join(", ", list)}]";
        }

        /// <summary>
        /// 値として等しいかどうかを返す。どちらもnullなら等しい。
        /// </summary>
        public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        /// <summary>
        /// 値として等しくないかどうかを返す。
        /// </summary>
        public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
        {
            return !(left == right);
        }
    }
}