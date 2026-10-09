using System;

namespace Supplement.Core
{
    /// <summary>
    /// メッセージの型ごとに、送信と購読を仲介する。
    /// </summary>
    public interface IMessageBroker
    {
        /// <summary>
        /// <typeparamref name="T"/>を購読しているハンドラーに<paramref name="message"/>を送る。
        /// </summary>
        /// <param name="message">送るメッセージ。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        void Publish<T>(T message) where T : struct;

        /// <summary>
        /// <typeparamref name="T"/>のメッセージを購読する。
        /// </summary>
        /// <param name="handler">メッセージを受け取ったときに呼ぶ処理。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>破棄すると購読をやめる。</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }
}