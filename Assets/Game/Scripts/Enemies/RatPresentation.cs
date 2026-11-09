using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(RatBrain2D))]
public sealed class RatPresentation : MonoBehaviour {
    [SerializeField] SpriteRenderer art, warning;
    [SerializeField] Sprite[] poses;
    [SerializeField] Sprite hurtPose;
    [SerializeField] SpriteRenderer impact;
    [SerializeField] AudioClip hitSound;
    [SerializeField, Min(.01f)] float impactDuration = .14f;
    RatBrain2D rat;
    Health health;
    AudioSource audioSource;
    Vector3 impactPoint;
    float impactUntil;

    public void Configure(SpriteRenderer visual, SpriteRenderer telegraph, Sprite[] frames) {
        art = visual;
        warning = telegraph;
        poses = frames;
    }

    public void ConfigureHit(Sprite recoil, SpriteRenderer spark, AudioClip clip) {
        hurtPose = recoil;
        impact = spark;
        hitSound = clip;
    }

    void Awake() {
        rat = GetComponent<RatBrain2D>();
        health = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable() { health.Damaged += OnHit; }

    void OnDisable() {
        health.Damaged -= OnHit;
        if (impact != null) impact.enabled = false;
        impactUntil = 0;
    }

    void OnHit(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (amount <= 0 || impact == null) return;
        impactPoint = new Vector3(damage.Point.x, damage.Point.y + .06f, transform.position.z);
        impactUntil = Time.time + impactDuration;
        impact.flipX = damage.Direction.x < 0;
        if (audioSource != null && hitSound != null && Time.timeScale > 0)
            audioSource.PlayOneShot(hitSound, .55f);
    }

    void LateUpdate() {
        int frame = rat.State switch {
            RatState.Approach => 1 + Mathf.FloorToInt(Time.time * 11) % 3,
            RatState.Windup => 4,
            RatState.Charge => 5,
            RatState.Recovery or RatState.Hurt => 6,
            RatState.Dead => 7,
            _ => 0
        };
        art.sprite = rat.State == RatState.Hurt && hurtPose != null ? hurtPose : poses[frame];
        art.flipX = rat.Facing < 0;
        UpdateImpact();
        warning.enabled = rat.State == RatState.Windup;
        if (!warning.enabled) return;
        warning.transform.localPosition = new Vector3(rat.Facing * rat.ChargeReach * .5f, -.28f, 0);
        warning.transform.localScale = new Vector3(rat.ChargeReach, .14f, 1);
        warning.flipX = rat.Facing < 0;
        warning.color = new Color(1, 1, 1, .55f + Mathf.PingPong(rat.Weapon.Elapsed * 4, .45f));
    }

    void UpdateImpact() {
        bool showing = impact != null && Time.time < impactUntil;
        art.color = rat.State == RatState.Dead ? Color.gray
            : showing && impactUntil - Time.time > impactDuration * .6f ? new Color(1, .65f, .45f) : Color.white;
        if (impact == null) return;
        impact.enabled = showing;
        if (!showing) return;
        float progress = 1 - (impactUntil - Time.time) / impactDuration;
        // The mark stays where the weapon landed while the rat recoils away.
        impact.transform.position = impactPoint;
        impact.transform.localScale = Vector3.one * Mathf.Lerp(.85f, 1.2f, progress);
        impact.color = new Color(1, 1, 1, 1 - progress);
    }
}
}
