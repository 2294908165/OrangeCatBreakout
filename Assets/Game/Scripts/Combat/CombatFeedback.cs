using UnityEngine;

namespace OrangeCat {
public sealed class CombatFeedback : MonoBehaviour {
    [SerializeField] SpriteRenderer visual;
    [SerializeField] bool recoilVisual;
    [SerializeField] SpriteRenderer hitSpark;
    [SerializeField] AudioClip hitSound;
    Health health;
    PlayerCombat player;
    AttackHitbox2D weapon;
    AudioSource hitAudio;
    Color baseColor;
    Vector3 restPosition;
    float flashUntil, recoilUntil;
    float sparkUntil;
    Vector3 hitPoint;
    Color flashColor;
    public string LastResult { get; private set; } = "-";

    public void Configure(SpriteRenderer renderer, bool recoil = false) {
        visual = renderer;
        recoilVisual = recoil;
    }

    public void ConfigurePlayerImpact(SpriteRenderer spark, AudioClip sound) {
        hitSpark = spark;
        hitSound = sound;
    }

    void Awake() {
        health = GetComponent<Health>();
        player = GetComponent<PlayerCombat>();
        weapon = GetComponent<AttackHitbox2D>();
        hitAudio = GetComponent<AudioSource>();
        baseColor = visual.color;
        restPosition = visual.transform.localPosition;
    }

    void OnEnable() {
        health.Damaged += OnDamaged;
        health.TargetReset += OnReset;
        if (player != null) player.PerfectGuarded += OnPerfectGuard;
        if (weapon != null) {
            weapon.Contact += OnContact;
            weapon.Finished += OnAttackFinished;
        }
    }

    void OnDisable() {
        health.Damaged -= OnDamaged;
        health.TargetReset -= OnReset;
        if (player != null) player.PerfectGuarded -= OnPerfectGuard;
        if (weapon != null) {
            weapon.Contact -= OnContact;
            weapon.Finished -= OnAttackFinished;
        }
        sparkUntil = flashUntil = 0;
        if (hitSpark != null) hitSpark.enabled = false;
    }

    void LateUpdate() {
        visual.color = Time.time < flashUntil ? flashColor : health.IsDead ? Color.gray : baseColor;
        if (recoilVisual && Time.time >= recoilUntil) visual.transform.localPosition = restPosition;
        if (hitSpark == null) return;
        float remaining = (sparkUntil - Time.time) / .14f;
        hitSpark.enabled = remaining > 0;
        if (!hitSpark.enabled) return;
        hitSpark.transform.position = hitPoint;
        hitSpark.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, .78f, remaining);
        hitSpark.color = new Color(1, .68f, .55f, Mathf.Clamp01(remaining));
    }

    void OnDamaged(DamageInfo damage, float amount, DamageOutcome outcome) {
        LastResult = $"{(outcome == DamageOutcome.Blocked ? "Blocked" : "Hit")} -{amount:0.##}";
        if (health.IsDead) LastResult += " / DEAD";
        if (player != null && amount > 0) {
            if (outcome == DamageOutcome.Blocked) Flash(new Color(.62f, .92f, 1));
            else {
                Flash(new Color(1, .44f, .39f));
                hitPoint = new Vector3(damage.Point.x, damage.Point.y + .08f, transform.position.z);
                sparkUntil = Time.time + .14f;
                if (hitAudio != null && hitSound != null && Time.timeScale > 0)
                    hitAudio.PlayOneShot(hitSound, .55f);
            }
        } else Flash(Color.white);
        // Fixed training objects stay anchored; only their visual recoils.
        if (!recoilVisual) return;
        visual.transform.localPosition = restPosition + Vector3.right *
            (damage.Direction.x * Mathf.Min(.32f, damage.Knockback * .06f));
        recoilUntil = Time.time + .12f;
    }

    void OnPerfectGuard() {
        LastResult = "PERFECT GUARD";
        Flash(new Color(.4f, 1, 1));
    }

    void OnContact(DamageOutcome outcome) {
        if (outcome == DamageOutcome.Hit || outcome == DamageOutcome.Blocked) LastResult = "Attack landed";
        else if (outcome == DamageOutcome.Invulnerable) LastResult = "Target invulnerable";
    }

    void OnAttackFinished(bool hit) { LastResult = hit ? "Attack landed" : "Attack missed"; }

    void OnReset() {
        LastResult = "-";
        flashUntil = recoilUntil = 0;
    }

    void Flash(Color color) {
        flashColor = color;
        flashUntil = Time.time + .12f;
    }
}
}
