using UnityEngine;

namespace OrangeCat {
internal static class DefeatMenu {
    internal static readonly Rect RetryArea = new(417, 400, 214, 55);
    internal static readonly Rect TitleArea = new(647, 400, 214, 55);
    static GUIStyle heading, caption, button;

    internal static void Draw() {
        if (Event.current.type != EventType.Repaint) return;
        heading ??= Style(54, new Color32(255, 232, 189, 255), true);
        caption ??= Style(19, new Color32(189, 200, 215, 255), false);
        button ??= Style(19, new Color32(242, 221, 186, 255), true);
        var matrix = MenuCanvas.Begin();
        MenuCanvas.Fill(new Rect(0, 0, 1280, 720), new Color32(5, 11, 24, 168));
        MenuCanvas.Fill(new Rect(376, 217, 528, 271), new Color32(14, 25, 43, 245));
        MenuCanvas.Fill(new Rect(376, 217, 528, 3), new Color32(240, 177, 88, 255));
        GUI.Label(new Rect(390, 247, 500, 67), "OUT OF HP", heading);
        GUI.Label(new Rect(390, 320, 500, 40), "Pick yourself up and try this stage again.", caption);
        DrawButton(RetryArea, "Retry  /  R");
        DrawButton(TitleArea, "Title  /  T");
        GUI.matrix = matrix;
    }

    static void DrawButton(Rect rect, string text) {
        MenuCanvas.Fill(rect, rect.Contains(MenuCanvas.Pointer) ? new Color32(68, 84, 97, 255)
            : new Color32(33, 52, 66, 255));
        GUI.Label(rect, text, button);
    }

    static GUIStyle Style(int size, Color color, bool bold) {
        var style = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal
        };
        style.normal.textColor = color;
        return style;
    }
}
}
