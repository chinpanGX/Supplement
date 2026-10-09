using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="Application.persistentDataPath"/>の下のディレクトリに、<see cref="IEncryptedFileAccessor"/>でファイルを読み書きする。
    /// </summary>
    /// <remarks>
    /// ファイル名はキーから<see cref="EncryptedBinFileFormatProvider.GetFileName"/>で決める。ディレクトリは
    /// <see cref="SetDirectoryName"/>で変えなければ<c>SaveData</c>。同じファイルへの読み書き・削除は1つずつ順に行う。
    /// </remarks>
    public sealed class FileStorageService : IFileStorageService
    {
        private const string DefaultDirectoryName = "SaveData";

        private readonly IEncryptedFileAccessor fileAccessor;
        // ファイルごとの排他。書き込み・削除はリトライで待つ間に別の操作が割り込めるため、同じファイルへの操作は1つずつ順に行う。
        // 例えば削除のリトライ待ちの間に書き込んだファイルを、再開した削除が消してしまうのを防ぐ。
        private readonly Dictionary<string, SemaphoreSlim> fileLocks = new();
        private string directoryName;

        /// <summary>
        /// ファイルの読み書きに使う<see cref="IEncryptedFileAccessor"/>を指定して作る。
        /// </summary>
        /// <param name="fileAccessor">データを変換・暗号化してファイルに読み書きする。</param>
        public FileStorageService(IEncryptedFileAccessor fileAccessor)
        {
            this.fileAccessor = fileAccessor;
        }

        /// <inheritdoc/>
        public async UniTask<T> ReadAsync<T>(string fileKey, string password)
        {
            var path = GetFilePath(fileKey);
            var fileLock = GetFileLock(path);
            await fileLock.WaitAsync();
            try
            {
                return await fileAccessor.ReadAsync<T>(path, password);
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async UniTask WriteAsync<T>(string fileKey, T data, string password)
        {
            var path = GetFilePath(fileKey);
            var fileLock = GetFileLock(path);
            await fileLock.WaitAsync();
            try
            {
                await fileAccessor.WriteAsync(path, data, password);
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public bool Exists(string fileKey)
        {
            var path = GetFilePath(fileKey);
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public void SetDirectoryName(string targetDirectoryName)
        {
            directoryName = targetDirectoryName;
        }

        /// <inheritdoc/>
        public void CreateDirectoryIfNotExists(string fileKey)
        {
            var filePath = GetFilePath(fileKey);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <inheritdoc/>
        public async UniTask DeleteFileAsync(string fileKey)
        {
            var path = GetFilePath(fileKey);
            var fileLock = GetFileLock(path);
            await fileLock.WaitAsync();
            try
            {
                await SafeFile.DeleteAsync(path);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private SemaphoreSlim GetFileLock(string path)
        {
            lock (fileLocks)
            {
                if (!fileLocks.TryGetValue(path, out var fileLock))
                {
                    fileLock = new SemaphoreSlim(1, 1);
                    fileLocks.Add(path, fileLock);
                }
                return fileLock;
            }
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
