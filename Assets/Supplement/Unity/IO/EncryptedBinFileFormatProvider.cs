using Supplement.Core;
using Supplement.Core.Abstractions;

namespace Supplement.Unity.IO
{
    public sealed class EncryptedBinFileFormatProvider : IFileFormatProvider
    {
        public string GetFileName(string fileNameWithoutExtension)
        {
            return $"{Crc32.Compute(fileNameWithoutExtension)}.bin";
        }
        
        public string GetSupportedFileExtension()
        {
            return ".bin";
        }
    }
}