using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    public abstract class SaveDataRepository<TKey, TEntity, TDto> : ISaveDataRepository, IBulkUpdater<TEntity>
    {
        protected readonly Dictionary<TKey, TEntity> Entities = new();

        private readonly IFileStorageService fileStorageService;

        protected SaveDataRepository(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        }

        protected abstract string FileKey { get; }

        protected abstract string Password { get; }

        public UniTask UpdateAsync(List<TEntity> entities, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            foreach (var entity in entities)
            {
                UpdateCore(entity);
            }

            return SaveAsync(token);
        }

        public bool IsCreated => fileStorageService.Exists(FileKey);

        public async UniTask LoadAsync(CancellationToken token)
        {
            if (!IsCreated)
            {
                return;
            }

            token.ThrowIfCancellationRequested();

            var dtos = await fileStorageService.ReadAsync<List<TDto>>(FileKey, Password, token);

            Entities.Clear();

            var sb = new StringBuilder();
            foreach (var dto in dtos)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var entity = ConvertToEntity(dto);
                    UpdateCore(entity);
                }
                catch (Exception e)
                {
                    sb.AppendFormat(
                        "[{0}] {1} ({2}::ConvertToEntity)",
                        e.GetType().Name,
                        e.Message,
                        GetType().Name
                    );
                    sb.AppendLine();
                }
            }

            if (sb.Length > 0)
            {
                throw new SaveDataRepositoryException(
                    $"Data corruption detected in {GetType().Name}{Environment.NewLine}{sb}"
                );
            }
        }

        public async UniTask SaveAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            fileStorageService.CreateDirectoryIfNotExists(FileKey);

            var dtos = new List<TDto>(Entities.Count);
            foreach (var entity in Entities.Values)
            {
                token.ThrowIfCancellationRequested();
                dtos.Add(ConvertToDto(entity));
            }

            await fileStorageService.WriteAsync(FileKey, dtos, Password, token);
        }

        public void Delete()
        {
            fileStorageService.DeleteFile(FileKey);
        }

        protected abstract TDto ConvertToDto(TEntity entity);

        protected abstract TEntity ConvertToEntity(TDto dto);

        protected abstract void UpdateCore(TEntity entity);
    }
}