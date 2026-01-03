using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Application.Abstractions
{
    public interface IItemService
    {
        UniTask GrantDummyItemsAsync(CancellationToken token);
        ItemDto GetById(int itemId);
        IReadOnlyList<ItemDto> GetAll();
        UniTask AddAmountAsync(int itemId, int amount, CancellationToken token);
        UniTask SubtractAmountAsync(int itemId, int amount, CancellationToken token);
    }
}