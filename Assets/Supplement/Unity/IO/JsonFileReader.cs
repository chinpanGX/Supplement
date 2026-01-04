using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public class JsonFileReader : IFileReader
    {
        public async UniTask<T> ReadAsync<T>(string fileFullPath, string password, CancellationToken token)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(fileFullPath));
            }
            
            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException($"File not found at path: {fileFullPath}");
            }

            try
            {
                var json = await File.ReadAllTextAsync(fileFullPath, Encoding.UTF8, token).AsUniTask();
                var dto = JsonUtility.FromJson<JsonDto<T>>(json);
                return dto.Data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read JSON file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }
    }
}