using UnityEngine;
using UnityEngine.InputSystem;

namespace OrangeCat {
internal static class MenuCanvas {
    static float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
    static Vector2 Offset => new((Screen.width - 1280 * Scale) * .5f, (Screen.height - 720 * Scale) * .5f);

    internal static Vector2 Pointer {
        get {
            if (Mouse.current == null) return new Vector2(-1, -1);
            Vector2 pixel = Mouse.current.position.ReadValue();
            pixel.y = Screen.height - pixel.y;
            return (pixel - Offset) / Scale;
        }
    }

    internal static bool Clicked(Rect rect) => Mouse.current != null &&
        Mouse.current.leftButton.wasPressedThisFrame && rect.Contains(Pointer);

    internal static Matrix4x4 Begin() {
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Offset, Quaternion.identity, new Vector3(Scale, Scale, 1));
        return previous;
    }

    internal static void Fill(Rect rect, Color color) {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    internal static void DrawButton(Rect rect, Texture2D label, Color fill) {
        Fill(new Rect(rect.x, rect.y + 5, rect.width, rect.height), new Color32(8, 13, 23, 225));
        Fill(rect, fill);
        GUI.DrawTexture(rect, label);
    }
}
}
