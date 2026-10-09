using System;
using System.Security.Cryptography;

namespace Supplement.Core
{
    /// <summary>
    /// AESで暗号化・復号する。鍵とIVはパスワードと暗号化ごとのランダムなソルトからPBKDF2で導く。
    /// </summary>
    /// <remarks>
    /// 暗号化したバイト列は、形式を表すヘッダとソルトの後ろに暗号文を続けたもの。改ざんの検出はしない。
    /// </remarks>
    public sealed class AesCryptoAlgorithm : ICryptoAlgorithm
    {
        // フォーマット定義
        // "SAC1" = Supplement AES Crypt v1
        // [ Magic(4) | Version(1) | SaltLength(2, BE) | Salt | Cipher ]
        private const uint Magic = 0x53414331; // 'S' 'A' 'C' '1'
        private const byte Version = 0x01;

        private static readonly Func<string, Exception> CreateFormatException = message => new InvalidOperationException(message);

        private readonly AesOptions options;

        /// <summary>
        /// 暗号化の設定を指定して作る。
        /// </summary>
        /// <param name="options">鍵長・反復回数・ソルトの長さなどの設定。</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException">ソルトの長さが1〜1024バイトの範囲外。</exception>
        public AesCryptoAlgorithm(AesOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            AesFormat.ValidateOptions(options);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="plainBytes"/>がnull、または<paramref name="password"/>がnullか空。</exception>
        public byte[] Encrypt(byte[] plainBytes, string password)
        {
            if (plainBytes == null)
            {
                throw new ArgumentNullException(nameof(plainBytes));
            }

            AesFormat.ThrowIfPasswordIsEmpty(password);

            // 暗号化ごとにランダムな salt を生成
            var salt = AesFormat.GenerateRandomSalt(options.SaltSizeInBytes);

            using var aes = AesFormat.CreateAes(options);
            AesFormat.SetKeyAndIv(aes, password, salt, options);

            using var encryptor = aes.CreateEncryptor();
            var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[AesFormat.HeaderSize + salt.Length + cipher.Length];
            var offset = AesFormat.WriteHeader(result, Magic, Version, salt);
            Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);
            return result;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="cipherBytes"/>がnull、または<paramref name="password"/>がnullか空。</exception>
        /// <exception cref="ArgumentException"><paramref name="cipherBytes"/>がヘッダより短い。</exception>
        /// <exception cref="InvalidOperationException">ヘッダの形式が違う、またはソルトの長さが不正。</exception>
        /// <exception cref="CryptographicException">パスワードが違うなどで復号できない。</exception>
        public byte[] Decrypt(byte[] cipherBytes, string password)
        {
            if (cipherBytes == null)
            {
                throw new ArgumentNullException(nameof(cipherBytes));
            }

            AesFormat.ThrowIfPasswordIsEmpty(password);

            if (cipherBytes.Length <= AesFormat.HeaderSize)
            {
                throw new ArgumentException(
                    "Cipher bytes are too short to contain header and salt.",
                    nameof(cipherBytes)
                );
            }

            var offset = AesFormat.ReadHeader(cipherBytes, Magic, Version, 0, CreateFormatException, out var salt);

            using var aes = AesFormat.CreateAes(options);
            AesFormat.SetKeyAndIv(aes, password, salt, options);

            // 残りが暗号データ本体。別の配列にコピーせず、範囲を指定して復号する。
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherBytes, offset, cipherBytes.Length - offset);
        }
    }
}
