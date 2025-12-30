using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core.Abstractions;
using VContainer;

namespace Supplement.Tests
{
    internal class RepositoriesCache
    {
        private IReadOnlyList<ISaveDataRepository> Repositories { get; }
        
        [Inject]
        public RepositoriesCache(IReadOnlyList<ISaveDataRepository> repositories)
        {
            Repositories = repositories;
        }
        
        public async UniTask LoadAllAsync(CancellationToken token)
        {
            foreach (var repo in Repositories)
            {
                await repo.LoadAsync(token);
            }
        }
        
        public void DeleteAll()
        {
            foreach (var repo in Repositories)
            {
                repo.Delete();
            }
        }
    }
}