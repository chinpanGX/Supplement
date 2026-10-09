using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// エンティティをメモリに持ち、DTOのリストに変換して<see cref="IFileStorageService"/>で1つのファイルに保存する。
    /// 継承して、ファイルのキー・パスワード・変換・エンティティの登録の仕方を決める。
    /// </summary>
    /// <typeparam name="TKey">エンティティを引くキーの型。</typeparam>
    /// <typeparam name="TEntity">メモリに持つエンティティの型。</typeparam>
    /// <typeparam name="TDto">ファイルに保存する形の型。</typeparam>
    public abstract class SaveDataRepository<TKey, TEntity, TDto> : ISaveDataRepository, IBulkUpdater<TEntity>
    {
        /// <summary>
        /// メモリに持つエンティティ。<see cref="SaveAsync"/>でこの内容をすべて保存する。
        /// </summary>
        protected readonly Dictionary<TKey, TEntity> Entities = new();

        private readonly IFileStorageService fileStorageService;
        // DeleteAsyncのたびに進める。読み込みの途中で削除されたら、読み込んだ内容をメモリに反映しない。
        private int deleteGeneration;

        /// <summary>
        /// 保存に使う<see cref="IFileStorageService"/>を指定して作る。
        /// </summary>
        /// <param name="fileStorageService">ファイルを読み書きする。</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileStorageService"/>がnull。</exception>
        protected SaveDataRepository(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        }

        /// <summary>
        /// 保存するファイルのキー。
        /// </summary>
        protected abstract string FileKey { get; }

        /// <summary>
        /// ファイルの暗号化に使うパスワード。
        /// </summary>
        protected abstract string Password { get; }

        /// <summary>
        /// <paramref name="entities"/>を<see cref="UpdateCore"/>でメモリに反映し、すべてを保存する。
        /// </summary>
        /// <param name="entities">更新するエンティティ。</param>
        public UniTask UpdateAsync(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                UpdateCore(entity);
            }

            return SaveAsync();
        }

        /// <inheritdoc/>
        public bool IsCreated => fileStorageService.Exists(FileKey);

        /// <summary>
        /// 保存データを読み込み、メモリのエンティティを置き換える。保存データが無ければ何もしない。
        /// 待っていない書き込み・削除があれば、その後のファイルを読む。読み込みの途中で<see cref="DeleteAsync"/>を呼ぶと、
        /// 読み込んだ内容は反映しない。
        /// </summary>
        /// <exception cref="SaveDataRepositoryException">
        /// エンティティに変換できないデータがあった。このときメモリのエンティティは変えない。
        /// </exception>
        public async UniTask LoadAsync()
        {
            var generation = deleteGeneration;

            // 先にIsCreatedを見ると、待っていない書き込み・削除より前の状態で判断してしまう。
            // 読み込みは同じファイルへの操作の順番を待つので、その時点でファイルが無ければ保存データが無いとみなす。
            List<TDto> dtos;
            try
            {
                dtos = await fileStorageService.ReadAsync<List<TDto>>(FileKey, Password);
            }
            catch (FileNotFoundException)
            {
                return;
            }

            if (generation != deleteGeneration)
            {
                return;
            }

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

        /// <summary>
        /// メモリのエンティティをすべてDTOに変換して保存する。保存先のディレクトリが無ければ作る。
        /// </summary>
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

        /// <summary>
        /// 保存データを削除し、メモリのエンティティも消す。読み込み中の<see cref="LoadAsync"/>があれば、その結果は反映しない。
        /// </summary>
        public UniTask DeleteAsync()
        {
            // 手元に残すと、次のSaveAsyncで削除したはずのデータを書き戻してしまう。
            Entities.Clear();
            deleteGeneration++;
            return fileStorageService.DeleteFileAsync(FileKey);
        }

        /// <summary>
        /// 保存のために、エンティティをDTOに変換する。
        /// </summary>
        /// <param name="entity">変換するエンティティ。</param>
        protected abstract TDto ConvertToDto(TEntity entity);

        /// <summary>
        /// 読み込んだDTOをエンティティに変換する。不正なデータなら例外を投げる。
        /// </summary>
        /// <param name="dto">変換するDTO。</param>
        protected abstract TEntity ConvertToEntity(TDto dto);

        /// <summary>
        /// エンティティを<see cref="Entities"/>に追加、またはキーが同じものと置き換える。
        /// </summary>
        /// <param name="entity">反映するエンティティ。</param>
        protected abstract void UpdateCore(TEntity entity);
    }
}