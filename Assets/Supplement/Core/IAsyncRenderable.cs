using Cysharp.Threading.Tasks;

namespace Supplement.Unity
{
    public interface IAsyncRenderable
    {
        
    }
    
    public interface IAsyncRenderable<T>
    {
        UniTask RenderAsync(T dto);
    }
}