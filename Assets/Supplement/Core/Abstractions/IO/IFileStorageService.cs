using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// ファイルをキーで指定して、データを暗号化して読み書きする。
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// <paramref name="fileKey"/>のファイルを読み込み、復号してデータに戻す。
        /// </summary>
        /// <param name="fileKey">ファイルを表すキー。</param>
        /// <param name="password">書き込んだときのパスワード。</param>
        /// <typeparam name="T">データの型。</typeparam>
        /// <returns>読み込んだデータ。</returns>
        /// <exception cref="System.IO.FileNotFoundException">ファイルが無い。</exception>
        UniTask<T> ReadAsync<T>(string fileKey, string password);

        /// <summary>
        /// <paramref name="data"/>を暗号化して<paramref name="fileKey"/>のファイルに書き込む。ファイルがあれば置き換える。
        /// </summary>
        /// <param name="fileKey">ファイルを表すキー。</param>
        /// <param name="data">書き込むデータ。</param>
        /// <param name="password">暗号化に使うパスワード。</param>
        /// <typeparam name="T">データの型。</typeparam>
        UniTask WriteAsync<T>(string fileKey, T data, string password);

        /// <summary>
        /// <paramref name="fileKey"/>のファイルがあるかどうかを返す。
        /// 待っている書き込み・削除の完了は待たず、今の状態を返す。
        /// </summary>
        /// <param name="fileKey">ファイルを表すキー。</param>
        bool Exists(string fileKey);

        /// <summary>
        /// ファイルを置くディレクトリの名前を変える。以降の読み書きはこのディレクトリに対して行う。
        /// </summary>
        /// <param name="targetDirectoryName">ディレクトリの名前。</param>
        void SetDirectoryName(string targetDirectoryName);

        /// <summary>
        /// <paramref name="fileKey"/>のファイルを置くディレクトリが無ければ作る。
        /// </summary>
        /// <param name="fileKey">ファイルを表すキー。</param>
        void CreateDirectoryIfNotExists(string fileKey);

        /// <summary>
        /// <paramref name="fileKey"/>のファイルを削除する。ファイルが無ければ何もしない。
        /// </summary>
        /// <param name="fileKey">ファイルを表すキー。</param>
        UniTask DeleteFileAsync(string fileKey);
    }
}