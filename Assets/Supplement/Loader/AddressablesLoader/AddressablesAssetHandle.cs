using System;
using Supplement.Loader.Abstractions;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Supplement.Loader.AddressablesLoader
{
    /// <summary>
    /// Addressablesで読み込んだアセットへのハンドル。破棄すると<c>Addressables.Release</c>で解放する。
    /// </summary>
    /// <typeparam name="T">アセットの型。</typeparam>
    public sealed class AddressablesAssetHandle<T> : IAssetHandle<T> where T : Object
    {

        private bool disposed;

        internal AddressablesAssetHandle(AsyncOperationHandle<T> handle)
        {
            Handle = handle;
            disposed = false;
        }
        private AsyncOperationHandle<T> Handle { get; }
        /// <inheritdoc/>
        public bool IsDone => Handle.IsDone;

        /// <inheritdoc/>
        public bool Succeeded => Handle.Status == AsyncOperationStatus.Succeeded;

        /// <inheritdoc/>
        public T Result => disposed
            ? throw new ObjectDisposedException(nameof(AddressablesAssetHandle<T>), "It has already been destroyed.")
            : Handle.Result;

        /// <summary>
        /// アセットを解放する。2回目以降は何もしない。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (Handle.IsValid())
            {
                Addressables.Release(Handle);
            }
            disposed = true;
        }
    }
}