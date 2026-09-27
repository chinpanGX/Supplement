using Supplement.Core;
using Supplement.Unity;
using Supplement.Unity.IO;
using VContainer;

public static class _DIExtensions
{
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
    
    public static void RegisterBackKeyDebugOverlay(this IContainerBuilder builder)
    {
        builder.Register<BackKeyReceiverDebugRegistry>(Lifetime.Singleton);
        builder.Register<IDebugSettingsStore, PlayerPrefsDebugSettingsStore>(Lifetime.Singleton);
        builder.Register<IDebugOverlayState, BackKeyDebugOverlayState>(Lifetime.Singleton);
    }

    /// <summary>
    /// <see cref="BootInitializer"/>を登録する。初期化タスクは<c>As&lt;IBootInitializationTask&gt;()</c>でアプリ側から登録し、
    /// アプリ側のエントリーポイントから<see cref="BootInitializer.RunAsync"/>を呼ぶ。
    /// </summary>
    public static void RegisterBootInitializer(this IContainerBuilder builder)
    {
        builder.Register<BootInitializer>(Lifetime.Singleton);
    }
}
