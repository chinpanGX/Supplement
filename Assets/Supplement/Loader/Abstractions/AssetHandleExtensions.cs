using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// <see cref="IAssetHandle{T}"/>の拡張メソッド。
    /// </summary>
    public static class AssetHandleExtensions
    {
        /// <summary>
        /// <paramref name="handles"/>をすべて破棄する。<paramref name="handles"/>がnullなら何もしない。
        /// </summary>
        /// <param name="handles">破棄するハンドル。</param>
        /// <typeparam name="T">アセットの型。</typeparam>
        public static void DisposeAll<T>(this IReadOnlyList<IAssetHandle<T>> handles) where T : Object
        {
            if (handles == null)
            {
                return;
            }

            foreach (var handle in handles)
            {
                handle.Dispose();
            }
        }
    }
}
