using System;
using Object = UnityEngine.Object;

namespace Supplement.Loader.Abstractions
{
    public interface IAssetHandle<T> : IDisposable where T : Object
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        T Result { get; }
    }
}