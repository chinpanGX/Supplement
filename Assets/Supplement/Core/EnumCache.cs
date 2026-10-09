using System;
using System.Collections.Generic;

namespace Supplement.Core
{
    /// <summary>
    /// 列挙型の値と名前を型ごとにキャッシュする。<see cref="Enum.ToString()"/>・<see cref="Enum.GetValues"/>・
    /// <see cref="Enum.IsDefined"/>は呼ぶたびに文字列や配列の生成・ボックス化が起きるため、代わりに使う。
    /// </summary>
    public static class EnumCache<T> where T : struct, Enum
    {
        // GetValuesとGetNamesはどちらも値の順に並ぶため、同じ添字が同じ定義を指す。
        private static readonly T[] ValueArray = (T[])Enum.GetValues(typeof(T));
        private static readonly string[] NameArray = Enum.GetNames(typeof(T));
        private static readonly Dictionary<T, string> NameByValue = CreateNameByValue();

        /// <summary>
        /// 定義された値。<see cref="Enum.GetValues"/>と同じく値の順に並ぶ。
        /// </summary>
        public static ReadOnlySpan<T> Values => ValueArray;

        /// <summary>
        /// 定義された名前。<see cref="Values"/>と同じ添字が同じ定義を指す。
        /// </summary>
        public static ReadOnlySpan<string> Names => NameArray;

        /// <summary>
        /// 定義された値なら、キャッシュした名前を返す。定義されていない値(Flagsの組み合わせ等)は
        /// <see cref="Enum.ToString()"/>に任せるため、そのときだけ文字列を生成する。
        /// </summary>
        public static string GetName(T value)
        {
            return NameByValue.TryGetValue(value, out var name) ? name : value.ToString();
        }

        /// <summary>
        /// <paramref name="value"/>が定義された値かどうかを返す。Flagsの組み合わせは、その値自体が定義されていなければfalse。
        /// </summary>
        /// <param name="value">調べる値。</param>
        public static bool IsDefined(T value)
        {
            return NameByValue.ContainsKey(value);
        }

        private static Dictionary<T, string> CreateNameByValue()
        {
            var nameByValue = new Dictionary<T, string>(ValueArray.Length);
            for (var i = 0; i < ValueArray.Length; i++)
            {
                // 同じ値に複数の名前がある場合は、先に並ぶ名前を使う。
                nameByValue.TryAdd(ValueArray[i], NameArray[i]);
            }
            return nameByValue;
        }
    }
}