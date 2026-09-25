using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity.IO
{
    public class EncryptedFileAccessor
    {
        private readonly ICryptographyExecutor cryptographyExecutor;

        public EncryptedFileAccessor(ICryptographyExecutor cryptographyExecutor)
        {
            this.cryptographyExecutor = cryptographyExecutor
                                        ?? throw new ArgumentNullException(nameof(cryptographyExecutor));
        }

        public async UniTask<T> ReadAsync<T>(string fileFullPath, string password)
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
                var cipherBytes = await File.ReadAllBytesAsync(fileFullPath).AsUniTask();
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

        public async UniTask WriteAsync<T>(string fileFullPath, T data, string password)
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
                await SafeFile.WriteAllBytesAsync(fileFullPath, cipherBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write encrypted file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }
    }
}
