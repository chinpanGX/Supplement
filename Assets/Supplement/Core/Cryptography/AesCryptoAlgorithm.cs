using System;
using System.Security.Cryptography;
using Supplement.Core.Abstractions;

namespace Supplement.Core
{
    public sealed class AesCryptoAlgorithm : ICryptoAlgorithm
    {
        // フォーマット定義
        // "SAC1" = Supplement AES Crypt v1
        private const uint Magic = 0x53414331; // 'S' 'A' 'C' '1'
        private const byte Version = 0x01;
        private const int MaxAllowedSaltSize = 1024;

        private readonly AesOptions options;

        public AesCryptoAlgorithm(AesOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            if (options.SaltSizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.SaltSizeInBytes),
                    "SaltSizeInBytes must be greater than zero."
                );
            }

            if (options.SaltSizeInBytes > MaxAllowedSaltSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.SaltSizeInBytes),
                    $"SaltSizeInBytes must be less than or equal to {MaxAllowedSaltSize}."
                );
            }
        }

        public byte[] Encrypt(byte[] plainBytes, string password)
        {
            if (plainBytes == null)
            {
                throw new ArgumentNullException(nameof(plainBytes));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // 暗号化ごとにランダムな salt を生成
            var salt = GenerateRandomSalt(options.SaltSizeInBytes);

            using var aes = CreateAes();
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                options.IterationCount,
                options.KdfHashAlgorithm
            );

            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

            using var encryptor = aes.CreateEncryptor();
            var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // フォーマット：
            // [ Magic(4) | Version(1) | SaltLength(2, BE) | Salt | Cipher ]
            var saltLength = (ushort)salt.Length;
            var headerSize = 4 + 1 + 2;
            var result = new byte[headerSize + salt.Length + cipher.Length];
            var offset = 0;

            // Magic (big endian)
            WriteUInt32BigEndian(result, ref offset, Magic);

            // Version
            result[offset++] = Version;

            // SaltLength (big endian)
            WriteUInt16BigEndian(result, ref offset, saltLength);

            // Salt
            Buffer.BlockCopy(salt, 0, result, offset, salt.Length);
            offset += salt.Length;

            // Cipher
            Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);

            return result;
        }

        public byte[] Decrypt(byte[] cipherBytes, string password)
        {
            if (cipherBytes == null)
            {
                throw new ArgumentNullException(nameof(cipherBytes));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // ヘッダの最小サイズ
            const int headerSize = 4 + 1 + 2; // Magic(4) + Version(1) + SaltLength(2)

            if (cipherBytes.Length <= headerSize)
            {
                throw new ArgumentException(
                    "Cipher bytes are too short to contain header and salt.",
                    nameof(cipherBytes)
                );
            }

            var offset = 0;

            // Magic チェック
            var magic = ReadUInt32BigEndian(cipherBytes, ref offset);
            if (magic != Magic)
            {
                throw new InvalidOperationException("Invalid cipher format: magic mismatch.");
            }

            // Version チェック
            var version = cipherBytes[offset++];
            if (version != Version)
            {
                throw new InvalidOperationException($"Unsupported cipher version: {version}.");
            }

            // SaltLength 取得
            var saltLength = ReadUInt16BigEndian(cipherBytes, ref offset);

            if (saltLength == 0 || saltLength > MaxAllowedSaltSize)
            {
                throw new InvalidOperationException($"Invalid salt length: {saltLength}.");
            }

            if (cipherBytes.Length < headerSize + saltLength + 1)
            {
                // salt と最低 1 バイトの暗号データが入っていない
                throw new InvalidOperationException("Cipher bytes are too short for the specified salt length.");
            }

            // Salt 抽出
            var salt = new byte[saltLength];
            Buffer.BlockCopy(cipherBytes, offset, salt, 0, saltLength);
            offset += saltLength;

            // 残りが暗号データ本体
            var actualCipherLength = cipherBytes.Length - offset;
            var actualCipher = new byte[actualCipherLength];
            Buffer.BlockCopy(cipherBytes, offset, actualCipher, 0, actualCipherLength);

            using var aes = CreateAes();
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                options.IterationCount,
                options.KdfHashAlgorithm
            );

            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(actualCipher, 0, actualCipher.Length);
        }

        private static byte[] GenerateRandomSalt(int size)
        {
            var salt = new byte[size];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        private Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.BlockSize = options.KeySizeInBytes * 8;
            aes.KeySize = options.KeySizeInBytes * 8;
            aes.Mode = options.CipherMode;
            aes.Padding = options.PaddingMode;
            return aes;
        }

        private static void WriteUInt32BigEndian(byte[] buffer, ref int offset, uint value)
        {
            buffer[offset++] = (byte)(value >> 24);
            buffer[offset++] = (byte)(value >> 16);
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)value;
        }

        private static uint ReadUInt32BigEndian(byte[] buffer, ref int offset)
        {
            uint b0 = buffer[offset++];
            uint b1 = buffer[offset++];
            uint b2 = buffer[offset++];
            uint b3 = buffer[offset++];
            return b0 << 24 | b1 << 16 | b2 << 8 | b3;
        }

        private static void WriteUInt16BigEndian(byte[] buffer, ref int offset, ushort value)
        {
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)value;
        }

        private static ushort ReadUInt16BigEndian(byte[] buffer, ref int offset)
        {
            ushort b0 = buffer[offset++];
            ushort b1 = buffer[offset++];
            return (ushort)(b0 << 8 | b1);
        }
    }
}