using Supplement.Core;

namespace Supplement.Unity.IO
{
    public static class EncryptedBinFileFormatProvider
    {
        public static string GetFileName(string fileNameWithoutExtension)
        {
            return $"{Crc32.Compute(fileNameWithoutExtension)}.bin";
        }
    }
}