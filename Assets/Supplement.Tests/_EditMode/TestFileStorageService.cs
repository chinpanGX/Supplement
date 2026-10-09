using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using Supplement.Unity;
using UnityEngine.TestTools;
using VContainer;

namespace Supplement.Tests.EditMode
{
    public class TestFileStorageService
    {
        private const string DirectoryName = "SupplementTestFileStorageService";

        [UnityTest]
        public IEnumerator WriteAsyncRemovesStaleTempFileFromPreviousCrash()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEncryptedFileStorage();

            IFileStorageService fileStorageService = null;
            builder.RegisterBuildCallback(resolver =>
            {
                fileStorageService = resolver.Resolve<IFileStorageService>();
                fileStorageService.SetDirectoryName(DirectoryName);
            });
            builder.Build();

            yield return RunTestCode(fileStorageService).ToCoroutine();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, DirectoryName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private async UniTask RunTestCode(IFileStorageService fileStorageService)
        {
            const string fileKey = "staleTempFileTest";
            const string password = "test-password";

            fileStorageService.CreateDirectoryIfNotExists(fileKey);
            await fileStorageService.WriteAsync(fileKey, "first", password);

            var actualFilePath = Path.Combine(
                UnityEngine.Application.persistentDataPath,
                DirectoryName,
                EncryptedBinFileFormatProvider.GetFileName(fileKey)
            );
            var staleTempPath = $"{actualFilePath}.tmp{Guid.NewGuid():N}~";
            File.WriteAllBytes(staleTempPath, new byte[] { 0 });
            Assert.IsTrue(File.Exists(staleTempPath));

            await fileStorageService.WriteAsync(fileKey, "second", password);

            Assert.IsFalse(
                File.Exists(staleTempPath),
                "Stale temp file left over from a previous crash should be cleaned up on the next write."
            );

            var result = await fileStorageService.ReadAsync<string>(fileKey, password);
            Assert.AreEqual("second", result);
        }
    }
}
