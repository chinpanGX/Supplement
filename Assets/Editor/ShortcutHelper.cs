using UnityEditor;
using UnityEngine;

namespace Supplement.Editor
{
    internal static class ShortcutHelper
    {
        [MenuItem("Supplement/Shortcuts/Persistent Data Path")]
        private static void OpenPersistentDataPath()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
