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
    /// <summary>
    /// Addressablesでアセットとシーンを読み込む。
    /// </summary>
    public class AddressablesAssetLoader : IAssetLoader, ISceneLoader
    {
        /// <inheritdoc/>
        public async UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string address, CancellationToken token)
            where T : Object
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            try
            {
                await handle.ToUniTask(cancellationToken: token);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new AssetLoadFailedException($"Failed to load asset. address: {address}");
                }
                return new AddressablesAssetHandle<T>(handle);
            }
            catch (OperationCanceledException e)
            {
                ReleaseIfValid(handle);
                Debug.LogWarning($"LoadAssetAsync was canceled. address : {address}\n{e}");
                throw;
            }
            catch (Exception e)
            {
                ReleaseIfValid(handle);
                Debug.LogError($"Failed to load asset. address: {address}\n{e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async UniTask<IReadOnlyList<IAssetHandle<T>>> LoadAssetsByLabelAsync<T>(string label,
            CancellationToken token) where T : Object
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentNullException(nameof(label));
            }

            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            AsyncOperationHandle<T>[] handles = null;
            try
            {
                var locations = await locationsHandle.ToUniTask(cancellationToken: token);
                if (locations.Count == 0)
                {
                    return Array.Empty<IAssetHandle<T>>();
                }

                handles = new AsyncOperationHandle<T>[locations.Count];
                for (var i = 0; i < locations.Count; i++)
                {
                    handles[i] = Addressables.LoadAssetAsync<T>(locations[i]);
                }

                // 1つでも失敗・取り消しされるとWhenAllが例外を投げ、catchで読み込み済みのものも含めて全件を解放する。
                await UniTask.WhenAll(Array.ConvertAll(handles,
                    h => h.ToUniTask(cancellationToken: token)));

                var results = new List<IAssetHandle<T>>(handles.Length);
                foreach (var handle in handles)
                {
                    results.Add(new AddressablesAssetHandle<T>(handle));
                }
                return results;
            }
            catch (OperationCanceledException e)
            {
                ReleaseAllIfValid(handles);
                Debug.LogWarning($"LoadAssetsByLabelAsync was canceled. label: {label}\n{e}");
                throw;
            }
            catch (Exception e)
            {
                ReleaseAllIfValid(handles);
                Debug.LogError($"Failed to load assets by label. label: {label}\n{e}");
                throw;
            }
            finally
            {
                ReleaseIfValid(locationsHandle);
            }
        }

        /// <inheritdoc/>
        public async UniTask<ISceneHandle> LoadSceneAsync(string address, bool additive, bool activateOnLoad,
            CancellationToken token, IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (token.IsCancellationRequested)
            {
                Debug.LogWarning($"LoadSceneAsync was canceled before loading. address: {address}");
                token.ThrowIfCancellationRequested();
            }

            var handle = Addressables.LoadSceneAsync(address,
                additive ? LoadSceneMode.Additive : LoadSceneMode.Single,
                activateOnLoad
            );
            try
            {
                // シーンの読み込みは途中で止められない。取り消しても読み込みは進み、Singleなら今のシーンが入れ替わり、
                // activateOnLoad: falseならアクティブ化待ちのまま残って後のシーン読み込みをすべて止めてしまう。
                // そのため開始した後は取り消しを受け付けず、読み終えたハンドルを返して破棄を呼び出し側に任せる。
                await handle.ToUniTask(progress);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new AssetLoadFailedException($"Failed to load scene. address: {address}");
                }

                return new AddressablesSceneHandle(handle, activateOnLoad);
            }
            catch (Exception e)
            {
                ReleaseIfValid(handle);
                Debug.LogError($"Failed to load scene. address: {address}\n{e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public UniTask<ISceneHandle> ChangeScene(string address, bool additive, CancellationToken token)
        {
            return LoadSceneAsync(address, additive, true, token);
        }

        /// <inheritdoc/>
        public void SetActiveScene(ISceneHandle sceneHandle)
        {
            SceneManager.SetActiveScene(sceneHandle.Result);
        }

        /// <inheritdoc/>
        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        private static void ReleaseIfValid(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        private static void ReleaseAllIfValid<T>(AsyncOperationHandle<T>[] handles)
        {
            if (handles == null)
            {
                return;
            }

            foreach (var handle in handles)
            {
                ReleaseIfValid(handle);
            }
        }
    }
}