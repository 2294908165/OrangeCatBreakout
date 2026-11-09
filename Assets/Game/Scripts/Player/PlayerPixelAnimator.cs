using UnityEngine;

namespace OrangeCat {
// Presentation only. Combat and movement keep their own fixed-step clocks.
[RequireComponent(typeof(PlayerMotor2D), typeof(PlayerCombat), typeof(Rigidbody2D))]
public sealed class PlayerPixelAnimator : MonoBehaviour {
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Sprite idle, guard, primaryWindup, primaryStrike, comboWindup, comboStrike, counterWindup, counterStrike;
    [SerializeField] Sprite hurt, deathFall, deathDown;
    [SerializeField] Sprite jumpRise, jumpFall, jumpLand;
    [SerializeField] SpriteRenderer landingSplash;
    [SerializeField] Sprite finisherWindup, finisherStrike;
    [SerializeField] Sprite[] clawRecoveries, clawEffects;
    [SerializeField] SpriteRenderer clawTrails;
    [SerializeField, Min(0)] float collapseDuration = .16f;
    [SerializeField] Sprite[] idleLoop, run, roll;
    [SerializeField, Min(.04f)] float idleCycleDuration = .96f;
    [SerializeField, Min(.04f)] float airCurlDuration = .16f;
    [SerializeField, Min(0)] float airUncurlDuration = .08f;
    PlayerMotor2D motor;
    PlayerCombat combat;
    HissPresentation hiss;
    Rigidbody2D body;
    float idlePhase, runPhase;
    float airJumpElapsed;
    bool airJump;
    float landUntil, splashUntil;
    Vector3 splashPoint;
    // Offsets are relative to the feet of the character sprite.
    static readonly Vector2[] ClawTrailOffsets = {
        new(1.19f, .35f), new(1.09f, .90f), new(1.21f, .81f)
    };
    static readonly Vector2[] ClawTrailSizes = {
        new(1.10f, .44f), new(.89f, 1.33f), new(1.27f, 1.40f)
    };

    public void Configure(SpriteRenderer renderer, Sprite standing, Sprite[] running, Sprite[] rolling,
        Sprite defending, Sprite primaryPrepare, Sprite primaryHit, Sprite comboPrepare, Sprite comboHit,
        Sprite counterPrepare, Sprite counterHit) {
        visual = renderer;
        idle = standing;
        run = running;
        roll = rolling;
        guard = defending;
        primaryWindup = primaryPrepare;
        primaryStrike = primaryHit;
        comboWindup = comboPrepare;
        comboStrike = comboHit;
        counterWindup = counterPrepare;
        counterStrike = counterHit;
    }

    public void ConfigureHurtPose(Sprite frame) { hurt = frame; }
    public void ConfigureIdle(Sprite[] frames) { idleLoop = frames; }

    public void ConfigureDeathPoses(Sprite collapse, Sprite down) {
        deathFall = collapse;
        deathDown = down;
    }

    public void ConfigureJump(Sprite rise, Sprite fall, Sprite land, SpriteRenderer splash) {
        jumpRise = rise;
        jumpFall = fall;
        jumpLand = land;
        landingSplash = splash;
    }

    public void ConfigureClawChain(Sprite[] windups, Sprite[] strikes, Sprite[] recoveries,
        SpriteRenderer trails, Sprite[] effects) {
        primaryWindup = windups[0];
        primaryStrike = strikes[0];
        comboWindup = windups[1];
        comboStrike = strikes[1];
        finisherWindup = windups[2];
        finisherStrike = strikes[2];
        clawRecoveries = recoveries;
        clawTrails = trails;
        clawEffects = effects;
    }

    void Awake() {
        motor = GetComponent<PlayerMotor2D>();
        combat = GetComponent<PlayerCombat>();
        hiss = GetComponent<HissPresentation>();
        body = GetComponent<Rigidbody2D>();
    }

    void OnEnable() {
        motor.AirJumped += OnAirJumped;
        motor.Landed += OnLanded;
    }

    void OnDisable() {
        motor.AirJumped -= OnAirJumped;
        motor.Landed -= OnLanded;
        airJump = false;
        idlePhase = runPhase = 0;
        landUntil = splashUntil = 0;
        if (landingSplash != null) landingSplash.enabled = false;
        if (clawTrails != null) clawTrails.enabled = false;
    }

    void OnAirJumped() {
        airJump = roll != null && roll.Length >= 4;
        airJumpElapsed = 0;
    }

    void OnLanded() {
        if (motor.States.Current != PlayerState.Free) return;
        if (jumpLand != null) landUntil = Time.time + .11f;
        if (landingSplash == null) return;
        splashPoint = (Vector3)body.position + visual.transform.localPosition + Vector3.up * .07f;
        splashUntil = Time.time + .16f;
    }

    void LateUpdate() {
        if (motor.Grounded || motor.States.Current == PlayerState.Hurt || motor.States.Current == PlayerState.Dead)
            airJump = false;
        float speed = Mathf.Abs(body.linearVelocity.x - motor.SurfaceVelocity);
        bool freeOnGround = motor.States.Current == PlayerState.Free && motor.Grounded;
        if (!freeOnGround) landUntil = 0;
        bool running = freeOnGround && speed > .25f;
        bool idling = freeOnGround && !running && Time.time >= landUntil;
        if (running) {
            float strideRate = Mathf.Lerp(1.4f, 3.2f, speed / Mathf.Max(.01f, motor.Definition.MoveSpeed));
            runPhase = Mathf.Repeat(runPhase + Time.deltaTime * strideRate, 1);
        } else runPhase = 0;

        if (!idling) idlePhase = 0;
        var pose = SelectPose(running, idling);
        if (visual.sprite != pose) visual.sprite = pose;
        if (idling) idlePhase = Mathf.Repeat(idlePhase + Time.deltaTime / idleCycleDuration, 1);
        if (airJump) airJumpElapsed += Time.deltaTime;
        UpdateSplash();
        UpdateClawTrails();
    }

