using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    public interface IFileReader
    {
        /// <summary>
        /// 指定されたパスからデータを読み込みます。
        /// </summary>
        UniTask<T> ReadAsync<T>(string fileFullPath, string password, CancellationToken token);
    }
}