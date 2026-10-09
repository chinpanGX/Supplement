using System;
using Cysharp.Threading.Tasks;
using MessagePack;
using Supplement.Core;
using Supplement.Unity;

namespace Supplement.MessagePack
{
    /// <summary>
    /// データをMessagePackでバイナリにし、暗号化してファイルに読み書きする。
    /// </summary>
    /// <remarks>
    /// 暗号化は<see cref="ICryptoAlgorithm"/>に任せる。<see cref="ContainerBuilderExtensions.RegisterMessagePackFileStorage"/>は
    /// 改ざんを検知する<see cref="AesHmacCryptoAlgorithm"/>を登録する。
    /// </remarks>
    public sealed class MessagePackFileAccessor : IEncryptedFileAccessor
    {
        private readonly ICryptoAlgorithm cryptoAlgorithm;
        private readonly MessagePackSerializerOptions serializerOptions;

        /// <summary>
        /// 暗号化のアルゴリズムとシリアライズの設定を指定して作る。
        /// </summary>
        /// <param name="cryptoAlgorithm">MessagePackのバイナリを暗号化・復号する。</param>
        /// <param name="serializerOptions">シリアライズの設定。特に無ければ<see cref="DefaultSerializerOptions"/>。</param>
        /// <exception cref="ArgumentNullException"><paramref name="cryptoAlgorithm"/>または<paramref name="serializerOptions"/>がnull。</exception>
        public MessagePackFileAccessor(ICryptoAlgorithm cryptoAlgorithm, MessagePackSerializerOptions serializerOptions)
        {
            this.cryptoAlgorithm = cryptoAlgorithm ?? throw new ArgumentNullException(nameof(cryptoAlgorithm));
            this.serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        }

        /// <summary>
        /// 既定のシリアライズ設定。Source Generatorの出力を含む<c>StandardResolver</c>に、LZ4の圧縮を付ける。
        /// </summary>
        public static MessagePackSerializerOptions DefaultSerializerOptions { get; } =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        /// <inheritdoc/>
        public UniTask<T> ReadAsync<T>(string fileFullPath, string password)
        {
            return EncryptedFileIO.ReadAsync(
                fileFullPath,
                password,
                this,
                static (cipherBytes, pw, accessor) => MessagePackSerializer.Deserialize<T>(
                    accessor.cryptoAlgorithm.Decrypt(cipherBytes, pw),
                    accessor.serializerOptions
                )
            );
        }

        /// <inheritdoc/>
        public UniTask WriteAsync<T>(string fileFullPath, T data, string password)
        {
            return EncryptedFileIO.WriteAsync(
                fileFullPath,
                data,
                password,
                this,
                static (value, pw, accessor) => accessor.cryptoAlgorithm.Encrypt(
                    MessagePackSerializer.Serialize(value, accessor.serializerOptions),
                    pw
                )
            );
        }
    }
}
