using System;
using System.Buffers;
using System.Text;

namespace Supplement.Core
{
    /// <summary>
    /// CRC-32(IEEE 802.3、多項式0xEDB88320)を計算する。
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] Table = new uint[256];

        static Crc32()
        {
            const uint polynomial = 0xedb88320;
            for (uint i = 0; i < 256; i++)
            {
                var crc = i;
                for (var j = 0; j < 8; j++) crc = (crc & 1) == 1 ? crc >> 1 ^ polynomial : crc >> 1;
                Table[i] = crc;
            }
        }

        /// <summary>
        /// 文字列をUTF-8にしたバイト列のCRC-32を、ゼロ埋めした8桁の16進数で返す。
        /// </summary>
        /// <remarks>
        /// 保存ファイル名の生成に使っているため、出力を変えると既存のファイルを読めなくなる。
        /// </remarks>
        public static string Compute(string source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(source.Length));
            try
            {
                var byteCount = Encoding.UTF8.GetBytes(source, 0, source.Length, buffer, 0);
                return Compute(buffer.AsSpan(0, byteCount)).ToString("x8");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// <paramref name="data"/>のCRC-32を返す。
        /// </summary>
        /// <param name="data">計算するバイト列。</param>
        public static uint Compute(ReadOnlySpan<byte> data)
        {
            var crc = 0xffffffff;
            foreach (var b in data)
            {
                crc = crc >> 8 ^ Table[(crc ^ b) & 0xff];
            }
            return ~crc;
        }
    }
}