using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core.Abstractions
{
    public interface IFileStorageService
    {
        UniTask<T> ReadAsync<T>(string fileKey, string password, CancellationToken token);
        UniTask WriteAsync<T>(string fileKey, T data, string password, CancellationToken token);
        bool Exists(string fileKey);
        
        void SetDirectoryName(string targetDirectoryName);
        void CreateDirectoryIfNotExists(string fileKey);
        void DeleteFile(string fileKey);
    }
}