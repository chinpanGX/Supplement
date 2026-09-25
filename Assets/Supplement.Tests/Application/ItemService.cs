using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Domain;

namespace Supplement.Tests.Application
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository itemRepository;

        public ItemService(IItemRepository itemRepository)
        {
            this.itemRepository = itemRepository;
        }

        public async UniTask GrantDummyItemsAsync()
        {
            itemRepository.Begin();
            for (var i = 1; i <= 10; i++)
            {
                await itemRepository.UpdateAsync(new ItemEntity(1000 + i, 10));
            }
            await itemRepository.CommitAsync();
        }
        
        public ItemDto GetById(int itemId)
        {
            var entity = itemRepository.GetById(itemId);
            return new ItemDto(entity.Id, entity.Amount);
        }
        
        public IReadOnlyList<ItemDto> GetAll()
        {
            var entities = itemRepository.GetAll();
            var dtos = new List<ItemDto>(entities.Count);
            foreach (var entity in entities)
            {
                dtos.Add(new ItemDto(entity.Id, entity.Amount));
            }
            return dtos;
        }
        
        public UniTask AddAmountAsync(int itemId, int amount)
        {
            var entity = itemRepository.GetById(itemId);
            var updatedEntity = new ItemEntity(entity.Id, entity.Amount + amount);
            return itemRepository.UpdateAsync(updatedEntity);
        }

        public UniTask SubtractAmountAsync(int itemId, int amount)
        {
            var entity = itemRepository.GetById(itemId);
            var updatedEntity = new ItemEntity(entity.Id, entity.Amount - amount);
            return itemRepository.UpdateAsync(updatedEntity);
        }
    }
}