using UnityEngine;
using UnityEngine.InputSystem;
namespace OrangeCat {
public sealed class GameManager : MonoBehaviour {
    [SerializeField] PlayerInputReader input;
    public void Configure(PlayerInputReader reader) { input = reader; }
    public bool IsPaused { get; private set; }
    public void SetPaused(bool value) {
        IsPaused = value;
        Time.timeScale = value ? 0 : 1;
        if (input != null) input.Clear();
    }
    void Update() {
        if (input != null && input.Actions.FindAction("Gameplay/Pause", true).WasPressedThisFrame()) {
            SetPaused(!IsPaused);
        }
    }
    void OnGUI() { if (IsPaused) GUI.Box(new Rect(Screen.width/2-110, 30, 220, 40), "Paused - Esc / Start"); }
    void OnDestroy() { Time.timeScale = 1; }
}
}
