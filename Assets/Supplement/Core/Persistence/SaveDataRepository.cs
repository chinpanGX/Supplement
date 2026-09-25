using System;
using System.Collections.Generic;
using System.Text;
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

        public UniTask UpdateAsync(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                UpdateCore(entity);
            }

            return SaveAsync();
        }

        public bool IsCreated => fileStorageService.Exists(FileKey);

        public async UniTask LoadAsync()
        {
            if (!IsCreated)
            {
                return;
            }

            var dtos = await fileStorageService.ReadAsync<List<TDto>>(FileKey, Password);

            var entities = new List<TEntity>(dtos.Count);
            var sb = new StringBuilder();
            List<Exception> exceptions = null;

            foreach (var dto in dtos)
            {
                try
                {
                    entities.Add(ConvertToEntity(dto));
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);

                    sb.AppendFormat(
                        "[{0}] {1} ({2}::ConvertToEntity)",
                        e.GetType().Name,
                        e.Message,
                        GetType().Name
                    );
                    sb.AppendLine();
                }
            }

            if (exceptions is not null)
            {
                throw new SaveDataRepositoryException(
                    $"Data corruption detected in {GetType().Name}{Environment.NewLine}{sb}",
                    new AggregateException(exceptions)
                );
            }

            Entities.Clear();
            foreach (var entity in entities)
            {
                UpdateCore(entity);
            }
        }

        public async UniTask SaveAsync()
        {
            fileStorageService.CreateDirectoryIfNotExists(FileKey);

            var dtos = new List<TDto>(Entities.Count);
            foreach (var entity in Entities.Values)
            {
                dtos.Add(ConvertToDto(entity));
            }

            await fileStorageService.WriteAsync(FileKey, dtos, Password);
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