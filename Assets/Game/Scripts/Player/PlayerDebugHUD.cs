using UnityEngine;
using UnityEngine.InputSystem;
namespace OrangeCat {
public sealed class PlayerDebugHUD : MonoBehaviour {
    [SerializeField] PlayerMotor2D player;
    bool visible = true;
    public void Configure(PlayerMotor2D value) { player = value; }
    void Update() { if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible; }
    void OnGUI() {
        if (!visible || player == null) return;
        GUI.Box(new Rect(12,12,420,145), $"{player.Definition.DisplayName} | Phase 0/1\nState: {player.States.Current} ({player.States.Elapsed:F2}s)\nGrounded: {player.Grounded} | Jumps: {player.JumpsUsed}\nRoll invincible: {player.Invincible}\nA/D: Move  Space: Jump  Shift: Roll  Esc: Pause\nF1: HUD | Combat actions reserved for Phase 2");
    }
}
}
