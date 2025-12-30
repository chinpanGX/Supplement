namespace Supplement.Core.Abstractions
{
    public interface IFileFormatProvider
    {
        /// <summary>
        /// 指定されたファイル名から、適切な拡張子を付与した完全なファイル名を取得します。
        /// </summary>
        string GetFileName(string fileNameWithoutExtension);

        /// <summary>
        /// サポートされているファイル形式の拡張子を取得します。
        /// </summary>
        string GetSupportedFileExtension();
    }
}