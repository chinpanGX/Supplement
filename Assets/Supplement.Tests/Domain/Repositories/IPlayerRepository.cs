using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Domain
{
    public interface IPlayerRepository
    {
        PlayerEntity GetById(string uniqueId);
        void Begin();
        UniTask CommitAsync();
        void Rollback();
        UniTask UpdateAsync(PlayerEntity entity);
    }
}