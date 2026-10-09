using System;
using Supplement.Core;
using VContainer;

namespace Supplement.Unity
{
    /// <summary>
    /// Supplementの機能をVContainerに登録する拡張メソッド。
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// 暗号化したファイルの読み書き(<see cref="IFileStorageService"/>とその依存)を、既定のAESの設定
        /// (<see cref="AesOptions.CreateDefault"/>)で登録する。
        /// </summary>
        public static void RegisterEncryptedFileStorage(this IContainerBuilder builder)
        {
            builder.RegisterEncryptedFileStorageWithCustomAesOptions(AesOptions.CreateDefault());
        }

        /// <summary>
        /// 暗号化したファイルの読み書き(<see cref="IFileStorageService"/>・<see cref="IEncryptedFileAccessor"/>・
        /// <see cref="ICryptographyExecutor"/>・<see cref="ICryptoAlgorithm"/>)を、指定したAESの設定でSingletonとして登録する。
        /// </summary>
        /// <param name="builder">登録先。</param>
        /// <param name="aesOptions">暗号化に使うAESの設定。</param>
        public static void RegisterEncryptedFileStorageWithCustomAesOptions(this IContainerBuilder builder, AesOptions aesOptions)
        {
            builder.RegisterInstance(aesOptions);
            builder.Register<IFileStorageService, FileStorageService>(Lifetime.Singleton);
            builder.Register<IEncryptedFileAccessor, EncryptedFileAccessor>(Lifetime.Singleton);
            builder.Register<ICryptographyExecutor, CryptographyExecutor>(Lifetime.Singleton);
            builder.Register<ICryptoAlgorithm, AesCryptoAlgorithm>(Lifetime.Singleton);
        }

        /// <summary>
        /// BackKey Event Viewerに表示するため、有効な<see cref="BackKeyReceiver"/>の一覧を登録する。
        /// </summary>
        public static void RegisterBackKeyDebugRegistry(this IContainerBuilder builder)
        {
            builder.Register<BackKeyReceiverDebugRegistry>(Lifetime.Singleton);
        }

        /// <summary>
        /// <see cref="RegisterBackKeyDebugRegistry"/>と同じ。Game Viewへの表示は削除された。
        /// </summary>
        [Obsolete("Game Viewへの表示は削除された。Receiverの一覧だけを登録するRegisterBackKeyDebugRegistryを使う。")]
        public static void RegisterBackKeyDebugOverlay(this IContainerBuilder builder)
        {
            builder.RegisterBackKeyDebugRegistry();
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
}
