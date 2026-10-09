using System;
using System.Security.Cryptography;

namespace Supplement.Core
{
    /// <summary>
    /// <see cref="AesCryptoAlgorithm"/>とMessagePack版の暗号化が共有する、ヘッダの読み書きと鍵の導出。
    /// </summary>
    /// <remarks>
    /// ヘッダは <c>[ Magic(4, BE) | Version(1) | SaltLength(2, BE) | Salt ]</c>。形式の違いはMagicとVersionで区別する。
    /// 不正なデータで投げる例外の型は呼び出し側ごとに違うため、例外は呼び出し側から渡してもらって作る。
    /// </remarks>
    internal static class AesFormat
    {
        internal const int HeaderSize = 4 + 1 + 2; // Magic(4) + Version(1) + SaltLength(2)
        private const int MaxAllowedSaltSize = 1024;

        internal static void ValidateOptions(AesOptions options)
        {
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

        internal static void ThrowIfPasswordIsEmpty(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
        }

        internal static byte[] GenerateRandomSalt(int size)
        {
            var salt = new byte[size];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        internal static Aes CreateAes(AesOptions options)
        {
            var aes = Aes.Create();
            // AESのブロックサイズは鍵長に関わらず仕様上常に128bit固定。KeySizeInBytesから設定すると
            // 128bit鍵以外でCryptographicExceptionになるため、BlockSizeはAesのデフォルト値のままにする
            aes.KeySize = options.KeySizeInBytes * 8;
            aes.Mode = options.CipherMode;
            aes.Padding = options.PaddingMode;
            return aes;
        }

        /// <summary>
        /// パスワードとソルトからPBKDF2でAES鍵とIVを導いて<paramref name="aes"/>に設定する。
        /// <paramref name="additionalKeySize"/>が0より大きければ、続けて導いた鍵(HMAC鍵など)を返す。
        /// </summary>
        internal static byte[] SetKeyAndIv(Aes aes, string password, byte[] salt, AesOptions options, int additionalKeySize = 0)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                options.IterationCount,
                options.KdfHashAlgorithm
            );

            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);
            return additionalKeySize > 0 ? deriveBytes.GetBytes(additionalKeySize) : Array.Empty<byte>();
        }

        /// <summary>
        /// ヘッダとソルトを書き込み、続きを書く位置を返す。
        /// </summary>
        internal static int WriteHeader(byte[] buffer, uint magic, byte version, byte[] salt)
        {
            var offset = 0;
            WriteUInt32BigEndian(buffer, ref offset, magic);
            buffer[offset++] = version;
            WriteUInt16BigEndian(buffer, ref offset, (ushort)salt.Length);
            Buffer.BlockCopy(salt, 0, buffer, offset, salt.Length);
            return offset + salt.Length;
        }

        /// <summary>
        /// ヘッダを検証してソルトを取り出し、暗号文の始まる位置を返す。
        /// </summary>
        /// <param name="trailerSize">暗号文の後ろに付く値(Macなど)の長さ。</param>
        /// <remarks><paramref name="bytes"/>はヘッダより長いことを呼び出し側で確かめておくこと。</remarks>
        internal static int ReadHeader(
            byte[] bytes,
            uint magic,
            byte version,
            int trailerSize,
            Func<string, Exception> createFormatException,
            out byte[] salt)
        {
            var offset = 0;
            if (ReadUInt32BigEndian(bytes, ref offset) != magic)
            {
                throw createFormatException("Invalid cipher format: magic mismatch.");
            }

            var actualVersion = bytes[offset++];
            if (actualVersion != version)
            {
                throw createFormatException($"Unsupported cipher version: {actualVersion}.");
            }

            var saltLength = ReadUInt16BigEndian(bytes, ref offset);
            if (saltLength == 0 || saltLength > MaxAllowedSaltSize)
            {
                throw createFormatException($"Invalid salt length: {saltLength}.");
            }

            if (bytes.Length < HeaderSize + saltLength + 1 + trailerSize)
            {
                // salt と最低 1 バイトの暗号データ(と後ろに付く値)が入っていない
                throw createFormatException("Cipher bytes are too short for the specified salt length.");
            }

            salt = new byte[saltLength];
            Buffer.BlockCopy(bytes, offset, salt, 0, saltLength);
            return offset + saltLength;
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
