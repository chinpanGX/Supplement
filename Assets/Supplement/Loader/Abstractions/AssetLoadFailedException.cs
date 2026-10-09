using System;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// アセットやシーンの読み込みに失敗したときに投げる。
    /// </summary>
    public class AssetLoadFailedException : Exception
    {
        /// <summary>
        /// メッセージを指定して作る。
        /// </summary>
        /// <param name="message">失敗の内容。</param>
        public AssetLoadFailedException(string message) : base(message)
        {
        }
    }
}