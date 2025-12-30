using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Supplement.Loader.Abstractions
{
    public interface IAssetLoader
    {
        UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string address, CancellationToken token) where T : Object;
    }
}