    Sprite SelectPose(bool running, bool idling) {
        switch (motor.States.Current) {
            case PlayerState.Rolling:
                return Frame(roll, motor.States.Elapsed / motor.Definition.RollDuration);
            case PlayerState.Attacking:
                return AttackPose();
            case PlayerState.Guarding:
                return guard != null ? guard : idle;
            case PlayerState.Hurt:
                return hurt != null ? hurt : idle;
            case PlayerState.Dead:
                if (motor.Grounded && motor.States.Elapsed >= collapseDuration && deathDown != null)
                    return deathDown;
                return deathFall != null ? deathFall : idle;
            case PlayerState.Free:
                if (airJump) return AirJumpPose();
                if (!motor.Grounded) return JumpPose();
                if (Time.time < landUntil && jumpLand != null) return jumpLand;
                if (running) return Frame(run, runPhase);
                return idling ? Frame(idleLoop, idlePhase) : idle;
            default:
                return idle;
        }
    }

    Sprite AirJumpPose() {
        // Share the actual roll silhouettes and their native scale. This only changes the sprite.
        if (airJumpElapsed < airCurlDuration)
            return roll[airJumpElapsed < airCurlDuration * .45f ? 1 : 2];
        if (airJumpElapsed < airCurlDuration + airUncurlDuration) return roll[3];
        return JumpPose();
    }

    Sprite JumpPose() {
        var pose = body.linearVelocity.y > .12f ? jumpRise : jumpFall;
        return pose != null ? pose : idle;
    }

    void UpdateSplash() {
        if (landingSplash == null) return;
        float progress = 1 - (splashUntil - Time.time) / .16f;
        landingSplash.enabled = progress < 1;
        if (!landingSplash.enabled) return;
        landingSplash.transform.position = splashPoint;
        landingSplash.transform.localScale = new Vector3(Mathf.Lerp(.7f, 1.25f, progress), 1, 1);
        landingSplash.color = new Color(1, 1, 1, Mathf.Clamp01(1 - progress));
    }

    Sprite AttackPose() {
        var weapon = combat.Weapon;
        if (weapon.Stage == AttackStage.Idle) return idle;
        if (weapon.Stage == AttackStage.Recovery) {
            int claw = ClawIndex(weapon.Definition);
            return claw >= 0 && clawRecoveries != null && claw < clawRecoveries.Length
                ? clawRecoveries[claw] : idle;
        }
        bool active = weapon.Stage == AttackStage.Active;
        if (weapon.Definition == motor.Definition.HissAttack)
            return active && hiss != null && hiss.HissPose != null ? hiss.HissPose : idle;
        if (weapon.Definition == motor.Definition.FinisherAttack) {
            var pose = active ? finisherStrike : finisherWindup;
            return pose != null ? pose : idle;
        }
        if (weapon.Definition == motor.Definition.ComboAttack) return active ? comboStrike : comboWindup;
        if (weapon.Definition == motor.Definition.CounterAttack) return active ? counterStrike : counterWindup;
        return active ? primaryStrike : primaryWindup != null ? primaryWindup : idle;
    }

    int ClawIndex(AttackDefinition attack) {
        if (attack == null) return -1;
        if (attack == motor.Definition.PrimaryAttack) return 0;
        if (attack == motor.Definition.ComboAttack) return 1;
        return attack == motor.Definition.FinisherAttack ? 2 : -1;
    }

    void UpdateClawTrails() {
        if (clawTrails == null) return;
        var weapon = combat.Weapon;
        var attack = weapon.Definition;
        int claw = ClawIndex(attack);
        bool active = motor.States.Current == PlayerState.Attacking && weapon.Stage == AttackStage.Active
            && claw >= 0 && clawEffects != null && claw < clawEffects.Length && clawEffects[claw] != null;
        clawTrails.enabled = active;
        if (!active) return;
        float progress = Mathf.Clamp01((weapon.Elapsed - attack.Startup) / attack.Active);
        float spread = Mathf.Lerp(.87f, 1.12f, progress);
        var offset = ClawTrailOffsets[claw];
        var size = ClawTrailSizes[claw];
        clawTrails.sprite = clawEffects[claw];
        clawTrails.flipX = weapon.Facing < 0;
        clawTrails.transform.localPosition = visual.transform.localPosition
            + new Vector3(offset.x * weapon.Facing, offset.y, 0);
        Vector2 spriteSize = clawTrails.sprite.bounds.size;
        clawTrails.transform.localScale = new Vector3(size.x / spriteSize.x * spread, size.y / spriteSize.y * spread, 1);
        clawTrails.color = new Color(1, 1, 1, Mathf.Lerp(1, .15f, progress));
    }

    Sprite Frame(Sprite[] frames, float phase) {
        if (frames == null || frames.Length == 0) return idle;
        int index = Mathf.Clamp(Mathf.FloorToInt(phase * frames.Length), 0, frames.Length - 1);
        return frames[index];
    }
}
}
