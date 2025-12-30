using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core.Abstractions;

namespace Supplement.Core
{
    public class DeferredUpdateBuffer<TEntity>
    {
        private readonly IBulkUpdater<TEntity> bulkUpdater;
        private readonly List<TEntity> pendingToUpdate = new();

        public DeferredUpdateBuffer(IBulkUpdater<TEntity> bulkUpdater)
        {
            this.bulkUpdater = bulkUpdater;
        }

        public bool IsActive { get; private set; }

        /// <summary>
        /// DeferredUpdateBuffer をアクティブな状態にし、操作を開始します。
        /// Begin メソッドを呼び出すことで、Add メソッドまたは UpdateAsync メソッドなどのその他の操作を安全に実行できるようになります。
        /// このメソッドが呼び出される前に操作を行おうとすると例外が発生します。
        /// </summary>
        public void Begin()
        {
            if (IsActive)
            {
                throw new DeferredUpdateBufferException("DeferredUpdateBuffer is already active.");
            }

            IsActive = true;
        }

        /// <summary>
        ///     更新対象のEntityをバッファに追加します。
        /// </summary>
        public void Add(TEntity entity)
        {
            if (!IsActive)
            {
                throw new DeferredUpdateBufferException("DeferredUpdateBuffer is not active. Call Begin() before Add()."
                );
            }

            pendingToUpdate.Add(entity);
        }

        /// <summary>
        ///     バッファに積まれているEntityを一括で更新します。
        /// </summary>
        public async UniTask CommitAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!IsActive)
            {
                throw new DeferredUpdateBufferException(
                    "DeferredUpdateBuffer is not active. Call Begin() before CommitAsync()."
                );
            }

            IsActive = false;
            await bulkUpdater.UpdateAsync(pendingToUpdate, token);
            pendingToUpdate.Clear();
        }

        /// <summary>
        ///     バッファに積まれているEntityを破棄します。
        /// </summary>
        public void Rollback()
        {
            if (!IsActive)
            {
                throw new DeferredUpdateBufferException(
                    "DeferredUpdateBuffer is not active. Call Begin() before Rollback()."
                );
            }

            IsActive = false;
            pendingToUpdate.Clear();
        }
    }
}