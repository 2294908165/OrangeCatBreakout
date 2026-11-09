using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OrangeCat {
public sealed class TitleScreen : MonoBehaviour {
    [SerializeField] AudioSource music;
    [SerializeField] Texture2D artwork, startLabel, musicOnLabel, musicOffLabel;
    [SerializeField] string firstStage = "Warehouse01";
    static readonly Rect StartArea = new(92, 463, 334, 72);
    static readonly Rect SoundArea = new(1040, 44, 168, 44);
    bool loading;

    public void Configure(AudioSource source, Texture2D layout, Texture2D start, Texture2D on, Texture2D off) {
        music = source;
        artwork = layout;
        startLabel = start;
        musicOnLabel = on;
        musicOffLabel = off;
    }

    void Update() {
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        if ((keyboard != null && keyboard.mKey.wasPressedThisFrame) || MenuCanvas.Clicked(SoundArea)) ToggleMusic();
        if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
            (pad != null && pad.buttonSouth.wasPressedThisFrame) || MenuCanvas.Clicked(StartArea)) Begin();
    }

    void Begin() {
        if (loading) return;
        loading = true;
        SceneManager.LoadSceneAsync(firstStage);
    }

    void ToggleMusic() { music.mute = !music.mute; }

    void OnGUI() {
        if (Event.current.type != EventType.Repaint) return;
        var matrix = MenuCanvas.Begin();
        GUI.DrawTexture(new Rect(0, 0, 1280, 720), artwork);
        Vector2 pointer = MenuCanvas.Pointer;
        MenuCanvas.DrawButton(StartArea, startLabel, StartArea.Contains(pointer) ? new Color32(255, 211, 124, 255)
            : new Color32(240, 177, 88, 255));
        MenuCanvas.DrawButton(SoundArea, music.mute ? musicOffLabel : musicOnLabel, SoundArea.Contains(pointer)
            ? new Color32(30, 65, 76, 255) : new Color32(14, 27, 40, 236));
        GUI.matrix = matrix;
    }

}
}
