using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core.Abstractions;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public class EncryptedFileWriter : IFileWriter
    {
        private readonly ICryptographyExecutor cryptographyExecutor;

        public EncryptedFileWriter(ICryptographyExecutor cryptographyExecutor)
        {
            this.cryptographyExecutor = cryptographyExecutor
                                        ?? throw new ArgumentNullException(nameof(cryptographyExecutor));
        }

        public async UniTask WriteAsync<T>(string fileFullPath, T data, string password, CancellationToken token)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentException("Path must not be null or empty.", nameof(fileFullPath));
            }
            
            password ??= string.Empty;

            try
            {
                var dto = new JsonDto<T> { Data = data };
                var json = JsonUtility.ToJson(dto, true);
                var cipherBytes = cryptographyExecutor.Encrypt(json, password);
                await File.WriteAllBytesAsync(fileFullPath, cipherBytes, token).AsUniTask();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write encrypted file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }
    }
}