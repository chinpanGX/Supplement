using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Unity.IO
{
    /// <summary>
    /// 一時ファイルに書き込んでから対象のファイルと置き換えることで、書き込み途中でプロセスが
    /// クラッシュしても対象のファイルが中途半端な内容にならないようにする。
    /// </summary>
    internal static class SafeFile
    {
        private const string TempFileInfix = ".tmp";
        private const string TempFileSuffix = "~";

        private const int MaxRetryCount = 10;
        private const int InitialRetryDelayMilliseconds = 100;
        private const int MaxRetryDelayMilliseconds = 5000;

        public static async UniTask WriteAllBytesAsync(string filePath, byte[] bytes)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            CleanUpStaleTempFiles(filePath, dir);

            var tempPath = $"{filePath}{TempFileInfix}{Guid.NewGuid():N}{TempFileSuffix}";
            await WriteTempFileAsync(tempPath, bytes);
            await ReplaceAsync(tempPath, filePath);
        }

        private static async UniTask WriteTempFileAsync(string tempPath, byte[] bytes)
        {
            // セキュリティソフト等による一時的なファイルロックの可能性があるためリトライする。
            // ディスク容量不足等それ以外の原因のIOExceptionはリトライしても解決しないためそのまま投げる。
            var delay = InitialRetryDelayMilliseconds;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await File.WriteAllBytesAsync(tempPath, bytes).AsUniTask();
                    return;
                }
                catch (UnauthorizedAccessException) when (attempt < MaxRetryCount)
                {
                    await UniTask.Delay(delay);
                    delay = Math.Min(delay * 2, MaxRetryDelayMilliseconds);
                }
            }
        }

        private static async UniTask ReplaceAsync(string tempPath, string filePath)
        {
            // 置き換え先を他プロセス(セキュリティソフト等)が一時的に開いている可能性があるためリトライする。
            var delay = InitialRetryDelayMilliseconds;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Replace(tempPath, filePath, null);
                    }
                    else
                    {
                        File.Move(tempPath, filePath);
                    }
                    return;
                }
                catch (IOException) when (attempt < MaxRetryCount)
                {
                    await UniTask.Delay(delay);
                    delay = Math.Min(delay * 2, MaxRetryDelayMilliseconds);
                }
            }
        }

        /// <summary>
        /// 前回書き込み中のクラッシュ等で残った一時ファイルを削除する。掃除できなくても
        /// 今回の書き込み自体には影響しないため、失敗は無視する。
        /// </summary>
        private static void CleanUpStaleTempFiles(string filePath, string dir)
        {
            var searchDir = string.IsNullOrEmpty(dir) ? "." : dir;
            if (!Directory.Exists(searchDir))
            {
                return;
            }

            var searchPattern = $"{Path.GetFileName(filePath)}{TempFileInfix}*{TempFileSuffix}";
            foreach (var stalePath in Directory.EnumerateFiles(searchDir, searchPattern))
            {
                try
                {
                    File.Delete(stalePath);
                }
                catch (IOException)
                {
                }
            }
        }

        public static void Delete(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var delay = InitialRetryDelayMilliseconds;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    File.Delete(filePath);
                    return;
                }
                catch (IOException) when (attempt < MaxRetryCount)
                {
                    Thread.Sleep(delay);
                    delay = Math.Min(delay * 2, MaxRetryDelayMilliseconds);
                }
            }
        }
    }
}
