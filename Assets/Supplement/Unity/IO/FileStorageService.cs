using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public sealed class FileStorageService : IFileStorageService
    {
        private const string DefaultDirectoryName = "SaveData";
        
        private readonly IFileReader reader;
        private readonly IFileWriter writer;
        private readonly IFileFormatProvider fileFormatProvider;
        private string directoryName;

        public FileStorageService(IFileReader reader, IFileWriter writer, IFileFormatProvider fileFormatProvider)
        {
            this.reader = reader;
            this.writer = writer;
            this.fileFormatProvider = fileFormatProvider;
        }

        public UniTask<T> ReadAsync<T>(string fileKey, string password, CancellationToken token)
        {
            var path = GetFilePath(fileKey);
            return reader.ReadAsync<T>(path, password, token);
        }

        public UniTask WriteAsync<T>(string fileKey, T data, string password, CancellationToken token)
        {
            var path = GetFilePath(fileKey);
            return writer.WriteAsync(path, data, password, token);
        }
        
        public bool Exists(string fileKey)
        {
            var path = GetFilePath(fileKey);
            return File.Exists(path);
        }
        
        public void SetDirectoryName(string targetDirectoryName)
        {
            directoryName = targetDirectoryName;
        }
        
        public void CreateDirectoryIfNotExists(string fileKey)
        {
            var filePath = GetFilePath(fileKey);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        
        public void DeleteFile(string fileKey)
        {
            var path = GetFilePath(fileKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetFilePath(string fileKey)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                directoryName = DefaultDirectoryName;
            }
            var fileName = fileFormatProvider.GetFileName(fileKey);
            return Path.Combine(Application.persistentDataPath, directoryName, fileName);
        }
    }
}