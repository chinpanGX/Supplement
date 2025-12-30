using System;
using System.Text;
using Supplement.Core.Abstractions;

namespace Supplement.Core
{
    public sealed class CryptographyExecutor : ICryptographyExecutor
    {
        private readonly ICryptoAlgorithm cryptoAlgorithm;

        public CryptographyExecutor(ICryptoAlgorithm cryptoAlgorithm)
        {
            this.cryptoAlgorithm = cryptoAlgorithm;
        }

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