using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Presentation.Abstractions
{
    public interface ISampleItemListPresenter : IPresenter
    {
        void Pop();
        UniTask WaitForPopCompletionAsync();
        void AddItemAmount(int itemId);
        void SubtractItemAmount(int itemId);
    }
}