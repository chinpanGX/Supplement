using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Loader.Abstractions
{
    public interface ISceneLoader
    {
        UniTask<ISceneHandle> LoadSceneAsync(string address, bool additive, bool activateOnLoad,
            CancellationToken token);
        UniTask<ISceneHandle> ChangeScene(string address, bool additive, CancellationToken token);
        void SetActiveScene(ISceneHandle sceneHandle);
        string GetActiveSceneName();
    }
}