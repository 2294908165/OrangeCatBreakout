using UnityEngine;
using UnityEngine.InputSystem;
namespace OrangeCat {
public readonly struct PlayerCommand {
    public readonly float Move;
    public readonly bool JumpPressed, JumpHeld, RollPressed, AttackPressed, GuardHeld, HissPressed;
    public PlayerCommand(float move, bool jumpPressed, bool jumpHeld, bool rollPressed,
        bool attackPressed = false, bool guardHeld = false, bool hissPressed = false) {
        Move = move; JumpPressed = jumpPressed; JumpHeld = jumpHeld; RollPressed = rollPressed;
        AttackPressed = attackPressed; GuardHeld = guardHeld;
        HissPressed = hissPressed;
    }
}
public sealed class PlayerInputReader : MonoBehaviour {
    [SerializeField] InputActionAsset actions;
    InputAction move, jump, roll, attack, guard, hiss;
    bool jumpQueued, rollQueued, attackQueued, guardHeld, hissQueued;
    float moveValue;
    bool jumpHeld;
    public InputActionAsset Actions => actions;
    public void Configure(InputActionAsset asset) { actions = asset; }
    void Awake() {
        if (actions == null) { Debug.LogError("Missing input asset", this); enabled = false; return; }
        // Each player owns an instance, avoiding shared enable/disable lifetimes.
        actions = Instantiate(actions);
        move = actions.FindAction("Gameplay/Move", true);
        jump = actions.FindAction("Gameplay/Jump", true);
        roll = actions.FindAction("Gameplay/Roll", true);
        attack = actions.FindAction("Gameplay/Attack", true);
        guard = actions.FindAction("Gameplay/Guard", true);
        hiss = actions.FindAction("Gameplay/Hiss", true);
    }
    void OnEnable() { if (actions != null) actions.Enable(); }
    void OnDisable() { if (actions != null) actions.Disable(); Clear(); }
    void Update() {
        if (Time.timeScale == 0) { Clear(); return; }
        moveValue = move.ReadValue<float>(); jumpHeld = jump.IsPressed();
        jumpQueued |= jump.WasPressedThisFrame(); rollQueued |= roll.WasPressedThisFrame();
        attackQueued |= attack.WasPressedThisFrame(); guardHeld = guard.IsPressed();
        hissQueued |= hiss.WasPressedThisFrame();
    }
    public PlayerCommand Consume() {
        var command = new PlayerCommand(moveValue, jumpQueued, jumpHeld, rollQueued, attackQueued, guardHeld, hissQueued);
        jumpQueued = rollQueued = attackQueued = hissQueued = false; return command;
    }
    public void Clear() { jumpQueued = rollQueued = jumpHeld = attackQueued = guardHeld = hissQueued = false; moveValue = 0; }
    void OnDestroy() { if (actions != null) Destroy(actions); }
}
}
