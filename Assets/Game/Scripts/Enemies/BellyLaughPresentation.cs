using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(BellyLaughBrain2D), typeof(Health), typeof(AudioSource))]
public sealed class BellyLaughPresentation : MonoBehaviour {
    [SerializeField] SpriteRenderer art, warning, impact;
    [SerializeField] Sprite idle, guard, hurt, laughLow, laughHigh, windup, bump, dead;
    [SerializeField] AudioClip hitSound;
    [SerializeField] AudioSource laughAudio;
    BellyLaughBrain2D monster;
    Health life;
    AudioSource audioSource;
    Vector3 hitPoint;
    float hitUntil;
    const float FlashDuration = .14f;

    public void Configure(SpriteRenderer visual, SpriteRenderer telegraph, SpriteRenderer spark,
        Sprite idlePose, Sprite guardPose, Sprite[] reactions, Sprite[] actions, AudioClip hit, AudioSource laugh) {
        art = visual;
        warning = telegraph;
        impact = spark;
        idle = idlePose;
        guard = guardPose;
        hurt = reactions[1];
        laughLow = reactions[2];
        laughHigh = reactions[3];
        windup = actions[0];
        bump = actions[1];
        dead = actions[2];
        hitSound = hit;
        laughAudio = laugh;
    }

    void Awake() {
        monster = GetComponent<BellyLaughBrain2D>();
        life = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable() {
        life.Damaged += OnHit;
        monster.LaughStarted += PlayLaugh;
    }

    void OnDisable() {
        life.Damaged -= OnHit;
        monster.LaughStarted -= PlayLaugh;
        if (laughAudio != null) laughAudio.Stop();
        impact.enabled = warning.enabled = false;
        hitUntil = 0;
    }

    void OnHit(DamageInfo damage, float amount, DamageOutcome outcome) {
        if (amount <= 0) return;
        if (laughAudio != null) laughAudio.Stop();
        hitPoint = damage.Point;
        hitUntil = Time.time + FlashDuration;
        if (hitSound != null) audioSource.PlayOneShot(hitSound, .6f);
    }

    void PlayLaugh() {
        if (laughAudio != null) laughAudio.Play();
    }

    void LateUpdate() {
        art.sprite = monster.State switch {
            BellyLaughState.Guarding => guard,
            BellyLaughState.Windup => windup,
            BellyLaughState.Bumping => bump,
            BellyLaughState.Hurt => hurt,
            BellyLaughState.Laughing => Mathf.FloorToInt(monster.Elapsed / .11f) % 2 == 0 ? laughLow : laughHigh,
            BellyLaughState.Dead => dead,
            _ => idle
        };
        art.flipX = monster.Facing > 0;
        warning.enabled = monster.State == BellyLaughState.Windup;
        if (warning.enabled) {
            warning.transform.localPosition = new Vector3(monster.Facing * .85f, -.87f, 0);
            warning.color = new Color(1, 1, 1, .55f + Mathf.PingPong(Time.time * 4, .45f));
        }
        float flash = Mathf.Clamp01((hitUntil - Time.time) / FlashDuration);
        art.color = flash > .6f ? new Color(1, .62f, .49f) : Color.white;
        impact.enabled = flash > 0;
        if (!impact.enabled) return;
        impact.transform.position = hitPoint;
        impact.color = new Color(1, .81f, .57f, flash);
    }
}
}
