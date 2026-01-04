using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    public interface IFileWriter
    {
        /// <summary>
        /// 指定された基底ファイルパスにデータを書き込みます。
        /// </summary>
        UniTask WriteAsync<T>(string fileFullPath, T data, string password, CancellationToken token);
    }
}