using System;
using System.Collections.Generic;

namespace Supplement
{
    public static class EquatableReadOnlyListExtensions
    {
        public static EquatableReadOnlyList<T> ToEquatableReadOnlyList<T>(this IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new EquatableReadOnlyList<T>(source);
        }
    }
}