using Cysharp.Threading.Tasks;

namespace Supplement.Core
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
        UniTask LoadAsync();

        /// <summary>
        /// 保存データを非同期で保存します。
        /// </summary>
        UniTask SaveAsync();

        /// <summary>
        /// 削除します。
        /// </summary>
        void Delete();
    }
}