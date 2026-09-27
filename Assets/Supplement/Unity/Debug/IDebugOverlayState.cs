namespace Supplement.Core
{
    /// <summary>
    /// デバッグ用オーバーレイ表示のOn/Off状態を表す。
    /// </summary>
    public interface IDebugOverlayState
    {
        bool Enabled { get; set; }
    }
}
