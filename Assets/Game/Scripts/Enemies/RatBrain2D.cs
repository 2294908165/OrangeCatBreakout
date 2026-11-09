using UnityEngine;

namespace OrangeCat {
public enum RatState { Waiting, Approach, Windup, Charge, Recovery, Hurt, Dead }

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(AttackHitbox2D))]
public sealed class RatBrain2D : MonoBehaviour, IParryable {
    [SerializeField] AttackDefinition charge;
    [SerializeField, Min(0)] float wakeDistance = 5f, strikeDistance = 2f;
    [SerializeField, Min(0)] float approachSpeed = 2.6f, chargeSpeed = 7f;
    [SerializeField, Min(0)] float hurtTime = .18f, parryRecovery = .75f;
    [SerializeField] bool restrictRange;
    [SerializeField] Vector2 range;
    Rigidbody2D body;
    Health health, target;
    AttackHitbox2D weapon;
    Vector2 knockback;
    float forcedDelay;

    public RatState State { get; private set; }
    public bool Engaged { get; private set; }
    public int Facing { get; private set; } = -1;
    public float ChargeReach => chargeSpeed * charge.Active + charge.HitboxOffset.x + charge.HitboxSize.x * .5f;
    public Health Life => health;
    public AttackHitbox2D Weapon => weapon;

    public void Configure(AttackDefinition attack) { charge = attack; }
    public void Track(Health player) { target = player; }
    public void ConfigureRange(Vector2 bounds, float wake) {
        restrictRange = true;
        range = bounds;
        wakeDistance = wake;
    }

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        weapon = GetComponent<AttackHitbox2D>();
    }

    void OnEnable() {
        health.Damaged += OnDamage;
        health.Died += OnDeath;
    }

    void OnDisable() {
        health.Damaged -= OnDamage;
        health.Died -= OnDeath;
        weapon.Cancel();
    }

    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;
        if (State == RatState.Dead) {
            body.linearVelocityX = 0;
            return;
        }
        if (forcedDelay > 0) {
            forcedDelay = Mathf.Max(0, forcedDelay - dt);
            if (knockback != Vector2.zero) {
                body.linearVelocity = new Vector2(LimitHorizontal(knockback.x, dt), knockback.y);
                knockback = Vector2.zero;
            } else body.linearVelocityX = Mathf.MoveTowards(body.linearVelocityX, 0, 30 * dt);
            return;
        }
        if (weapon.IsRunning) {
            weapon.Step(dt);
            // A perfect guard can cancel the active hit synchronously during Step.
            if (forcedDelay > 0) return;
            State = weapon.Stage switch {
                AttackStage.Startup => RatState.Windup,
                AttackStage.Active => RatState.Charge,
                AttackStage.Recovery => RatState.Recovery,
                _ => RatState.Approach
            };
            body.linearVelocityX = State == RatState.Charge ? LimitHorizontal(Facing * chargeSpeed, dt) : 0;
            return;
        }

        body.linearVelocityX = 0;
        if (target == null || target.IsDead) {
            State = RatState.Waiting;
            return;
        }
        Vector2 distance = (Vector2)target.transform.position - body.position;
        if (!Engaged && Mathf.Abs(distance.x) > wakeDistance) {
            State = RatState.Waiting;
            return;
        }
        Engaged = true;
        if (Mathf.Abs(distance.x) > .04f) Facing = distance.x > 0 ? 1 : -1;
        if (Mathf.Abs(distance.x) <= strikeDistance && Mathf.Abs(distance.y) < .9f && weapon.Begin(charge, Facing)) {
            State = RatState.Windup;
            return;
        }
        State = RatState.Approach;
        if (Mathf.Abs(distance.x) > .45f) body.linearVelocityX = LimitHorizontal(Facing * approachSpeed, dt);
    }

    float LimitHorizontal(float requested, float dt) {
        if (!restrictRange) return requested;
        float x = body.position.x;
        return (Mathf.Clamp(x + requested * dt, range.x, range.y) - x) / dt;
    }

    public void OnParried() {
        if (health.IsDead) return;
        weapon.Cancel();
        State = RatState.Recovery;
        forcedDelay = parryRecovery;
        knockback = Vector2.zero;
        body.linearVelocityX = 0;
    }

    void OnDamage(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (health.IsDead || amount <= 0) return;
        weapon.Cancel();
        State = RatState.Hurt;
        forcedDelay = hurtTime;
        knockback = new Vector2(damage.Direction.x * damage.Knockback * .70f, damage.Knockback * .12f);
    }

    void OnDeath() {
        weapon.Cancel();
        State = RatState.Dead;
        knockback = Vector2.zero;
        body.linearVelocityX = 0;
    }
}
}
