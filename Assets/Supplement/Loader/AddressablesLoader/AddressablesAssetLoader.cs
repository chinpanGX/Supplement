using System;
using System.Collections.Generic;
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

        public async UniTask<IReadOnlyList<IAssetHandle<T>>> LoadAssetsByLabelAsync<T>(string label,
            CancellationToken token) where T : Object
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentNullException(nameof(label));
            }

            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
                var locations = await locationsHandle.ToUniTask(cancellationToken: token);
                Addressables.Release(locationsHandle);

                if (locations.Count == 0)
                {
                    return Array.Empty<IAssetHandle<T>>();
                }

                var handles = new AsyncOperationHandle<T>[locations.Count];
                for (var i = 0; i < locations.Count; i++)
                {
                    handles[i] = Addressables.LoadAssetAsync<T>(locations[i]);
                }

                try
                {
                    await UniTask.WhenAll(Array.ConvertAll(handles,
                        h => h.ToUniTask(cancellationToken: token)));
                }
                catch
                {
                    foreach (var handle in handles)
                    {
                        if (handle.IsValid())
                        {
                            Addressables.Release(handle);
                        }
                    }
                    throw;
                }

                var results = new List<IAssetHandle<T>>(handles.Length);
                foreach (var handle in handles)
                {
                    if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        throw new AssetLoadFailedException($"Failed to load asset. label: {label}");
                    }
                    results.Add(new AddressablesAssetHandle<T>(handle));
                }
                return results;
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning($"LoadAssetsByLabelAsync was canceled. label: {label}\n{e}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load assets by label. label: {label}\n{e}");
                throw;
            }
        }

        public async UniTask<ISceneHandle> LoadSceneAsync(string address, bool additive, bool activateOnLoad,
            CancellationToken token, IProgress<float> progress = null)
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
                await handle.ToUniTask(progress, cancellationToken: token);
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