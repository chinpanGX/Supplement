using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core.Abstractions
{
    public interface IBulkUpdater<TEntity>
    {
        UniTask UpdateAsync(List<TEntity> entities, CancellationToken token);
    }
}