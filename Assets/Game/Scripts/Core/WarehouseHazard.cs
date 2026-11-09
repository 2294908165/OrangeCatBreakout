using UnityEngine;

namespace OrangeCat {
public enum WarehouseHazardKind { SteamVent, Spikes, Fan }
public enum WarehouseHazardPhase { Safe, Warning, Active }

public sealed class WarehouseHazard : MonoBehaviour {
    [SerializeField] WarehouseHazardKind kind;
    [SerializeField] Collider2D contact;
    [SerializeField] SpriteRenderer effect, warning;
    [SerializeField, Min(0)] float damage = 1, knockback = 3f;
    [SerializeField, Min(.01f)] float contactInterval = .8f;
    [SerializeField, Min(.01f)] float idleTime = 1.25f, warningTime = .55f, steamTime = .68f;
    [SerializeField, Min(0)] float phaseOffset;
    [SerializeField] float fanDegreesPerSecond = 300f;
    Health lastVictim;
    float retryAt, createdAt;

    public WarehouseHazardKind Kind => kind;
    public WarehouseHazardPhase Phase { get; private set; }
    public bool IsDangerous => Phase == WarehouseHazardPhase.Active;

    public void Configure(WarehouseHazardKind type, Collider2D trigger,
        SpriteRenderer animated = null, SpriteRenderer signal = null, float cycleOffset = 0) {
        kind = type;
        contact = trigger;
        effect = animated;
        warning = signal;
        phaseOffset = cycleOffset;
    }

    void Awake() {
        createdAt = Time.fixedTime - phaseOffset;
        SetPhase(kind == WarehouseHazardKind.SteamVent ? WarehouseHazardPhase.Safe : WarehouseHazardPhase.Active);
    }

    void FixedUpdate() {
        if (kind != WarehouseHazardKind.SteamVent) return;
        float elapsed = Mathf.Repeat(Time.fixedTime - createdAt, idleTime + warningTime + steamTime);
        var phase = elapsed < idleTime ? WarehouseHazardPhase.Safe
            : elapsed < idleTime + warningTime ? WarehouseHazardPhase.Warning : WarehouseHazardPhase.Active;
        if (phase != Phase) SetPhase(phase);
    }

    void Update() {
        if (kind == WarehouseHazardKind.Fan)
            effect.transform.Rotate(0, 0, -fanDegreesPerSecond * Time.deltaTime);
        else if (Phase == WarehouseHazardPhase.Warning && warning != null)
            warning.color = new Color(1, 1, 1, .55f + Mathf.PingPong(Time.time * 4, .45f));
    }

    void SetPhase(WarehouseHazardPhase phase) {
        Phase = phase;
        contact.enabled = IsDangerous;
        if (kind == WarehouseHazardKind.SteamVent) {
            effect.enabled = IsDangerous;
            warning.enabled = phase == WarehouseHazardPhase.Warning;
        }
    }

    void OnTriggerStay2D(Collider2D other) {
        if (!IsDangerous || other.attachedRigidbody == null ||
            !other.attachedRigidbody.TryGetComponent<PlayerMotor2D>(out var player) ||
            other.gameObject.layer != player.gameObject.layer) return;
        var health = player.GetComponent<Health>();
        if (health == null || health.IsDead || (lastVictim == health && Time.fixedTime < retryAt)) return;

        float delta = player.transform.position.x - transform.position.x;
        float away = Mathf.Abs(delta) < .05f ? -player.Facing : Mathf.Sign(delta);
        var damageInfo = new DamageInfo(gameObject, damage, other.ClosestPoint(transform.position),
            new Vector2(away, 0), knockback, DamageType.Hazard, false);
        if (health.TakeDamage(damageInfo) != DamageOutcome.Hit) return;
        lastVictim = health;
        retryAt = Time.fixedTime + contactInterval;
    }
}
}
