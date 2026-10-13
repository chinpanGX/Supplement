using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// メッセージの型ごとに、送信と購読を仲介する。
    /// </summary>
    /// <remarks>
    /// 破棄すると、このブローカーへの購読をすべて解除する。
    /// </remarks>
    public interface IMessageBroker : IDisposable
    {
        /// <summary>
        /// <typeparamref name="T"/>を購読しているハンドラーに<paramref name="message"/>を送る。
        /// </summary>
        /// <remarks>
        /// 同期のハンドラーは、この呼び出しの中で登録順に呼ぶ。非同期のハンドラー(<see cref="SubscribeAwait{T}"/>)は
        /// 開始するだけで完了を待たない。完了を待つ場合は<see cref="PublishAsync{T}"/>を使う。
        /// </remarks>
        /// <param name="message">送るメッセージ。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        void Publish<T>(T message) where T : struct;

        /// <summary>
        /// <typeparamref name="T"/>を購読しているハンドラーに<paramref name="message"/>を送り、すべてのハンドラーの完了を待つ。
        /// </summary>
        /// <param name="message">送るメッセージ。</param>
        /// <param name="publishStrategy">非同期のハンドラーを並行して呼ぶか、1つずつ順に呼ぶか。</param>
        /// <param name="cancellationToken">ハンドラーに渡す取り消しのトークン。取り消すと、まだ始まっていないハンドラーは呼ばない。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>すべてのハンドラーが完了すると完了する。ハンドラーが投げた例外をどう返すかは実装による。</returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/>が取り消された。</exception>
        UniTask PublishAsync<T>(T message, AsyncPublishStrategy publishStrategy = AsyncPublishStrategy.Parallel,
            CancellationToken cancellationToken = default) where T : struct;

        /// <summary>
        /// <typeparamref name="T"/>のメッセージを購読する。
        /// </summary>
        /// <param name="handler">メッセージを受け取ったときに呼ぶ処理。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>破棄すると購読をやめる。</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;

        /// <summary>
        /// <typeparamref name="T"/>のメッセージを、非同期の処理で購読する。
        /// </summary>
        /// <param name="handler">
        /// メッセージを受け取ったときに呼ぶ処理。渡すトークンは、購読を解除したとき(<see cref="AsyncSubscribeStrategy.Switch"/>では
        /// 次のメッセージが届いたときも)に取り消される。
        /// </param>
        /// <param name="subscribeStrategy">処理中に次のメッセージが届いたときの扱い。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>破棄すると購読をやめ、処理中のハンドラーに渡したトークンを取り消す。</returns>
        IDisposable SubscribeAwait<T>(Func<T, CancellationToken, UniTask> handler,
            AsyncSubscribeStrategy subscribeStrategy = AsyncSubscribeStrategy.Sequential) where T : struct;
    }
}
