using System;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="DeferredUpdateBuffer{TEntity}"/>を、始める前に操作したり、二重に始めたりしたときに投げる。
    /// </summary>
    public class DeferredUpdateBufferException : Exception
    {
        /// <summary>
        /// メッセージを指定して作る。
        /// </summary>
        /// <param name="message">誤った操作の内容。</param>
        public DeferredUpdateBufferException(string message) : base(message)
        {
        }
    }
}