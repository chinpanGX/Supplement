using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using MessagePack;
using NUnit.Framework;
using Supplement.Core;
using Supplement.MessagePack;
using Supplement.Unity;
using UnityEngine.TestTools;
using VContainer;
// VContainerにも同じ名前の属性があるため
using KeyAttribute = MessagePack.KeyAttribute;

namespace Supplement.Tests.MessagePack
{
    [MessagePackObject]
    public sealed class SampleMissionDto
    {
        [Key(0)] public int MissionId { get; set; }
        [Key(1)] public int Progress { get; set; }
        [Key(2)] public bool IsCompleted { get; set; }
        [Key(3)] public Dictionary<string, int> Counters { get; set; }
    }

    public sealed class SampleMission
    {
        public SampleMission(int missionId, int progress)
        {
            MissionId = missionId;
            Progress = progress;
        }

        public int MissionId { get; }
        public int Progress { get; }
    }

    public sealed class SampleMissionRepository : SaveDataRepository<int, SampleMission, SampleMissionDto>
    {
        public SampleMissionRepository(IFileStorageService fileStorageService) : base(fileStorageService)
        {
        }

        protected override string FileKey => "messagePackMissions";
        protected override string Password => "mission-password";

        public SampleMission Find(int missionId) => Entities[missionId];

        protected override SampleMissionDto ConvertToDto(SampleMission entity)
        {
            return new SampleMissionDto { MissionId = entity.MissionId, Progress = entity.Progress };
        }

        protected override SampleMission ConvertToEntity(SampleMissionDto dto)
        {
            return new SampleMission(dto.MissionId, dto.Progress);
        }

        protected override void UpdateCore(SampleMission entity)
        {
            Entities[entity.MissionId] = entity;
        }
    }

    public class TestMessagePackFileStorage
    {
        private const string DirectoryName = "SupplementTestMessagePackFileStorage";
        private const string Password = "test-password";

        private IObjectResolver resolver;
        private IFileStorageService fileStorageService;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            builder.RegisterMessagePackFileStorage();
            builder.Register<SampleMissionRepository>(Lifetime.Singleton);
            resolver = builder.Build();
            fileStorageService = resolver.Resolve<IFileStorageService>();
            fileStorageService.SetDirectoryName(DirectoryName);
        }

        [TearDown]
        public void TearDown()
        {
            resolver.Dispose();
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, DirectoryName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        [UnityTest]
        [Description("MessagePack版のIFileStorageServiceで書いたDTOのリスト(Dictionaryを含む)を読むと、同じ内容に戻る")]
        public IEnumerator WriteThenReadReturnsSameData() => UniTask.ToCoroutine(async () =>
        {
            const string fileKey = "roundTrip";
            var dtos = new List<SampleMissionDto>
            {
                new() { MissionId = 1, Progress = 3, IsCompleted = false, Counters = new Dictionary<string, int> { ["kill"] = 2 } },
                new() { MissionId = 2, Progress = 10, IsCompleted = true, Counters = new Dictionary<string, int>() },
            };

            fileStorageService.CreateDirectoryIfNotExists(fileKey);
            await fileStorageService.WriteAsync(fileKey, dtos, Password);
            var actual = await fileStorageService.ReadAsync<List<SampleMissionDto>>(fileKey, Password);

            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(3, actual[0].Progress);
            Assert.AreEqual(2, actual[0].Counters["kill"]);
            Assert.IsTrue(actual[1].IsCompleted);
        });

        [UnityTest]
        [Description("SaveDataRepositoryを変更せずにMessagePack版で使え、保存したEntityを別のインスタンスで読み込める")]
        public IEnumerator RepositorySavesAndLoadsEntities() => UniTask.ToCoroutine(async () =>
        {
            var repository = resolver.Resolve<SampleMissionRepository>();
            await repository.UpdateAsync(new List<SampleMission> { new(1, 5), new(2, 7) });

            var reloaded = new SampleMissionRepository(fileStorageService);
            await reloaded.LoadAsync();

            Assert.AreEqual(5, reloaded.Find(1).Progress);
            Assert.AreEqual(7, reloaded.Find(2).Progress);
        });

        [UnityTest]
        [Description("同じ形のレコードが繰り返すデータは、LZ4の圧縮で、圧縮しないMessagePackの半分未満のファイルになる")]
        public IEnumerator RepeatedRecordsAreCompressed() => UniTask.ToCoroutine(async () =>
        {
            const string fileKey = "compressed";
            var dtos = new List<SampleMissionDto>();
            for (var i = 0; i < 500; i++)
            {
                dtos.Add(new SampleMissionDto
                {
                    MissionId = i % 10,
                    Progress = 100,
                    IsCompleted = true,
                    Counters = new Dictionary<string, int> { ["defeatedEnemies"] = 3 },
                });
            }

            fileStorageService.CreateDirectoryIfNotExists(fileKey);
            await fileStorageService.WriteAsync(fileKey, dtos, Password);

            var uncompressedSize = MessagePackSerializer.Serialize(dtos, MessagePackSerializerOptions.Standard).Length;
            var fileSize = new FileInfo(GetFilePath(fileKey)).Length;
            Assert.Less(fileSize, uncompressedSize / 2);
        });

        [UnityTest]
        [Description("保存したファイルを書き換えると、読み込みでCryptographicExceptionになり、改ざんしたデータを返さない")]
        public IEnumerator ReadThrowsWhenFileIsTampered() => UniTask.ToCoroutine(async () =>
        {
            const string fileKey = "tampered";
            fileStorageService.CreateDirectoryIfNotExists(fileKey);
            await fileStorageService.WriteAsync(fileKey, new List<SampleMissionDto> { new() { MissionId = 1 } }, Password);

            var path = GetFilePath(fileKey);
            var bytes = File.ReadAllBytes(path);
            bytes[bytes.Length / 2] ^= 0x01;
            File.WriteAllBytes(path, bytes);

            LogAssert.ignoreFailingMessages = true;
            try
            {
                await fileStorageService.ReadAsync<List<SampleMissionDto>>(fileKey, Password);
                Assert.Fail("Tampered file should not be read.");
            }
            catch (CryptographicException)
            {
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        });

        private static string GetFilePath(string fileKey)
        {
            return Path.Combine(
                UnityEngine.Application.persistentDataPath,
                DirectoryName,
                EncryptedBinFileFormatProvider.GetFileName(fileKey)
            );
        }
    }
}
