using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core.Abstractions;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public class EncryptedFileReader : IFileReader
    {
        private readonly ICryptographyExecutor cryptographyExecutor;

        public EncryptedFileReader(ICryptographyExecutor cryptographyExecutor)
        {
            this.cryptographyExecutor = cryptographyExecutor
                                        ?? throw new ArgumentNullException(nameof(cryptographyExecutor));
        }

        public async UniTask<T> ReadAsync<T>(string fileFullPath, string password, CancellationToken token)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentException("Path must not be null or empty.", nameof(fileFullPath));
            }
            
            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException($"File not found at path: {fileFullPath}");
            }

            password ??= string.Empty;

            try
            {
                var cipherBytes = await File.ReadAllBytesAsync(fileFullPath, token).AsUniTask();
                var json = cryptographyExecutor.Decrypt(cipherBytes, password);
                var dto = JsonUtility.FromJson<JsonDto<T>>(json);
                return dto.Data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read encrypted file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }
    }
}