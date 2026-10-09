using MessagePack;
using Supplement.Core;
using Supplement.Unity;
using VContainer;

namespace Supplement.MessagePack
{
    /// <summary>
    /// MessagePackのセーブデータをVContainerに登録する拡張メソッド。
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// <see cref="IFileStorageService"/>を、MessagePackで読み書きする実装で登録する。
        /// </summary>
        /// <param name="builder">登録先。</param>
        /// <param name="serializerOptions">省略すると<see cref="MessagePackFileAccessor.DefaultSerializerOptions"/>。</param>
        /// <param name="aesOptions">省略すると<see cref="AesOptions.CreateDefault"/>。</param>
        public static void RegisterMessagePackFileStorage(
            this IContainerBuilder builder,
            MessagePackSerializerOptions serializerOptions = null,
            AesOptions aesOptions = null)
        {
            builder.RegisterInstance(aesOptions ?? AesOptions.CreateDefault());
            builder.RegisterInstance(serializerOptions ?? MessagePackFileAccessor.DefaultSerializerOptions);
            builder.Register<ICryptoAlgorithm, AesHmacCryptoAlgorithm>(Lifetime.Singleton);
            builder.Register<IEncryptedFileAccessor, MessagePackFileAccessor>(Lifetime.Singleton);
            builder.Register<IFileStorageService, FileStorageService>(Lifetime.Singleton);
        }
    }
}
