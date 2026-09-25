#if USE_VCONTAINER
using Supplement.Core;
using Supplement.Loader.Abstractions;
using Supplement.Loader.AddressablesLoader;
using Supplement.Unity.IO;
using VContainer;

public static class _DIExtensions
{
    public static void RegisterAddressablesLoader(this IContainerBuilder builder)
    {
        builder.Register<AddressablesAssetLoader>(Lifetime.Singleton).As<IAssetLoader, ISceneLoader>();
    }

    public static void RegisterEncryptedFileStorage(this IContainerBuilder builder)
    {
        var aesOption = AesOptions.CreateDefault();
        builder.RegisterInstance(aesOption);
        builder.Register<IFileStorageService, FileStorageService>(Lifetime.Singleton);
        builder.Register<EncryptedFileAccessor>(Lifetime.Singleton);
        builder.Register<ICryptographyExecutor, CryptographyExecutor>(Lifetime.Singleton);
        builder.Register<ICryptoAlgorithm, AesCryptoAlgorithm>(Lifetime.Singleton);
    }

    public static void RegisterEncryptedFileStorageWithCustomAesOptions(this IContainerBuilder builder, AesOptions aesOptions)
    {
        builder.RegisterInstance(aesOptions);
        builder.Register<IFileStorageService, FileStorageService>(Lifetime.Singleton);
        builder.Register<EncryptedFileAccessor>(Lifetime.Singleton);
        builder.Register<ICryptographyExecutor, CryptographyExecutor>(Lifetime.Singleton);
        builder.Register<ICryptoAlgorithm, AesCryptoAlgorithm>(Lifetime.Singleton);
    }
}
#endif