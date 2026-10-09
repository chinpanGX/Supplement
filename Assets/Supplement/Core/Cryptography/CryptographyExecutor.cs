using System;
using System.Text;

namespace Supplement.Core
{
    /// <summary>
    /// 文字列をUTF-8のバイト列にし、<see cref="ICryptoAlgorithm"/>で暗号化・復号する。
    /// </summary>
    public sealed class CryptographyExecutor : ICryptographyExecutor
    {
        private readonly ICryptoAlgorithm cryptoAlgorithm;

        /// <summary>
        /// 暗号化に使うアルゴリズムを指定して作る。
        /// </summary>
        /// <param name="cryptoAlgorithm">暗号化・復号に使うアルゴリズム。</param>
        public CryptographyExecutor(ICryptoAlgorithm cryptoAlgorithm)
        {
            this.cryptoAlgorithm = cryptoAlgorithm;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="plainText"/>または<paramref name="password"/>がnull。</exception>
        public byte[] Encrypt(string plainText, string password)
        {
            if (plainText == null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return cryptoAlgorithm.Encrypt(plainTextBytes, password);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="cipherTextBytes"/>または<paramref name="password"/>がnull。</exception>
        public string Decrypt(byte[] cipherTextBytes, string password)
        {
            if (cipherTextBytes == null)
            {
                throw new ArgumentNullException(nameof(cipherTextBytes));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var plainTextBytes = cryptoAlgorithm.Decrypt(cipherTextBytes, password);
            return Encoding.UTF8.GetString(plainTextBytes);
        }
    }
}