using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using Supplement.Core.Abstractions;
using Supplement.Tests.Domain;

namespace Supplement.Tests.Infrastructure
{
    public class PlayerRepository : SaveDataRepository<string, PlayerEntity, PlayerDto>,
        IPlayerRepository
    {
        private readonly DeferredUpdateBuffer<PlayerEntity> deferredUpdateBuffer;

        public PlayerRepository(IFileStorageService fileStorageService) : base(fileStorageService)
        {
            deferredUpdateBuffer = new DeferredUpdateBuffer<PlayerEntity>(this);
        }

        protected override string FileKey { get; } = "playerData";
        protected override string Password { get; } = "hsu%6qi23j";

        public PlayerEntity GetById(string uniqueId)
        {
            if (!Entities.TryGetValue(uniqueId, out var entity))
            {
                throw new EntityNotFoundException(nameof(PlayerEntity), uniqueId);
            }
            return entity;
        }

        public void Begin()
        {
            deferredUpdateBuffer.Begin();
        }

        public UniTask CommitAsync(CancellationToken token)
        {
            return deferredUpdateBuffer.CommitAsync(token);
        }

        public void Rollback()
        {
            deferredUpdateBuffer.Rollback();
        }

        public async UniTask UpdateAsync(PlayerEntity entity, CancellationToken token)
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

        protected override PlayerDto ConvertToDto(PlayerEntity entity)
        {
            var dto = new PlayerDto
            {
                UniqueId = entity.UniqueId,
                Level = entity.Level
            };
            return dto;
        }

        protected override PlayerEntity ConvertToEntity(PlayerDto dto)
        {
            var entity = new PlayerEntity(dto.UniqueId, dto.Level);
            return entity;
        }

        protected override void UpdateCore(PlayerEntity entity)
        {
            Entities[entity.UniqueId] = entity;
        }
    }
}