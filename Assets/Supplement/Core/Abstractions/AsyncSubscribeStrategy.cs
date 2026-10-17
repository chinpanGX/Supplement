namespace Supplement.Core
{
    /// <summary>
    /// <see cref="IMessageBroker.SubscribeAwait{T}"/>で、処理中に次のメッセージが届いたときの扱い。
    /// </summary>
    public enum AsyncSubscribeStrategy
    {
        /// <summary>届いた順に並べ、前の処理が終わってから次の処理を始める。</summary>
        Sequential,

        /// <summary>前の処理を待たずに、次の処理を始める。</summary>
        Parallel,

        /// <summary>処理中のハンドラーに渡したトークンを取り消し、次の処理を始める。</summary>
        Switch,

        /// <summary>処理中に届いたメッセージは捨てる。</summary>
        Drop,

        /// <summary>
        /// 処理中に届いたメッセージは最後の1件だけ残し、前の処理が終わってからそれを処理する。それより前に届いたものは捨てる。
        /// </summary>
        ThrottleFirstLast,
    }
}
