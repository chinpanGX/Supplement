using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using VContainer;

namespace Supplement.Tests.EditMode
{
    internal class RepositoriesCache
    {
        private IReadOnlyList<ISaveDataRepository> Repositories { get; }

        [Inject]
        public RepositoriesCache(IReadOnlyList<ISaveDataRepository> repositories)
        {
            Repositories = repositories;
        }

        public async UniTask LoadAllAsync()
        {
            foreach (var repo in Repositories)
            {
                await repo.LoadAsync();
            }
        }
        
        public async UniTask DeleteAllAsync()
        {
            foreach (var repo in Repositories)
            {
                await repo.DeleteAsync();
            }
        }
    }
}