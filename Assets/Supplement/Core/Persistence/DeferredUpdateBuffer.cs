using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// 更新するエンティティをためておき、<see cref="CommitAsync"/>で<see cref="IBulkUpdater{TEntity}"/>にまとめて渡す。
    /// </summary>
    /// <remarks>
    /// <see cref="Begin"/>で始め、<see cref="Add"/>でため、<see cref="CommitAsync"/>で更新するか<see cref="Rollback"/>で捨てる。
    /// </remarks>
    /// <typeparam name="TEntity">エンティティの型。</typeparam>
    public class DeferredUpdateBuffer<TEntity>
    {
        private readonly IBulkUpdater<TEntity> bulkUpdater;
        private List<TEntity> pendingToUpdate = new();
        // コミット中でないときに使い回す、もう1つのバッファ。コミットのたびにリストを確保しないため、
        // 保存を待つ間は切り離したバッファと入れ替えて使う。コミットが重なったときだけ新しく確保する。
        private List<TEntity> spareBuffer = new();

        /// <summary>
        /// ためたエンティティを渡す先を指定して作る。
        /// </summary>
        /// <param name="bulkUpdater">コミットでエンティティをまとめて渡す先。</param>
        public DeferredUpdateBuffer(IBulkUpdater<TEntity> bulkUpdater)
        {
            this.bulkUpdater = bulkUpdater;
        }

        /// <summary>
        /// <see cref="Begin"/>してから<see cref="CommitAsync"/>か<see cref="Rollback"/>するまでの間かどうか。
        /// </summary>
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
        /// 更新対象のEntityをバッファに追加します。
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
        /// バッファに積まれているEntityを一括で更新します。
        /// </summary>
        public async UniTask CommitAsync()
        {
            if (!IsActive)
            {
                throw new DeferredUpdateBufferException(
                    "DeferredUpdateBuffer is not active. Call Begin() before CommitAsync()."
                );
            }

            // 書き込みの完了を待つ間に次のBegin/Addが来ても混ざらないよう、await前にバッファを切り離す。
            // 書き込みに失敗した分も残さず破棄し、次のコミットで意図せず保存されないようにする。
            var entities = pendingToUpdate;
            pendingToUpdate = spareBuffer ?? new List<TEntity>();
            spareBuffer = null;
            IsActive = false;
            try
            {
                await bulkUpdater.UpdateAsync(entities);
            }
            finally
            {
                entities.Clear();
                spareBuffer ??= entities;
            }
        }

        /// <summary>
        /// バッファに積まれているEntityを破棄します。
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