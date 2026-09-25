using System.IO;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public sealed class FileStorageService : IFileStorageService
    {
        private const string DefaultDirectoryName = "SaveData";
        
        private readonly EncryptedFileAccessor fileAccessor;
        private string directoryName;

        public FileStorageService(EncryptedFileAccessor fileAccessor)
        {
            this.fileAccessor = fileAccessor;
        }

        public UniTask<T> ReadAsync<T>(string fileKey, string password)
        {
            var path = GetFilePath(fileKey);
            return fileAccessor.ReadAsync<T>(path, password);
        }

        public UniTask WriteAsync<T>(string fileKey, T data, string password)
        {
            var path = GetFilePath(fileKey);
            return fileAccessor.WriteAsync(path, data, password);
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
            SafeFile.Delete(path);
        }

        private string GetFilePath(string fileKey)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                directoryName = DefaultDirectoryName;
            }
            var fileName = EncryptedBinFileFormatProvider.GetFileName(fileKey);
            return Path.Combine(Application.persistentDataPath, directoryName, fileName);
        }
    }
}