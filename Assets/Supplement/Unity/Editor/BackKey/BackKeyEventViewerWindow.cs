using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Supplement.Unity.Editor
{
    /// <summary>
    /// Registerされた<see cref="BackKeyReceiver"/>を登録順に積み上げて表示し、
    /// 現在バックキーを受け取るアクティブなBackHandlerを示すエディタウィンドウ。
    /// </summary>
    public class BackKeyEventViewerWindow : EditorWindow
    {
        [MenuItem("Window/Supplement/BackKey Event Viewer")]
        private static void Open()
        {
            GetWindow<BackKeyEventViewerWindow>("BackKey Event Viewer");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode中のみ表示されます。", MessageType.Info);
                return;
            }

            var registry = FindRegistry();
            if (registry == null)
            {
                EditorGUILayout.HelpBox(
                    $"{nameof(BackKeyReceiverDebugRegistry)}が登録されたLifetimeScopeが見つかりません。", MessageType.Warning);
                return;
            }

            var receivers = registry.Receivers;
            if (receivers.Count == 0)
            {
                EditorGUILayout.HelpBox("登録されているBackKeyReceiverはありません。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Registered順(下ほど後からRegister)", EditorStyles.boldLabel);

            for (var i = 0; i < receivers.Count; i++)
            {
                var receiver = receivers[i];
                var isActive = receiver != null && receiver.IsTargetOnTop();

                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    var label = receiver != null ? receiver.name : "(destroyed)";
                    var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;

                    EditorGUILayout.LabelField($"{i}: {label}", style);

                    if (isActive)
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("ACTIVE", style, GUILayout.Width(60));
                    }
                }
            }
        }

        private static BackKeyReceiverDebugRegistry FindRegistry()
        {
            var scopes = Object.FindObjectsByType<LifetimeScope>();
            foreach (var scope in scopes)
            {
                if (scope.Container != null && scope.Container.TryResolve<BackKeyReceiverDebugRegistry>(out var registry))
                {
                    return registry;
                }
            }
            return null;
        }
    }
}
