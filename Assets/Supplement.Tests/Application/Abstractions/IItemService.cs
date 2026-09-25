using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Application.Abstractions
{
    public interface IItemService
    {
        UniTask GrantDummyItemsAsync();
        ItemDto GetById(int itemId);
        IReadOnlyList<ItemDto> GetAll();
        UniTask AddAmountAsync(int itemId, int amount);
        UniTask SubtractAmountAsync(int itemId, int amount);
    }
}