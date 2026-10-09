using Cysharp.Threading.Tasks;

namespace Supplement.Unity
{
    /// <summary>
    /// データを変換・暗号化してファイルに読み書きする。<see cref="FileStorageService"/>が使う。
    /// </summary>
    public interface IEncryptedFileAccessor
    {
        /// <summary>
        /// <paramref name="fileFullPath"/>のファイルを読み込み、復号してデータに戻す。
        /// </summary>
        /// <param name="fileFullPath">ファイルのフルパス。</param>
        /// <param name="password">書き込んだときのパスワード。</param>
        /// <typeparam name="T">データの型。</typeparam>
        /// <returns>読み込んだデータ。</returns>
        /// <exception cref="System.IO.FileNotFoundException">ファイルが無い。</exception>
        UniTask<T> ReadAsync<T>(string fileFullPath, string password);

        /// <summary>
        /// <paramref name="data"/>を変換・暗号化して<paramref name="fileFullPath"/>に書き込む。
        /// ファイルがあれば置き換え、書き込み途中で止まっても元のファイルを壊さない。
        /// </summary>
        /// <param name="fileFullPath">ファイルのフルパス。</param>
        /// <param name="data">書き込むデータ。</param>
        /// <param name="password">暗号化に使うパスワード。</param>
        /// <typeparam name="T">データの型。</typeparam>
        UniTask WriteAsync<T>(string fileFullPath, T data, string password);
    }
}
