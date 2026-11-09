using System;
using UnityEngine;

namespace OrangeCat {
public enum BellyLaughState { Waiting, Windup, Bumping, Recovery, Hurt, Laughing, Dead, Guarding }

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(AttackHitbox2D))]
public sealed class BellyLaughBrain2D : MonoBehaviour, IParryable {
    [SerializeField] AttackDefinition bump;
    [SerializeField] LayerMask sightBlockers;
    [SerializeField, Min(.1f)] float sightRange = 4.5f, sightHeight = 1.8f;
    [SerializeField, Min(.02f)] float guardReadyTime = .24f;
    [SerializeField, Min(.1f)] float strikeDistance = 1.75f, bumpSpeed = 4.4f, lungeRange = 1.15f;
    [SerializeField, Min(.02f)] float recoilDuration = .12f, laughDuration = .44f;
    Rigidbody2D body;
    AttackHitbox2D weapon;
    Health target;
    float origin;

    public Health Life { get; private set; }
    public BellyLaughState State { get; private set; }
    public float Elapsed { get; private set; }
    public int Facing { get; private set; } = -1;
    public bool Engaged { get; private set; }
    public event Action LaughStarted;

    public void Configure(AttackDefinition attack, LayerMask blockers) {
        bump = attack;
        sightBlockers = blockers;
    }
    public void Track(Health player) => target = player;

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        weapon = GetComponent<AttackHitbox2D>();
        Life = GetComponent<Health>();
        origin = body.position.x;
    }

    void OnEnable() {
        Life.Damaged += OnDamage;
        Life.Died += OnDeath;
    }

    void OnDisable() {
        Life.Damaged -= OnDamage;
        Life.Died -= OnDeath;
        weapon.Cancel();
    }

    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;
        Elapsed += dt;
        switch (State) {
            case BellyLaughState.Waiting:
                if (TrySeeTarget(false, out var noticed)) Guard(noticed.x);
                break;
            case BellyLaughState.Guarding:
                WatchTarget();
                break;
            case BellyLaughState.Windup:
            case BellyLaughState.Bumping:
                StepBump(dt);
                break;
            case BellyLaughState.Recovery:
                if (Elapsed >= bump.Recovery) ResumeWatching();
                break;
            case BellyLaughState.Hurt:
                if (Elapsed >= recoilDuration) Enter(BellyLaughState.Laughing);
                break;
            case BellyLaughState.Laughing:
                if (Elapsed >= laughDuration) ResumeWatching();
                break;
        }
    }

    bool TrySeeTarget(bool tracking, out Vector2 offset) {
        offset = default;
        if (target == null || target.IsDead) return false;
        Vector2 point = target.transform.position;
        offset = point - body.position;
        float range = sightRange + (tracking ? .65f : 0);
        if (Mathf.Abs(offset.x) > range || Mathf.Abs(offset.y) > sightHeight ||
            (!tracking && offset.x * Facing < 0)) return false;
        return Physics2D.Linecast(body.position + Vector2.up * .45f, point, sightBlockers).collider == null;
    }

    void WatchTarget() {
        if (!TrySeeTarget(true, out var offset)) {
            Enter(BellyLaughState.Waiting);
            return;
        }
        Face(offset.x);
        if (Elapsed >= guardReadyTime && Mathf.Abs(offset.x) <= strikeDistance && Mathf.Abs(offset.y) <= .85f &&
            weapon.Begin(bump, Facing)) Enter(BellyLaughState.Windup);
    }

    void Guard(float targetSide) {
        Face(targetSide);
        Engaged = true;
        Enter(BellyLaughState.Guarding);
    }

    void ResumeWatching() {
        if (TrySeeTarget(true, out var offset)) Guard(offset.x);
        else Enter(BellyLaughState.Waiting);
    }

    void Face(float x) {
        if (Mathf.Abs(x) > .04f) Facing = x < 0 ? -1 : 1;
    }

    void StepBump(float dt) {
        weapon.Step(dt);
        if (State == BellyLaughState.Recovery) return; // Perfect guard may interrupt synchronously.
        if (weapon.Stage == AttackStage.Startup) return;
        if (weapon.Stage != AttackStage.Active) {
            FinishBump();
            return;
        }
        if (State == BellyLaughState.Windup) Enter(BellyLaughState.Bumping);
        float x = Mathf.Clamp(body.position.x + Facing * bumpSpeed * dt, origin - lungeRange, origin + lungeRange);
        body.MovePosition(new Vector2(x, body.position.y));
    }

    public void OnParried() {
        if (State == BellyLaughState.Bumping || State == BellyLaughState.Windup) FinishBump();
    }

    void FinishBump() {
        weapon.Cancel();
        Enter(BellyLaughState.Recovery);
    }

    void OnDamage(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (amount <= 0 || Life.IsDead) return;
        Engaged = true;
        weapon.Cancel();
        Enter(BellyLaughState.Hurt);
    }

    void OnDeath() {
        weapon.Cancel();
        Enter(BellyLaughState.Dead);
    }

    void Enter(BellyLaughState state) {
        State = state;
        Elapsed = 0;
        if (state == BellyLaughState.Laughing) LaughStarted?.Invoke();
    }
}
}
