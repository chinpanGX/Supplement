using Supplement.Core;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="BackKeyReceiver"/> のデバッグオーバーレイ表示のOn/Off状態。
    /// <see cref="IDebugSettingsStore"/> 経由で値を永続化する。
    /// </summary>
    public sealed class BackKeyDebugOverlayState : IDebugOverlayState
    {
        private const string PrefKey = "Supplement.BackKey.DebugOverlay.Enabled";

        private readonly IDebugSettingsStore settingsStore;
        private bool enabled;

        public BackKeyDebugOverlayState(IDebugSettingsStore settingsStore)
        {
            this.settingsStore = settingsStore;
            enabled = settingsStore.GetBool(PrefKey, true);
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                settingsStore.SetBool(PrefKey, value);
            }
        }
    }
}
