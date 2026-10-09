using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// 暗号化したファイルの読み書きのうち、変換と暗号化以外(引数の検証・読み込み・安全な書き込み・エラーログ)を
    /// JSON版とMessagePack版で共有する。
    /// </summary>
    /// <remarks>
    /// 変換と暗号化はデリゲートで受け取る。呼び出しのたびにクロージャを作らないよう、必要な値は<c>state</c>で渡し、
    /// デリゲートはstaticラムダにする。
    /// </remarks>
    internal static class EncryptedFileIO
    {
        internal static async UniTask<T> ReadAsync<T, TState>(
            string fileFullPath,
            string password,
            TState state,
            Func<byte[], string, TState, T> decode)
        {
            ValidatePath(fileFullPath);

            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException($"File not found at path: {fileFullPath}");
            }

            ValidatePassword(password);

            try
            {
                var cipherBytes = await File.ReadAllBytesAsync(fileFullPath).AsUniTask();
                return decode(cipherBytes, password, state);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read encrypted file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }

        internal static async UniTask WriteAsync<T, TState>(
            string fileFullPath,
            T data,
            string password,
            TState state,
            Func<T, string, TState, byte[]> encode)
        {
            ValidatePath(fileFullPath);
            ValidatePassword(password);

            try
            {
                var cipherBytes = encode(data, password, state);
                await SafeFile.WriteAllBytesAsync(fileFullPath, cipherBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write encrypted file at \"{fileFullPath}\". {e.GetType().Name}: {e.Message}");
                throw;
            }
        }

        private static void ValidatePath(string fileFullPath)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentException("Path must not be null or empty.", nameof(fileFullPath));
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password must not be null or empty.", nameof(password));
            }
        }
    }
}
