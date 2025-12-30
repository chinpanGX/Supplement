using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Tests.Domain
{
    public interface IPlayerRepository
    {
        PlayerEntity GetById(string uniqueId);
        void Begin();
        UniTask CommitAsync(CancellationToken token);
        void Rollback();
        UniTask UpdateAsync(PlayerEntity entity, CancellationToken token);
    }
}