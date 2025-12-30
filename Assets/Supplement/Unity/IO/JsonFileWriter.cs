using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core.Abstractions;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public class JsonFileWriter : IFileWriter
    {
        public async UniTask WriteAsync<T>(string fileFullPath, T data, string password, CancellationToken token)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentException("Path must not be null or empty.", nameof(fileFullPath));
            }
            try
            {
                var dto = new JsonDto<T> { Data = data };
                var jsonContent = JsonUtility.ToJson(dto, true);
                await File.WriteAllTextAsync(fileFullPath, jsonContent, token).AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to file at {fileFullPath}: {ex.Message}");
                throw;
            }
        }
    }
}