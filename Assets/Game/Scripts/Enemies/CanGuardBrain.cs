using UnityEngine;

namespace OrangeCat {
public enum CanGuardState { Waiting, ChargeTell, Charging, SlamTell, Airborne, Recovering, Dead }

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(AttackHitbox2D))]
public sealed class CanGuardBrain : MonoBehaviour, IParryable {
    [SerializeField] AttackDefinition charge;
    [SerializeField] BossShockwave leftWave, rightWave;
    [SerializeField] Vector2 limits = new(-8.4f, 8.1f);
    [SerializeField] float floorY = -.5f;
    [SerializeField, Min(.1f)] float wakeDistance = 7f, chargeSpeed = 13f;
    [SerializeField, Min(.1f)] float slamTell = .72f, jumpTime = .72f, jumpHeight = 2.45f, slamRecovery = 1.65f;
    [SerializeField, Min(.1f)] float openingDelay = .4f;
    Rigidbody2D body;
    AttackHitbox2D weapon;
    Health target;
    float standingY, takeoffX;
    bool nextIsSlam;

    public Health Life { get; private set; }
    public CanGuardState State { get; private set; }
    public int Facing { get; private set; } = -1;
    public bool Engaged { get; private set; }
    public float Elapsed { get; private set; }
    public float LandingX { get; private set; }
    public float ChargeEndX => Facing < 0 ? limits.x : limits.y;
    public float FloorY => floorY;
    public bool RecoveredFromSlam { get; private set; }
    public float RecoveryDuration => RecoveredFromSlam ? slamRecovery : charge.Recovery;

    public void Configure(AttackDefinition attack, BossShockwave left, BossShockwave right, Vector2 arena, float groundY) {
        charge = attack;
        leftWave = left;
        rightWave = right;
        limits = arena;
        floorY = groundY;
    }

    public void Track(Health player) => target = player;

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        weapon = GetComponent<AttackHitbox2D>();
        Life = GetComponent<Health>();
        standingY = body.position.y;
    }

    void OnEnable() { Life.Died += Die; }
    void OnDisable() {
        Life.Died -= Die;
        StopAttacks();
    }

    void FixedUpdate() {
        if (State == CanGuardState.Dead) return;
        if (target == null || target.IsDead) {
            if (State == CanGuardState.Waiting) return;
            StopAttacks();
            body.MovePosition(new Vector2(body.position.x, standingY));
            Enter(CanGuardState.Waiting);
            return;
        }

        float dt = Time.fixedDeltaTime;
        Elapsed += dt;
        switch (State) {
            case CanGuardState.Waiting:
                if (Mathf.Abs(target.transform.position.x - body.position.x) > wakeDistance) {
                    Elapsed = 0;
                    break;
                }
                Engaged = true;
                if (Elapsed >= openingDelay) BeginNextAttack();
                break;
            case CanGuardState.ChargeTell:
            case CanGuardState.Charging:
                StepCharge(dt);
                break;
            case CanGuardState.SlamTell:
                if (Elapsed >= slamTell) Enter(CanGuardState.Airborne);
                break;
            case CanGuardState.Airborne:
                StepJump();
                break;
            case CanGuardState.Recovering:
                if (Elapsed >= RecoveryDuration) BeginNextAttack();
                break;
        }
    }

    void BeginNextAttack() {
        float dx = target.transform.position.x - body.position.x;
        if (Mathf.Abs(dx) > .05f) Facing = dx < 0 ? -1 : 1;
        if (nextIsSlam) {
            takeoffX = body.position.x;
            LandingX = Mathf.Clamp(target.transform.position.x, limits.x, limits.y);
            Enter(CanGuardState.SlamTell);
        } else {
            if (Mathf.Abs(body.position.x - ChargeEndX) < .25f) Facing = -Facing;
            weapon.Begin(charge, Facing);
            Enter(CanGuardState.ChargeTell);
        }
        nextIsSlam = !nextIsSlam;
    }

    void StepCharge(float dt) {
        weapon.Step(dt);
        if (State == CanGuardState.Recovering) return;
        if (weapon.Stage == AttackStage.Startup) return;
        if (weapon.Stage != AttackStage.Active) {
            Recover(false);
            return;
        }
        if (State != CanGuardState.Charging) Enter(CanGuardState.Charging);
        float x = Mathf.MoveTowards(body.position.x, ChargeEndX, chargeSpeed * dt);
        body.MovePosition(new Vector2(x, standingY));
        if (Mathf.Abs(body.position.x - ChargeEndX) < .01f) Recover(false);
    }

    void StepJump() {
        float progress = Mathf.Clamp01(Elapsed / jumpTime);
        float height = jumpHeight * 4 * progress * (1 - progress);
        body.MovePosition(new Vector2(Mathf.Lerp(takeoffX, LandingX, progress), standingY + height));
        if (progress < 1) return;
        leftWave.Launch(new Vector2(LandingX - .38f, floorY), -1);
        rightWave.Launch(new Vector2(LandingX + .38f, floorY), 1);
        Recover(true);
    }

    public void OnParried() {
        if (State == CanGuardState.Charging || State == CanGuardState.ChargeTell) Recover(false);
    }

    void Recover(bool slam) {
        weapon.Cancel();
        RecoveredFromSlam = slam;
        Enter(CanGuardState.Recovering);
    }

    void Die() {
        StopAttacks();
        body.position = new Vector2(body.position.x, standingY);
        Enter(CanGuardState.Dead);
    }

    void StopAttacks() {
        weapon.Cancel();
        if (leftWave != null) leftWave.Stop();
        if (rightWave != null) rightWave.Stop();
    }

    void Enter(CanGuardState state) {
        State = state;
        Elapsed = 0;
    }
}
}
