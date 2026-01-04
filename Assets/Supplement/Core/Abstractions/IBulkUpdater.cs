using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    public interface IBulkUpdater<TEntity>
    {
        UniTask UpdateAsync(List<TEntity> entities, CancellationToken token);
    }
}