#if USE_VCONTAINER
using System;
using VContainer;

namespace Supplement.VContainer
{
    /// <summary>
    /// VContainer の <see cref="IObjectResolver"/> への静的ゲートウェイ。
    /// </summary>
    /// <remarks>
    /// 主に MonoBehaviour など、コンストラクタインジェクションや
    /// IObjectResolver のインジェクションが行えない場所から、
    /// 例外的に依存オブジェクトを取得するためのクラスです。
    /// 通常は DI コンテナ経由のインジェクションを優先し、それが使えない箇所に限って利用してください。
    /// </remarks>
    public static class ObjectResolverGateway
    {
        private static IObjectResolver resolver;

        /// <summary>
        /// 静的ゲートウェイに使用する <see cref="IObjectResolver"/> を登録します。
        /// すでに別の <see cref="IObjectResolver"/> が登録されている場合は <see cref="InvalidOperationException"/> をスローします。
        /// 再登録したい場合は、先に <see cref="Reset"/> を呼び出してください。
        /// </summary>
        public static void Register(IObjectResolver objectResolver)
        {
            if (resolver != null)
            {
                throw new InvalidOperationException(
                    "Resolver is already set. Call Reset before registering a new resolver.");
            }

            resolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
        }

        /// <summary>
        /// 登録済みの <see cref="IObjectResolver"/> を使用して指定された型のインスタンスを解決します。
        /// 解決に失敗した場合は <see cref="InvalidOperationException"/> をスローします。
        /// </summary>
        /// <typeparam name="T">解決対象の型。</typeparam>
        /// <returns>
        /// 指定された型のインスタンスを返します。
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// 静的ゲートウェイに <see cref="IObjectResolver"/> が登録されていない場合にスローされます。
        /// </exception>
        public static T Resolve<T>() where T : class
        {
            if (resolver == null)
            {
                throw new InvalidOperationException("Resolver is not set. Please call Register before using Resolve.");
            }
            return resolver.Resolve<T>();
        }

        /// <summary>
        /// 登録済みの <see cref="IObjectResolver"/> を使用して指定された型のインスタンスを解決します。
        /// 解決に失敗した場合は、デフォルト値を返します。
        /// </summary>
        /// <param name="instance">解決された型のインスタンスが代入されます。失敗した場合は null が設定されます。</param>
        /// <typeparam name="T">解決対象の型。</typeparam>
        /// <returns>
        /// 解決に成功した場合は true を返します。失敗した場合は false を返します。
        /// </returns>
        public static bool TryResolve<T>(out T instance) where T : class
        {
            if (resolver == null)
            {
                instance = null;
                return false;
            }
            instance = resolver.Resolve<T>();
            return instance != null;
        }

        /// <summary>
        /// 静的ゲートウェイに登録されている <see cref="IObjectResolver"/> をリセットします。
        /// リセット後は、新たに <see cref="Register"/> を呼び出して <see cref="IObjectResolver"/> を再登録する必要があります。
        /// </summary>
        public static void Reset()
        {
            resolver = null;
        }
    }
}
#endif