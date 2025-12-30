using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Loader.Abstractions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Supplement.Loader.AddressablesLoader
{
    public class AddressablesAssetLoader : IAssetLoader, ISceneLoader
    {
        public async UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string address, CancellationToken token)
            where T : Object
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                await handle.ToUniTask(cancellationToken: token);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new AssetLoadFailedException($"Failed to load scene. address: {address}");
                }
                return new AddressablesAssetHandle<T>(handle);
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning($"LoadAssetAsync was canceled. address : {address}\n{e}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load asset. address: {address}\n{e}");
                throw;
            }
        }

        public async UniTask<ISceneHandle> LoadSceneAsync(string address, bool additive, bool activateOnLoad,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            try
            {
                var handle = Addressables.LoadSceneAsync(address,
                    additive ? LoadSceneMode.Additive : LoadSceneMode.Single,
                    activateOnLoad
                );
                await handle.Task.AsUniTask();
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new AssetLoadFailedException($"Failed to load scene. address: {address}");
                }

                return new AddressablesSceneHandle(handle, activateOnLoad);
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning($"LoadSceneAsync was canceled. address: {address}\n{e}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load scene. address: {address}\n{e}");
                throw;
            }
        }

        public UniTask<ISceneHandle> ChangeScene(string address, bool additive, CancellationToken token)
        {
            return LoadSceneAsync(address, additive, true, token);
        }

        public void SetActiveScene(ISceneHandle sceneHandle)
        {
            SceneManager.SetActiveScene(sceneHandle.Result);
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}