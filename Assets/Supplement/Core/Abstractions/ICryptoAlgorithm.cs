namespace Supplement.Core
{
    /// <summary>
    /// パスワードを使ってバイト列を暗号化・復号するアルゴリズム。
    /// </summary>
    public interface ICryptoAlgorithm
    {
        /// <summary>
        /// <paramref name="plainBytes"/>を<paramref name="password"/>で暗号化する。
        /// </summary>
        /// <param name="plainBytes">暗号化するバイト列。</param>
        /// <param name="password">暗号化に使うパスワード。</param>
        /// <returns>暗号化したバイト列。<see cref="Decrypt"/>で元に戻せる。</returns>
        byte[] Encrypt(byte[] plainBytes, string password);

        /// <summary>
        /// <see cref="Encrypt"/>で暗号化したバイト列を<paramref name="password"/>で復号する。
        /// </summary>
        /// <param name="cipherBytes">暗号化したバイト列。</param>
        /// <param name="password">暗号化に使ったパスワード。</param>
        /// <returns>復号したバイト列。</returns>
        byte[] Decrypt(byte[] cipherBytes, string password);
    }
}