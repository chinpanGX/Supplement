using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core.Abstractions
{
    public interface ISaveDataRepository
    {
        /// <summary>
        /// すでに保存データが作成されているかどうかを示します。
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// 保存データを非同期で読み込みます。
        /// </summary>
        UniTask LoadAsync(CancellationToken token);

        /// <summary>
        /// 保存データを非同期で保存します。
        /// </summary>
        UniTask SaveAsync(CancellationToken token);

        /// <summary>
        /// 削除します。
        /// </summary>
        void Delete();
    }
}