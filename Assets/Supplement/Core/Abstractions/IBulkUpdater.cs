using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// 複数のエンティティをまとめて更新する。
    /// </summary>
    /// <typeparam name="TEntity">更新するエンティティの型。</typeparam>
    public interface IBulkUpdater<TEntity>
    {
        /// <summary>
        /// <paramref name="entities"/>をまとめて更新する。
        /// </summary>
        /// <param name="entities">更新するエンティティ。</param>
        UniTask UpdateAsync(List<TEntity> entities);
    }
}