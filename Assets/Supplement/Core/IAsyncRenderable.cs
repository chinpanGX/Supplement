using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// DTOの内容を非同期で表示に反映する。
    /// </summary>
    /// <typeparam name="T">表示する内容を表すDTOの型。</typeparam>
    public interface IAsyncRenderable<T>
    {
        /// <summary>
        /// <paramref name="dto"/>の内容を表示に反映する。
        /// </summary>
        /// <param name="dto">表示する内容。</param>
        UniTask RenderAsync(T dto);
    }
}
