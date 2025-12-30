using UnityEngine;

namespace Supplement.Unity
{
    public static class ComponentExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            if (self.TryGetComponent<T>(out var component))
            {
                return component;
            }
            return self.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Component self) where T : Component
        {
            return self.gameObject.GetOrAddComponent<T>();
        }
    }
}