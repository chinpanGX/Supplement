namespace Supplement.Core
{
    /// <summary>
    /// <see cref="IMessageBroker.PublishAsync{T}"/>で、購読者を呼ぶ順序。
    /// </summary>
    public enum AsyncPublishStrategy
    {
        /// <summary>すべての購読者を一度に開始し、すべての完了を待つ。</summary>
        Parallel,

        /// <summary>購読者を登録順に1つずつ呼び、前の購読者の完了を待ってから次を呼ぶ。</summary>
        Sequential,
    }
}
