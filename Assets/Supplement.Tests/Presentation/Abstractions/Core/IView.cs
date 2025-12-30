using System;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Presentation.Abstractions
{
    public interface IView : IDisposable
    {
        UniTask RenderAsync(ViewDto dto);
    }
}