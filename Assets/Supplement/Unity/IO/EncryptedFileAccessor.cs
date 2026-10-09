using System;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// データを<see cref="JsonUtility"/>でJSONにし、暗号化してファイルに読み書きする。
    /// </summary>
    public class EncryptedFileAccessor : IEncryptedFileAccessor
    {
        private readonly ICryptographyExecutor cryptographyExecutor;

        /// <summary>
        /// 暗号化に使う<see cref="ICryptographyExecutor"/>を指定して作る。
        /// </summary>
        /// <param name="cryptographyExecutor">JSONの文字列を暗号化・復号する。</param>
        /// <exception cref="ArgumentNullException"><paramref name="cryptographyExecutor"/>がnull。</exception>
        public EncryptedFileAccessor(ICryptographyExecutor cryptographyExecutor)
        {
            this.cryptographyExecutor = cryptographyExecutor
                                        ?? throw new ArgumentNullException(nameof(cryptographyExecutor));
        }

        /// <inheritdoc/>
        public UniTask<T> ReadAsync<T>(string fileFullPath, string password)
        {
            return EncryptedFileIO.ReadAsync(
                fileFullPath,
                password,
                cryptographyExecutor,
                static (cipherBytes, pw, executor) =>
                    JsonUtility.FromJson<JsonDto<T>>(executor.Decrypt(cipherBytes, pw)).Data
            );
        }

        /// <inheritdoc/>
        public UniTask WriteAsync<T>(string fileFullPath, T data, string password)
        {
            return EncryptedFileIO.WriteAsync(
                fileFullPath,
                data,
                password,
                cryptographyExecutor,
                static (value, pw, executor) =>
                    executor.Encrypt(JsonUtility.ToJson(new JsonDto<T> { Data = value }, true), pw)
            );
        }
    }
}
