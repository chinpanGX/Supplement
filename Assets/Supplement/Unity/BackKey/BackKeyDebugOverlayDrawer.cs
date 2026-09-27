using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="BackKeyReceiver"/> の判定に使うサンプル点をGame View上に描画する。
    /// </summary>
    /// <remarks>
    /// 判定はScreen座標系（<see cref="Screen.width"/>/<see cref="Screen.height"/>基準）で行っているため、
    /// Scene ViewのGizmoではなくOnGUIでGame View上に直接描画する。
    /// </remarks>
    internal static class BackKeyDebugOverlayDrawer
    {
        private const float DotSize = 8f;

        public static void Draw(BackKeyReceiver receiver)
        {
            var points = receiver.GetSamplePoints();

            foreach (var point in points)
            {
                DrawDot(point, receiver.IsPointOnTop(point) ? Color.green : Color.red);
            }
        }

        private static void DrawDot(Vector2 screenPoint, Color color)
        {
            // screenPointはRectTransformUtility.WorldToScreenPointと同じ、左下原点のScreen座標系。
            // OnGUI/GUIは左上原点でYが下向きなので、Yのみ反転する(Xはそのまま)。
            var guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(guiPoint.x - DotSize / 2f, guiPoint.y - DotSize / 2f, DotSize, DotSize), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
