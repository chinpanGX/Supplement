using System;
using System.Collections;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core.Abstractions;
using VContainer;
using Supplement.Tests.Domain;
using Supplement.Tests.Infrastructure;
using UnityEngine.TestTools;

namespace Supplement.Tests
{
    public class TestSaveData
    {
        private IPlayerRepository playerRepository;
        private IItemRepository itemRepository;
        private RepositoriesCache repositoriesCache;
        private CancellationTokenSource cts;

        [UnityTest]
        public IEnumerator TestSaveAndLoad()
        {
            var builder = new ContainerBuilder();
            builder.RegisterJsonFileStorage();
            RegisterDependencies(builder);
            cts?.Cancel();
            cts = new CancellationTokenSource();
            yield return RunTestCode().ToCoroutine();
        }
        
        [UnityTest]
        public IEnumerator TestEncryptedSaveAndLoad()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEncryptedFileStorage();
            RegisterDependencies(builder);
            cts?.Cancel();
            cts = new CancellationTokenSource();
            yield return RunTestCode().ToCoroutine();
        }
        
        [TearDown]
        public void AfterTest()
        {
            repositoriesCache.DeleteAll();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, "SupplementTest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            cts?.Cancel();
            cts?.Dispose();
        }
        
        private void RegisterDependencies(ContainerBuilder builder)
        {
            builder.Register<PlayerRepository>(Lifetime.Singleton).As<IPlayerRepository, ISaveDataRepository>();
            builder.Register<ItemRepository>(Lifetime.Singleton).As<IItemRepository, ISaveDataRepository>();
            builder.Register<RepositoriesCache>(Lifetime.Singleton);

            builder.RegisterBuildCallback(resolver =>
            {
                playerRepository = resolver.Resolve<IPlayerRepository>();
                itemRepository = resolver.Resolve<IItemRepository>();
                repositoriesCache = resolver.Resolve<RepositoriesCache>();
                resolver.Resolve<IFileStorageService>().SetDirectoryName("SupplementTest");
            });
            
            builder.Build();
        }
        
        private async UniTask RunTestCode()
        {
            await repositoriesCache.LoadAllAsync(cts.Token);
            var newPlayer = new PlayerEntity("player_001", 1);
            await playerRepository.UpdateAsync(newPlayer, cts.Token);
            var loadedPlayer = playerRepository.GetById("player_001");
            Assert.AreEqual(1, loadedPlayer.Level);
            try
            {
                playerRepository.Begin();
                itemRepository.Begin();
                    
                var updatedPlayer = new PlayerEntity("player_001", 10);
                await playerRepository.UpdateAsync(updatedPlayer, cts.Token);
                    
                var newItem = new ItemEntity(1001, 10);
                await itemRepository.UpdateAsync(newItem, cts.Token);

                await itemRepository.CommitAsync(cts.Token);
                await playerRepository.CommitAsync(cts.Token);
            }
            catch (Exception e)
            {
                itemRepository.Rollback();
                playerRepository.Rollback();
            }

            {
                var reloadedPlayer = playerRepository.GetById("player_001");
                Assert.AreEqual(10, reloadedPlayer.Level);
                var loadedItem = itemRepository.GetById(1001);
                Assert.AreEqual(10, loadedItem.Amount);
            }

            {
                itemRepository.Clear();
                await repositoriesCache.LoadAllAsync(cts.Token);
                    
                var reloadedPlayer = playerRepository.GetById("player_001");
                Assert.AreEqual(10, reloadedPlayer.Level);
                var reloadedItem = itemRepository.GetById(1001);
                Assert.AreEqual(10, reloadedItem.Amount);    
            }
        }
    }
}