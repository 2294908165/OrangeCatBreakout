using System;
namespace OrangeCat {
public enum PlayerState { Free, Attacking, Rolling, Guarding, Hurt, Dead }
// Animator may observe this state; it never owns transitions.
public sealed class PlayerStateMachine {
    public PlayerState Current { get; private set; } = PlayerState.Free;
    public float Elapsed { get; private set; }
    public event Action<PlayerState, PlayerState> Changed;
    public bool TryEnter(PlayerState next, bool allowAttackRollCancel = false) {
        if (Current == PlayerState.Dead || Current == next) return false;
        bool allowed = next == PlayerState.Dead || next == PlayerState.Hurt ||
            Current == PlayerState.Free || next == PlayerState.Free ||
            (Current == PlayerState.Attacking && next == PlayerState.Rolling && allowAttackRollCancel) ||
            (Current == PlayerState.Guarding && next == PlayerState.Attacking);
        if (!allowed) return false;
        var previous = Current; Current = next; Elapsed = 0;
        Changed?.Invoke(previous, next); return true;
    }
    public void Tick(float delta) { Elapsed += Math.Max(0, delta); }
}
}
