using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="GameObject"/>と<see cref="Component"/>の拡張メソッド。
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        /// <typeparamref name="T"/>のコンポーネントを返す。付いていなければ付けてから返す。
        /// </summary>
        /// <param name="self">コンポーネントを探すGameObject。</param>
        /// <typeparam name="T">コンポーネントの型。</typeparam>
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            if (self.TryGetComponent<T>(out var component))
            {
                return component;
            }
            return self.AddComponent<T>();
        }

        /// <summary>
        /// <paramref name="self"/>のGameObjectから<typeparamref name="T"/>のコンポーネントを返す。付いていなければ付けてから返す。
        /// </summary>
        /// <param name="self">GameObjectを指すコンポーネント。</param>
        /// <typeparam name="T">コンポーネントの型。</typeparam>
        public static T GetOrAddComponent<T>(this Component self) where T : Component
        {
            return self.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// <paramref name="self"/>のGameObjectのアクティブ状態を変える。
        /// </summary>
        /// <param name="self">GameObjectを指すコンポーネント。</param>
        /// <param name="active">アクティブにするならtrue。</param>
        public static void SetActive(this Component self, bool active)
        {
            self.gameObject.SetActive(active);
        }
    }
}