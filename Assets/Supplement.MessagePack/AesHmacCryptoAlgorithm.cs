using System;
using System.Buffers;
using System.Security.Cryptography;
using Supplement.Core;

namespace Supplement.MessagePack
{
    /// <summary>
    /// AESで暗号化し、HMAC-SHA256で改ざんと破損を検知する <see cref="ICryptoAlgorithm"/>。
    /// </summary>
    /// <remarks>
    /// 形式は <c>[ Magic "SAH1"(4) | Version(1) | SaltLength(2, BE) | Salt | Cipher | Mac(32) ]</c>。
    /// AES鍵・IV・HMAC鍵は、パスワードと保存ごとにランダムなソルトからPBKDF2で導く。
    /// 壊れた・書き換えられたデータは、ヘッダの不正も含めてすべて<see cref="CryptographicException"/>で知らせる。
    /// </remarks>
    public sealed class AesHmacCryptoAlgorithm : ICryptoAlgorithm
    {
        private const uint Magic = 0x53414831; // 'S' 'A' 'H' '1'
        private const byte Version = 0x01;
        private const int MacSizeInBytes = 32;

        private static readonly Func<string, Exception> CreateFormatException = message => new CryptographicException(message);

        private readonly AesOptions options;

        /// <summary>
        /// 暗号化の設定を指定して作る。
        /// </summary>
        /// <param name="options">鍵長・反復回数・ソルトの長さなどの設定。</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException">ソルトの長さが1〜1024バイトの範囲外。</exception>
        public AesHmacCryptoAlgorithm(AesOptions options)
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

            var salt = AesFormat.GenerateRandomSalt(options.SaltSizeInBytes);

            using var aes = AesFormat.CreateAes(options);
            var macKey = AesFormat.SetKeyAndIv(aes, password, salt, options, MacSizeInBytes);

            using var encryptor = aes.CreateEncryptor();
            var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[AesFormat.HeaderSize + salt.Length + cipher.Length + MacSizeInBytes];
            var offset = AesFormat.WriteHeader(result, Magic, Version, salt);
            Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);
            offset += cipher.Length;

            // ヘッダ・Salt・Cipherの全体にかけ、復号の前に改ざんと破損を検知できるようにする
            using var hmac = new HMACSHA256(macKey);
            hmac.TryComputeHash(
                new ReadOnlySpan<byte>(result, 0, offset),
                new Span<byte>(result, offset, MacSizeInBytes),
                out _
            );

            return result;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"><paramref name="cipherBytes"/>がnull、または<paramref name="password"/>がnullか空。</exception>
        /// <exception cref="CryptographicException">データが壊れている・書き換えられている、またはパスワードが違う。</exception>
        public byte[] Decrypt(byte[] cipherBytes, string password)
        {
            if (cipherBytes == null)
            {
                throw new ArgumentNullException(nameof(cipherBytes));
            }

            AesFormat.ThrowIfPasswordIsEmpty(password);

            if (cipherBytes.Length <= AesFormat.HeaderSize)
            {
                throw new CryptographicException("Cipher bytes are too short to contain header and salt.");
            }

            var offset = AesFormat.ReadHeader(cipherBytes, Magic, Version, MacSizeInBytes, CreateFormatException, out var salt);

            using var aes = AesFormat.CreateAes(options);
            var macKey = AesFormat.SetKeyAndIv(aes, password, salt, options, MacSizeInBytes);

            // 復号より先にMacを照合する。CBCは暗号文を書き換えると平文も狙い通りに変わり、
            // パディングさえ通れば復号に成功してしまうため、Macで弾かないと改ざんや破損に気付けない。
            var macOffset = cipherBytes.Length - MacSizeInBytes;
            var computedMac = ArrayPool<byte>.Shared.Rent(MacSizeInBytes);
            try
            {
                using var hmac = new HMACSHA256(macKey);
                hmac.TryComputeHash(new ReadOnlySpan<byte>(cipherBytes, 0, macOffset), computedMac, out _);

                if (!CryptographicOperations.FixedTimeEquals(
                        new ReadOnlySpan<byte>(computedMac, 0, MacSizeInBytes),
                        new ReadOnlySpan<byte>(cipherBytes, macOffset, MacSizeInBytes)))
                {
                    throw new CryptographicException(
                        "Cipher authentication failed. The data is corrupted, tampered, or the password is wrong.");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(computedMac);
            }

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherBytes, offset, macOffset - offset);
        }
    }
}
