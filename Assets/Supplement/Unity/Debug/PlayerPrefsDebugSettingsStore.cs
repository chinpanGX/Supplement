using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="PlayerPrefs"/> を使って <see cref="IDebugSettingsStore"/> を実装する。
    /// </summary>
    public sealed class PlayerPrefsDebugSettingsStore : IDebugSettingsStore
    {
        public bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }
    }
}
