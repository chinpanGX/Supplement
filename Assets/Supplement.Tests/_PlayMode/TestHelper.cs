using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Loader.Abstractions;
using UnityEngine;

namespace Supplement.Tests.PlayMode
{
    internal class TestHelper
    {
        public static async UniTask SetupEnvAsync(IAssetLoader assetLoader, CancellationToken token)
        {
            var handle =
                await assetLoader.LoadAssetAsync<GameObject>("Assets/Supplement.Tests/Addressables/Env.prefab",
                    token
                );

            handle.AddTo(token);
            Object.Instantiate(handle.Result);
        }
    }
}