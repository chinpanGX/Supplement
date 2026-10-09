using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using Supplement.Tests.Domain;
using Supplement.Tests.Infrastructure;
using Supplement.Unity;
using UnityEngine.TestTools;
using VContainer;

namespace Supplement.Tests.EditMode
{
    public class TestSaveDataDelete
    {
        private const string DirectoryName = "SupplementTestSaveDataDelete";

        private IFileStorageService fileStorageService;
        private PlayerRepository playerRepository;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEncryptedFileStorage();
            builder.Register<PlayerRepository>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver =>
            {
                fileStorageService = resolver.Resolve<IFileStorageService>();
                fileStorageService.SetDirectoryName(DirectoryName);
                playerRepository = resolver.Resolve<PlayerRepository>();
            });
            builder.Build();
        }

        [TearDown]
        public void TearDown()
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, DirectoryName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        [UnityTest]
        public IEnumerator DeleteClearsEntitiesSoNextSaveDoesNotRestoreThem() => UniTask.ToCoroutine(async () =>
        {
            await playerRepository.UpdateAsync(new PlayerEntity("player_001", 1));

            await playerRepository.DeleteAsync();
            await playerRepository.UpdateAsync(new PlayerEntity("player_002", 1));
            await playerRepository.LoadAsync();

            Assert.Throws<EntityNotFoundException>(() => playerRepository.GetById("player_001"));
            Assert.AreEqual(1, playerRepository.GetById("player_002").Level);
        });

        [UnityTest]
        public IEnumerator DeleteDuringLoadDoesNotRestoreEntities() => UniTask.ToCoroutine(async () =>
        {
            await playerRepository.UpdateAsync(new PlayerEntity("player_001", 1));

            // 読み込みの完了より先に削除する
            var load = playerRepository.LoadAsync();
            var delete = playerRepository.DeleteAsync();
            await UniTask.WhenAll(load, delete);

            Assert.Throws<EntityNotFoundException>(() => playerRepository.GetById("player_001"));
            Assert.IsFalse(playerRepository.IsCreated);
        });

        [UnityTest]
        public IEnumerator LoadWaitsForUnawaitedSaveAndDelete() => UniTask.ToCoroutine(async () =>
        {
            // 同じファイルを別のリポジトリから読み、メモリではなくファイルの内容を確かめる
            var otherRepository = new PlayerRepository(fileStorageService);

            var save = playerRepository.UpdateAsync(new PlayerEntity("player_001", 2));
            await otherRepository.LoadAsync();
            await save;
            Assert.AreEqual(2, otherRepository.GetById("player_001").Level);

            var delete = playerRepository.DeleteAsync();
            await otherRepository.LoadAsync();
            await delete;
            Assert.AreEqual(2, otherRepository.GetById("player_001").Level);
        });

        [UnityTest]
        public IEnumerator OperationsOnSameFileRunInCallOrder() => UniTask.ToCoroutine(async () =>
        {
            const string fileKey = "orderTest";
            const string password = "test-password";
            fileStorageService.CreateDirectoryIfNotExists(fileKey);
            await fileStorageService.WriteAsync(fileKey, "first", password);

            // 待たずに続けて呼んでも、削除の後に書き込みが行われる
            var delete = fileStorageService.DeleteFileAsync(fileKey);
            var write = fileStorageService.WriteAsync(fileKey, "second", password);
            await UniTask.WhenAll(delete, write);

            Assert.AreEqual("second", await fileStorageService.ReadAsync<string>(fileKey, password));
        });
    }
}
