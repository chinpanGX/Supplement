using System;
using Object = UnityEngine.Object;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// 読み込んだアセットへのハンドル。破棄するとアセットを解放する。
    /// </summary>
    /// <typeparam name="T">アセットの型。</typeparam>
    public interface IAssetHandle<T> : IDisposable where T : Object
    {
        /// <summary>
        /// 読み込みが終わったかどうか。
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// 読み込みに成功したかどうか。
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        /// 読み込んだアセット。破棄した後に取得すると<see cref="ObjectDisposedException"/>を投げる。
        /// </summary>
        T Result { get; }
    }
}