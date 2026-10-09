using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Supplement.Loader.Abstractions;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Supplement.Loader.AddressablesLoader
{
    /// <summary>
    /// Addressablesで読み込んだシーンへのハンドル。破棄すると<c>Addressables.UnloadSceneAsync</c>でアンロードする。
    /// </summary>
    public sealed class AddressablesSceneHandle : ISceneHandle
    {

        private bool activateOnLoad;

        private bool disposed;

        internal AddressablesSceneHandle(AsyncOperationHandle<SceneInstance> handle, bool activateOnLoad)
        {
            this.activateOnLoad = activateOnLoad;
            Handle = handle;
            disposed = false;
        }
        private AsyncOperationHandle<SceneInstance> Handle { get; }
        /// <inheritdoc/>
        public bool IsDone => Handle.IsDone;

        /// <inheritdoc/>
        public bool Succeeded => Handle.Status == AsyncOperationStatus.Succeeded;

        /// <inheritdoc/>
        public Scene Result
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(AddressablesSceneHandle));
                }
                return Handle.Result.Scene;
            }
        }

        /// <inheritdoc/>
        public async ValueTask ActivateAsync(CancellationToken token)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AddressablesSceneHandle));
            }
            if (activateOnLoad)
            {
                throw new InvalidOperationException("Scene is already set to activate on load.");
            }
            activateOnLoad = true;
            await Handle.Result.ActivateAsync().ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// シーンのアンロードを始める。アンロードの完了は待たない。2回目以降は何もしない。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (Handle.IsValid())
            {
                Addressables.UnloadSceneAsync(Handle);
            }
            disposed = true;
        }
    }
}