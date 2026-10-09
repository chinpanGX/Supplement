using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// 保存データの読み込み・保存・削除を行う。
    /// </summary>
    public interface ISaveDataRepository
    {
        /// <summary>
        /// すでに保存データが作成されているかどうかを示します。
        /// 待っている保存・削除の完了は待たず、今の状態を返します。
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
        /// 保存データを非同期で削除します。
        /// </summary>
        UniTask DeleteAsync();
    }
}