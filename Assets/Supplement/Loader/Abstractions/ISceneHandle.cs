using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Supplement.Loader.Abstractions
{
    public interface ISceneHandle : IDisposable
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        Scene Result { get; }
        ValueTask ActivateAsync(CancellationToken token);
    }
}