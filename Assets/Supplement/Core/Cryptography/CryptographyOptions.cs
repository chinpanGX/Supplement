using System;
using System.Security.Cryptography;

namespace Supplement.Core
{
    public sealed class AesOptions
    {
        private static readonly HashAlgorithmName DefaultKdfHashAlgorithm = HashAlgorithmName.SHA256;
        public readonly CipherMode CipherMode;
        public readonly int IterationCount;
        public readonly HashAlgorithmName KdfHashAlgorithm;

        public readonly int KeySizeInBytes;
        public readonly PaddingMode PaddingMode;
        public readonly int SaltSizeInBytes;

        public AesOptions(
            int keySizeInBytes,
            int iterationCount,
            int saltSizeInBytes,
            CipherMode cipherMode,
            PaddingMode paddingMode,
            HashAlgorithmName kdfHashAlgorithm)
        {
            if (keySizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keySizeInBytes), "Key size must be greater than zero.");
            }

            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterationCount),
                    "Iteration count must be greater than zero."
                );
            }

            if (saltSizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(saltSizeInBytes), "Salt size must be greater than zero.");
            }

            // KDF のハッシュアルゴリズムが未指定(default)の場合は、安全なデフォルトとして SHA-256 を使用する
            if (kdfHashAlgorithm == default)
            {
                kdfHashAlgorithm = DefaultKdfHashAlgorithm;
            }

            KeySizeInBytes = keySizeInBytes;
            IterationCount = iterationCount;
            SaltSizeInBytes = saltSizeInBytes;
            CipherMode = cipherMode;
            PaddingMode = paddingMode;
            KdfHashAlgorithm = kdfHashAlgorithm;
        }
        
        public static AesOptions CreateDefault()
        {
            return new AesOptions(
                keySizeInBytes: 16, // 128 bits
                iterationCount: 1000,
                saltSizeInBytes: 16,
                cipherMode: CipherMode.CBC,
                paddingMode: PaddingMode.PKCS7,
                kdfHashAlgorithm: HashAlgorithmName.SHA256
            );
        }
    }
}