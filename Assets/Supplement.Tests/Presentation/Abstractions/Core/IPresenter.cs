using System;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Presentation.Abstractions
{
    public interface IPresenter : IDisposable
    {
        UniTask RenderAsync(IView view);
    }
}