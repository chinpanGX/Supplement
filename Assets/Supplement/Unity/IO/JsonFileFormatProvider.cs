using Supplement.Core;

namespace Supplement.Unity.IO
{
    public sealed class JsonFileFormatProvider : IFileFormatProvider
    {
        public string GetFileName(string fileNameWithoutExtension)
        {
            return $"{fileNameWithoutExtension}.json";
        }
        
        public string GetSupportedFileExtension()
        {
            return "json";
        }
    }
}