using System;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="SaveDataRepository{TKey, TEntity, TDto}"/>で、保存データが壊れているなどして読み込めないときに投げる。
    /// </summary>
    public class SaveDataRepositoryException : Exception
    {
        /// <summary>
        /// メッセージを指定して作る。
        /// </summary>
        /// <param name="message">失敗の内容。</param>
        public SaveDataRepositoryException(string message) : base(message)
        {
        }

        /// <summary>
        /// メッセージと原因の例外を指定して作る。
        /// </summary>
        /// <param name="message">失敗の内容。</param>
        /// <param name="innerException">原因の例外。</param>
        public SaveDataRepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}