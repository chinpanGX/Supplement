using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Domain
{
    public interface IItemRepository
    {
        ItemEntity GetById(int id);
        IReadOnlyList<ItemEntity> GetAll();
        void Begin();
        ValueTask CommitAsync();
        void Rollback();
        UniTask UpdateAsync(ItemEntity entity);
        void Clear();
    }
}