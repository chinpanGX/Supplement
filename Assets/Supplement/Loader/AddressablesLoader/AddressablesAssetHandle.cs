using System;
using Supplement.Loader.Abstractions;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Supplement.Loader.AddressablesLoader
{
    public sealed class AddressablesAssetHandle<T> : IAssetHandle<T> where T : Object
    {

        private bool disposed;

        internal AddressablesAssetHandle(AsyncOperationHandle<T> handle)
        {
            Handle = handle;
            disposed = false;
        }
        private AsyncOperationHandle<T> Handle { get; }
        public bool IsDone => Handle.IsDone;
        public bool Succeeded => Handle.Status == AsyncOperationStatus.Succeeded;

        public T Result => disposed
            ? throw new ObjectDisposedException(nameof(AddressablesAssetHandle<T>), "It has already been destroyed.")
            : Handle.Result;

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AddressablesAssetHandle<T>), "It has already been destroyed.");
            }

            if (Handle.IsValid())
            {
                Addressables.Release(Handle);
            }
            disposed = true;
        }
    }
}