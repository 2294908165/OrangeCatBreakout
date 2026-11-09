using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OrangeCat {
public sealed class EndingScreen : MonoBehaviour {
    [SerializeField] Texture2D backdrop, artwork, replay, title;
    static readonly Rect ReplayArea = new(92, 482, 205, 60);
    static readonly Rect TitleArea = new(313, 482, 205, 60);
    float started;
    bool loading;

    public void Configure(Texture2D scene, Texture2D layout, Texture2D retryLabel, Texture2D titleLabel) {
        backdrop = scene;
        artwork = layout;
        replay = retryLabel;
        title = titleLabel;
    }

    void Awake() { started = Time.unscaledTime; }

    void Update() {
        if (loading) return;
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (pad != null && pad.buttonEast.wasPressedThisFrame) || MenuCanvas.Clicked(TitleArea))
            Load("TitleScreen");
        else if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)) ||
            (pad != null && pad.buttonSouth.wasPressedThisFrame) || MenuCanvas.Clicked(ReplayArea))
            Load("Warehouse01");
    }

    void Load(string scene) {
        loading = true;
        SceneManager.LoadSceneAsync(scene);
    }

    void OnGUI() {
        if (Event.current.type != EventType.Repaint) return;
        var matrix = MenuCanvas.Begin();
        var canvas = new Rect(0, 0, 1280, 720);
        GUI.DrawTexture(canvas, backdrop);
        float time = Time.unscaledTime - started;
        for (int i = 0; i < 15; i++) {
            float x = 706 + (i * 71) % 490 + Mathf.Sin(time * .8f + i) * 17;
            float y = 535 - Mathf.Repeat(i * 39 + time * (12 + i % 5), 402);
            float alpha = Mathf.Pow(Mathf.Sin(time * .75f + i), 2) * .48f;
            int size = 2 + i % 3;
            MenuCanvas.Fill(new Rect(x, y, size, size), new Color(1, .79f, .43f, alpha));
        }
        GUI.DrawTexture(canvas, artwork);
        Vector2 pointer = MenuCanvas.Pointer;
        MenuCanvas.DrawButton(ReplayArea, replay, ReplayArea.Contains(pointer) ? new Color32(255, 216, 133, 255)
            : new Color32(240, 177, 88, 255));
        MenuCanvas.DrawButton(TitleArea, title, TitleArea.Contains(pointer) ? new Color32(40, 78, 77, 255)
            : new Color32(20, 43, 52, 255));
        MenuCanvas.Fill(canvas, new Color(0, 0, 0, 1 - Mathf.Clamp01(time / .75f)));
        GUI.matrix = matrix;
    }
}
}
