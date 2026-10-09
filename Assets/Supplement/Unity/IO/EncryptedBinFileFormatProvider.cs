using Supplement.Core;

namespace Supplement.Unity
{
    /// <summary>
    /// 暗号化したファイルの名前を決める。
    /// </summary>
    public static class EncryptedBinFileFormatProvider
    {
        /// <summary>
        /// <paramref name="fileNameWithoutExtension"/>のCRC32を名前にし、拡張子<c>.bin</c>を付けたファイル名を返す。
        /// </summary>
        /// <param name="fileNameWithoutExtension">元の名前(ファイルのキー)。</param>
        public static string GetFileName(string fileNameWithoutExtension)
        {
            return $"{Crc32.Compute(fileNameWithoutExtension)}.bin";
        }
    }
}