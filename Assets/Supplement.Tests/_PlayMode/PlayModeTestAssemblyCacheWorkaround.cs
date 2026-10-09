#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace Supplement.Tests.PlayMode
{
    // Unity Test Framework 1.8.0 の PlayerTestAssemblyProvider は、テストアセンブリの一覧を static にキャッシュし、
    // null のときだけ読み込む。プレイモードに入るたびにこのキャッシュを null ではなく空のリストに戻すため、
    // ドメインリロードを無効にしていると2回目以降のPlayModeテストが0件になる。
    // プレイモードに入る前に null に戻し、毎回読み込み直させる。Test Framework側が直ったら消す。
    [InitializeOnLoad]
    internal static class PlayModeTestAssemblyCacheWorkaround
    {
        private const string ProviderTypeName = "UnityEngine.TestTools.Utils.PlayerTestAssemblyProvider, UnityEngine.TestRunner";
        private const string CacheFieldName = "m_LoadedAssemblies";

        static PlayModeTestAssemblyCacheWorkaround()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            // 型やフィールドが無くなっていれば、Test Framework側で作りが変わっているので何もしない
            var providerType = Type.GetType(ProviderTypeName);
            var cacheField = providerType?.GetField(CacheFieldName, BindingFlags.NonPublic | BindingFlags.Static);
            cacheField?.SetValue(null, null);
        }
    }
}
#endif
