using System;
using System.Collections.Generic;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="EquatableReadOnlyList{T}"/>を作るための拡張メソッド。
    /// </summary>
    public static class EquatableReadOnlyListExtensions
    {
        /// <summary>
        /// <paramref name="source"/>の要素をコピーした<see cref="EquatableReadOnlyList{T}"/>を作る。
        /// </summary>
        /// <param name="source">コピーする要素。</param>
        /// <typeparam name="T">要素の型。</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>がnull。</exception>
        public static EquatableReadOnlyList<T> ToEquatableReadOnlyList<T>(this IEnumerable<T> source)
            where T : IEquatable<T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new EquatableReadOnlyList<T>(source);
        }
    }
}