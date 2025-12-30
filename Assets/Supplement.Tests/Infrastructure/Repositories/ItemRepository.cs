using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using Supplement.Core.Abstractions;
using Supplement.Tests.Domain;

namespace Supplement.Tests.Infrastructure
{
    public class ItemRepository : SaveDataRepository<int, ItemEntity, ItemDto>,
        IItemRepository
    {
        private readonly DeferredUpdateBuffer<ItemEntity> deferredUpdateBuffer;
        public ItemRepository(IFileStorageService fileStorageService) : base(fileStorageService)
        {
            deferredUpdateBuffer = new DeferredUpdateBuffer<ItemEntity>(this);
        }

        protected override string FileKey => "itemData";
        protected override string Password => "it3m$ecr3t!";

        public ItemEntity GetById(int id)
        {
            if (!Entities.TryGetValue(id, out var entity))
            {
                throw new EntityNotFoundException(nameof(ItemEntity), id);
            }

            return entity;
        }

        public void Begin()
        {
            deferredUpdateBuffer.Begin();
        }

        public ValueTask CommitAsync(CancellationToken token)
        {
            return deferredUpdateBuffer.CommitAsync(token);
        }

        public void Rollback()
        {
            deferredUpdateBuffer.Rollback();
        }

        public async UniTask UpdateAsync(ItemEntity entity, CancellationToken token)
        {
            if (deferredUpdateBuffer.IsActive)
            {
                deferredUpdateBuffer.Add(entity);
            }
            else
            {
                token.ThrowIfCancellationRequested();
                UpdateCore(entity);
                await SaveAsync(token);
            }
        }
        public void Clear()
        {
            Entities.Clear();
        }

        protected override ItemDto ConvertToDto(ItemEntity entity)
        {
            var dto = new ItemDto
            {
                ItemId = entity.Id,
                Amount = entity.Amount
            };
            return dto;
        }

        protected override ItemEntity ConvertToEntity(ItemDto dto)
        {
            var entity = new ItemEntity(dto.ItemId, dto.Amount);
            return entity;
        }

        protected override void UpdateCore(ItemEntity entity)
        {
            Entities[entity.Id] = entity;
        }
    }
}