using System;
using UnityEngine;

namespace OrangeCat {
public sealed class Health : MonoBehaviour, IDamageable {
    [SerializeField, Min(.01f)] float maxHealth = 20;
    public float MaxHealth => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0;
    public event Action<float, float> Changed;
    public event Action<DamageInfo, float, DamageOutcome> Damaged;
    public event Action Died;
    public event Action TargetReset;
    PlayerCombat defense;

    public void Configure(float maximum) { maxHealth = Mathf.Max(.01f, maximum); }

    void Awake() {
        defense = GetComponent<PlayerCombat>();
        if (defense != null) maxHealth = GetComponent<PlayerMotor2D>().Definition.MaxHealth;
        Current = maxHealth;
    }

    public DamageOutcome TakeDamage(DamageInfo damage) {
        if (IsDead || !(damage.Amount > 0) || damage.Source == gameObject) return DamageOutcome.Ignored;
        var outcome = defense != null && defense.isActiveAndEnabled
            ? defense.ResolveDefense(damage)
            : DamageOutcome.Hit;
        if (outcome != DamageOutcome.Hit && outcome != DamageOutcome.Blocked) return outcome;

        float requested = outcome == DamageOutcome.Blocked ? damage.Amount * defense.GuardDamageMultiplier : damage.Amount;
        float before = Current;
        Current = Mathf.Max(0, Current - requested);
        bool died = IsDead;
        if (died) Died?.Invoke();
        Damaged?.Invoke(damage, before - Current, outcome);
        Changed?.Invoke(Current, MaxHealth);
        return outcome;
    }

    public void Heal(float amount) {
        if (IsDead || !(amount > 0)) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, MaxHealth);
    }

    // Training targets may be reused. Players respawn as fresh instances.
    public void ResetTarget() {
        if (defense != null) return;
        Current = maxHealth;
        TargetReset?.Invoke();
        Changed?.Invoke(Current, MaxHealth);
    }
}
}
