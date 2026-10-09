using System;
using System.Security.Cryptography;

namespace Supplement.Core
{
    /// <summary>
    /// AESによる暗号化の設定。
    /// </summary>
    public sealed class AesOptions
    {
        private static readonly HashAlgorithmName DefaultKdfHashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// 暗号利用モード。
        /// </summary>
        public readonly CipherMode CipherMode;

        /// <summary>
        /// 鍵を導くPBKDF2の反復回数。
        /// </summary>
        public readonly int IterationCount;

        /// <summary>
        /// 鍵を導くPBKDF2で使うハッシュアルゴリズム。
        /// </summary>
        public readonly HashAlgorithmName KdfHashAlgorithm;

        /// <summary>
        /// 鍵の長さ(バイト)。AESでは16・24・32のいずれか。
        /// </summary>
        public readonly int KeySizeInBytes;

        /// <summary>
        /// パディングの方式。
        /// </summary>
        public readonly PaddingMode PaddingMode;

        /// <summary>
        /// 暗号化ごとに作るソルトの長さ(バイト)。
        /// </summary>
        public readonly int SaltSizeInBytes;

        /// <summary>
        /// 設定を指定して作る。
        /// </summary>
        /// <param name="keySizeInBytes">鍵の長さ(バイト)。</param>
        /// <param name="iterationCount">鍵を導くPBKDF2の反復回数。</param>
        /// <param name="saltSizeInBytes">暗号化ごとに作るソルトの長さ(バイト)。</param>
        /// <param name="cipherMode">暗号利用モード。</param>
        /// <param name="paddingMode">パディングの方式。</param>
        /// <param name="kdfHashAlgorithm">PBKDF2で使うハッシュアルゴリズム。<c>default</c>ならSHA-256を使う。</param>
        /// <exception cref="ArgumentOutOfRangeException">鍵の長さ・反復回数・ソルトの長さのいずれかが0以下。</exception>
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
        
        /// <summary>
        /// 既定の設定(128bit鍵、反復1000回、16バイトのソルト、CBC、PKCS7、SHA-256)を作る。
        /// </summary>
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