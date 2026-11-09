using UnityEngine;

namespace OrangeCat {
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(AttackHitbox2D), typeof(Health))]
public sealed class TrainingAttackSource : MonoBehaviour, IParryable {
    [SerializeField] AttackDefinition attack;
    [SerializeField] SpriteRenderer warning;
    [SerializeField, Min(.01f)] float period = 2;
    [SerializeField, Min(0)] float staggerDuration = .45f;
    [SerializeField] int facing = -1;
    AttackHitbox2D weapon;
    Health health;
    float untilNextAttack, staggerRemaining;
    public bool IsStaggered => staggerRemaining > 0;
    public AttackStage Stage => weapon.Stage;

    public void Configure(AttackDefinition definition, SpriteRenderer telegraph) {
        attack = definition;
        warning = telegraph;
    }

    void Awake() {
        weapon = GetComponent<AttackHitbox2D>();
        health = GetComponent<Health>();
    }

    void OnEnable() {
        health.Died += weapon.Cancel;
        health.TargetReset += ResetCycle;
    }

    void OnDisable() {
        health.Died -= weapon.Cancel;
        health.TargetReset -= ResetCycle;
        weapon.Cancel();
        if (warning != null) warning.enabled = false;
    }

    void FixedUpdate() {
        Step(Time.fixedDeltaTime);
    }

    public void Step(float dt) {
        if (health.IsDead) return;
        untilNextAttack -= dt;
        staggerRemaining = Mathf.Max(0, staggerRemaining - dt);
        if (IsStaggered) return;
        if (weapon.IsRunning) weapon.Step(dt);
        else if (untilNextAttack <= 0 && weapon.Begin(attack, facing)) untilNextAttack = period;
    }

    void LateUpdate() {
        if (warning == null) return;
        warning.enabled = !health.IsDead && (Stage == AttackStage.Startup || Stage == AttackStage.Active);
        warning.color = Stage == AttackStage.Active ? new Color(1, .12f, .15f, .42f) : new Color(1, .64f, .1f, .26f);
    }

    public void OnParried() {
        weapon.Cancel();
        staggerRemaining = staggerDuration;
    }

    void ResetCycle() {
        weapon.Cancel();
        untilNextAttack = staggerRemaining = 0;
    }
}
}
