using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Domain
{
    public interface IItemRepository
    {
        ItemEntity GetById(int id);
        void Begin();
        ValueTask CommitAsync(CancellationToken token);
        void Rollback();
        UniTask UpdateAsync(ItemEntity entity, CancellationToken token);
        void Clear();
    }
}