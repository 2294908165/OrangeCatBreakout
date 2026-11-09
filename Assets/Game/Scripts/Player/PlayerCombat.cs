using System;
using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(PlayerMotor2D), typeof(Health), typeof(AttackHitbox2D))]
public sealed class PlayerCombat : MonoBehaviour {
    PlayerMotor2D motor;
    PlayerStateMachine states;
    Health health;
    AttackHitbox2D weapon;
    float hurtProtection;
    bool guardNeedsRelease, comboQueued;
    CharacterDefinition Data => motor.Definition;
    public bool IsInvulnerable => motor.Invincible || hurtProtection > 0;
    public bool IsGuarding => states.Current == PlayerState.Guarding;
    public bool IsPerfectGuardWindow => IsGuarding && states.Elapsed < Data.PerfectGuardDuration;
    public float CounterRemaining { get; private set; }
    public float HissCooldownRemaining { get; private set; }
    public bool CanCounterAttack => CounterRemaining > 0;
    public bool CanRollCancel => states.Current == PlayerState.Attacking &&
        weapon.Definition != null && weapon.Definition.CanRollCancel(weapon.Elapsed);
    public float GuardDamageMultiplier => Data.GuardDamageMultiplier;
    public AttackHitbox2D Weapon => weapon;
    public event Action PerfectGuarded;

    void Awake() {
        motor = GetComponent<PlayerMotor2D>();
        health = GetComponent<Health>();
        weapon = GetComponent<AttackHitbox2D>();
        states = motor.States;
    }

    void OnEnable() {
        states.Changed += OnStateChanged;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable() {
        states.Changed -= OnStateChanged;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        weapon.Cancel();
        comboQueued = false;
        CounterRemaining = hurtProtection = 0;
        if (states.Current == PlayerState.Attacking || IsGuarding) states.TryEnter(PlayerState.Free);
    }

    // Called by the motor on the same fixed clock as movement, after its ground probe.
    public void Step(PlayerCommand command, float dt) {
        hurtProtection = Mathf.Max(0, hurtProtection - dt);
        CounterRemaining = Mathf.Max(0, CounterRemaining - dt);
        HissCooldownRemaining = Mathf.Max(0, HissCooldownRemaining - dt);
        if (!command.GuardHeld) guardNeedsRelease = false;
        if (states.Current == PlayerState.Hurt && states.Elapsed + .00001f >= Data.HurtDuration)
            states.TryEnter(PlayerState.Free);

        if (states.Current == PlayerState.Attacking) {
            AdvanceAttack(command.AttackPressed, dt);
            return;
        }

        if (IsGuarding) {
            if (command.AttackPressed && CanCounterAttack) {
                BeginAttack(Data.CounterAttack);
                return;
            }
            if (!command.GuardHeld || !motor.Grounded) states.TryEnter(PlayerState.Free);
        }

        if (states.Current != PlayerState.Free) return;
        if (command.HissPressed && motor.Grounded && HissCooldownRemaining <= 0 && BeginAttack(Data.HissAttack)) {
            HissCooldownRemaining = Data.HissCooldown;
        } else if (command.AttackPressed) {
            BeginAttack(CanCounterAttack ? Data.CounterAttack : Data.PrimaryAttack);
        } else if (Data.SupportsGuard && motor.Grounded && command.GuardHeld && !guardNeedsRelease) {
            guardNeedsRelease = states.TryEnter(PlayerState.Guarding);
        }
    }

    bool BeginAttack(AttackDefinition attack) {
        if (attack == null || !states.TryEnter(PlayerState.Attacking)) return false;
        if (!weapon.Begin(attack, motor.Facing)) {
            states.TryEnter(PlayerState.Free);
            return false;
        }
        if (attack == Data.CounterAttack) CounterRemaining = 0;
        return true;
    }

    void AdvanceAttack(bool pressed, float dt) {
        // One extra press per stage. The finisher has no successor.
        var current = weapon.Definition;
        var followup = current == Data.PrimaryAttack ? Data.ComboAttack
            : current == Data.ComboAttack ? Data.FinisherAttack : null;
        if (pressed && followup != null && weapon.Elapsed + dt >= Data.ComboInputStart)
            comboQueued = true;
        weapon.Step(dt);
        if (states.Current != PlayerState.Attacking || weapon.IsRunning) return;
        if (comboQueued) {
            comboQueued = false;
            if (weapon.Begin(followup, motor.Facing)) return;
        }
        states.TryEnter(PlayerState.Free);
    }

    public DamageOutcome ResolveDefense(DamageInfo damage) {
        if (IsInvulnerable) return DamageOutcome.Invulnerable;
        if (!IsGuarding || !damage.Blockable || !FromFront(damage)) return DamageOutcome.Hit;
        if (!IsPerfectGuardWindow) return DamageOutcome.Blocked;

        // One slot. Holding the guard key through interruption cannot reopen the window.
        CounterRemaining = Data.CounterDuration;
        if (damage.Source != null && damage.Source.TryGetComponent<IParryable>(out var source))
            source.OnParried();
        PerfectGuarded?.Invoke();
        return DamageOutcome.PerfectGuard;
    }

    bool FromFront(DamageInfo damage) {
        float side = damage.Source != null ? damage.Source.transform.position.x - transform.position.x : 0;
        if (Mathf.Abs(side) < .001f) side = -damage.Direction.x;
        return side * motor.Facing > 0;
    }

    void OnDamaged(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (health.IsDead || amount <= 0 || outcome == DamageOutcome.Blocked) return;
        hurtProtection = Data.HurtInvulnerability;
        states.TryEnter(PlayerState.Hurt);
        motor.QueueKnockback(new Vector2(damage.Direction.x * damage.Knockback, damage.Knockback * .22f));
    }

    void OnDied() {
        CounterRemaining = hurtProtection = 0;
        states.TryEnter(PlayerState.Dead);
    }

    void OnStateChanged(PlayerState previous, PlayerState next) {
        if (previous != PlayerState.Attacking) return;
        weapon.Cancel();
        comboQueued = false;
    }
}
}
