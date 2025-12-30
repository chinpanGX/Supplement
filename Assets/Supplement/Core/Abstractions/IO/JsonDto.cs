using System;

namespace Supplement.Core.Abstractions
{
    /// <summary>
    ///     JSONデータを保持するための汎用的なデータ転送オブジェクトクラス。
    ///     指定された型のデータを格納するために使用されます。
    /// </summary>
    /// <typeparam name="T">
    ///     格納されるデータの型。
    /// </typeparam>
    [Serializable]
    public class JsonDto<T>
    {
        public T Data;
    }
}