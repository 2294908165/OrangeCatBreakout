using UnityEngine;

namespace OrangeCat {
public enum ColdFlyState { Waiting, Hover, Windup, Dive, Recovery, Hurt, Dead }

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(AttackHitbox2D))]
public sealed class ColdFlyBrain2D : MonoBehaviour, IParryable {
    [SerializeField] AttackDefinition dive;
    [SerializeField] Vector2 arena = new Vector2(-4.8f, 6.6f);
    [SerializeField, Min(0)] float wakeDistance = 4.8f, hoverSpeed = 3.5f, hoverHeight = 1.65f;
    [SerializeField, Min(0)] float parryRecovery = .82f, hurtTime = .18f;
    Rigidbody2D body;
    Health target, life;
    AttackHitbox2D weapon;
    Vector2 diveStart, diveEnd;
    float forcedRecovery;

    public ColdFlyState State { get; private set; }
    public bool Engaged { get; private set; }
    public int Facing { get; private set; } = -1;
    public Health Life => life;
    public AttackHitbox2D Weapon => weapon;
    public Vector2 DiveEndpoint => diveEnd;

    public void Configure(AttackDefinition attack) { dive = attack; }
    public void Track(Health player) { target = player; }
    public void ConfigureArena(Vector2 bounds, float wake) {
        arena = bounds;
        wakeDistance = wake;
    }

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        life = GetComponent<Health>();
        weapon = GetComponent<AttackHitbox2D>();
    }

    void OnEnable() {
        life.Damaged += OnHit;
        life.Died += OnDeath;
    }

    void OnDisable() {
        life.Damaged -= OnHit;
        life.Died -= OnDeath;
        weapon.Cancel();
    }

    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;
        if (State == ColdFlyState.Dead) return;
        if (forcedRecovery > 0) {
            forcedRecovery = Mathf.Max(0, forcedRecovery - dt);
            if (State == ColdFlyState.Recovery) {
                // After a parry, settle into ground-level counter range.
                float descent = Mathf.Clamp((body.position.y - .16f) / dt, 0, 4);
                body.linearVelocity = Vector2.down * descent;
            } else body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, 14 * dt);
            return;
        }
        if (weapon.IsRunning) {
            TickDive(dt);
            return;
        }
        if (target == null || target.IsDead) {
            body.linearVelocity = Vector2.zero;
            State = ColdFlyState.Waiting;
            return;
        }
        Vector2 player = target.transform.position;
        float horizontal = player.x - body.position.x;
        if (!Engaged && Mathf.Abs(horizontal) > wakeDistance) {
            State = ColdFlyState.Waiting;
            body.linearVelocity = Vector2.zero;
            return;
        }
        Engaged = true;
        if (Mathf.Abs(horizontal) > .06f) Facing = horizontal > 0 ? 1 : -1;
        Vector2 perch = new Vector2(Mathf.Clamp(player.x - Facing * 1.15f, arena.x, arena.y), hoverHeight);
        Vector2 error = perch - body.position;
        if (error.sqrMagnitude <= .17f * .17f && Mathf.Abs(horizontal) <= 1.55f && weapon.Begin(dive, Facing)) {
            body.linearVelocity = Vector2.zero;
            diveStart = body.position;
            diveEnd = new Vector2(Mathf.Clamp(player.x + Facing * .55f, arena.x, arena.y),
                Mathf.Clamp(player.y - .02f, -.16f, 1.2f));
            State = ColdFlyState.Windup;
            return;
        }
        State = ColdFlyState.Hover;
        body.linearVelocity = Vector2.ClampMagnitude(error / dt, hoverSpeed);
    }

    void TickDive(float dt) {
        weapon.Step(dt);
        if (forcedRecovery > 0) return; // The damage callback can parry inside Step.
        State = weapon.Stage switch {
            AttackStage.Startup => ColdFlyState.Windup,
            AttackStage.Active => ColdFlyState.Dive,
            AttackStage.Recovery => ColdFlyState.Recovery,
            _ => ColdFlyState.Hover
        };
        if (State == ColdFlyState.Dive) {
            float progress = Mathf.Clamp01((weapon.Elapsed + dt - dive.Startup) / dive.Active);
            var next = Vector2.Lerp(diveStart, diveEnd, progress);
            body.linearVelocity = Vector2.ClampMagnitude((next - body.position) / dt, 13);
        } else body.linearVelocity = Vector2.zero;
    }

    public void OnParried() {
        if (life.IsDead) return;
        weapon.Cancel();
        State = ColdFlyState.Recovery;
        forcedRecovery = parryRecovery;
        body.linearVelocity = Vector2.zero;
    }

    void OnHit(DamageInfo info, float amount, DamageOutcome outcome) {
        if (life.IsDead || amount <= 0) return;
        weapon.Cancel();
        State = ColdFlyState.Hurt;
        forcedRecovery = hurtTime;
        body.linearVelocity = info.Direction * (info.Knockback * .65f);
    }

    void OnDeath() {
        weapon.Cancel();
        State = ColdFlyState.Dead;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 3;
    }
}
}
