using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;

namespace Supplement.Unity
{
    public interface IAsyncRenderable<T> : IRenderable
    {
        UniTask RenderAsync(T dto, CancellationToken token);
    }
}