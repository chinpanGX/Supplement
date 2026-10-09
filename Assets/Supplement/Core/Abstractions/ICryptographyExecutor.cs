namespace Supplement.Core
{
    /// <summary>
    /// 文字列を暗号化してバイト列にし、バイト列を復号して文字列に戻す。
    /// </summary>
    public interface ICryptographyExecutor
    {
        /// <summary>
        /// <paramref name="plainText"/>を<paramref name="password"/>で暗号化する。
        /// </summary>
        /// <param name="plainText">暗号化する文字列。</param>
        /// <param name="password">暗号化に使うパスワード。</param>
        /// <returns>暗号化したバイト列。<see cref="Decrypt"/>で元の文字列に戻せる。</returns>
        byte[] Encrypt(string plainText, string password);

        /// <summary>
        /// <see cref="Encrypt"/>で暗号化したバイト列を<paramref name="password"/>で復号し、文字列に戻す。
        /// </summary>
        /// <param name="cipherTextBytes">暗号化したバイト列。</param>
        /// <param name="password">暗号化に使ったパスワード。</param>
        /// <returns>復号した文字列。</returns>
        string Decrypt(byte[] cipherTextBytes, string password);
    }
}