using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Supplement.Loader.Abstractions
{
    public static class AssetHandleExtensions
    {
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
