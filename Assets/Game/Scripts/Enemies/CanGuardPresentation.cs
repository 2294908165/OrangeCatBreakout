using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(CanGuardBrain), typeof(Health), typeof(AudioSource))]
public sealed class CanGuardPresentation : MonoBehaviour {
    [SerializeField] SpriteRenderer visual, warning, impact;
    [SerializeField] Sprite idle, crouch, charge, stagger, leap, slam, defeated;
    [SerializeField] AudioClip hitSound;
    CanGuardBrain boss;
    Health life;
    AudioSource audioSource;
    float hitUntil;

    public void Configure(SpriteRenderer renderer, SpriteRenderer warningStrip, SpriteRenderer spark,
        Sprite standing, Sprite[] poses, AudioClip hit) {
        visual = renderer;
        warning = warningStrip;
        impact = spark;
        idle = standing;
        crouch = poses[0];
        charge = poses[1];
        stagger = poses[2];
        leap = poses[3];
        slam = poses[4];
        defeated = poses[5];
        hitSound = hit;
    }

    void Awake() {
        boss = GetComponent<CanGuardBrain>();
        life = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();
    }
    void OnEnable() { life.Damaged += OnHit; }
    void OnDisable() { life.Damaged -= OnHit; }

    void LateUpdate() {
        visual.flipX = boss.Facing > 0;
        visual.sprite = boss.State switch {
            CanGuardState.ChargeTell or CanGuardState.SlamTell => crouch,
            CanGuardState.Charging => charge,
            CanGuardState.Airborne => leap,
            CanGuardState.Recovering when boss.RecoveredFromSlam && boss.Elapsed < .22f => slam,
            CanGuardState.Recovering => stagger,
            CanGuardState.Dead => defeated,
            _ => idle
        };
        float hit = Mathf.Clamp01((hitUntil - Time.time) / .14f);
        impact.enabled = hit > 0;
        impact.color = new Color(1, .74f, .56f, hit);
        visual.color = hit > 0 ? new Color(1, .60f, .69f) : Color.white;
        UpdateWarning();
    }

    void UpdateWarning() {
        bool lane = boss.State == CanGuardState.ChargeTell;
        warning.enabled = lane || boss.State == CanGuardState.SlamTell || boss.State == CanGuardState.Airborne;
        if (!warning.enabled) return;
        float center = lane ? (transform.position.x + boss.ChargeEndX) * .5f : boss.LandingX;
        float width = lane ? Mathf.Abs(transform.position.x - boss.ChargeEndX) : 1.9f;
        warning.transform.position = new Vector3(center, boss.FloorY + .055f, 0);
        warning.size = new Vector2(width, .14f);
        float pulse = .60f + Mathf.PingPong(Time.time * 3.5f, .40f);
        warning.color = lane ? new Color(1, .78f, .34f, pulse) : new Color(1, .48f, .68f, pulse);
    }

    void OnHit(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (amount <= 0) return;
        impact.transform.position = damage.Point;
        hitUntil = Time.time + .14f;
        if (hitSound != null) audioSource.PlayOneShot(hitSound, .65f);
    }
}
}
