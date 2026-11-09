using UnityEngine;

namespace OrangeCat {
public enum DamageType { Claw, Counter, Training, Sonic, RatCharge, Hazard, FlyDive, BossCharge, BossShockwave, BellyBump }
public enum DamageOutcome { Ignored, Invulnerable, Hit, Blocked, PerfectGuard }

public readonly struct DamageInfo {
    public readonly GameObject Source;
    public readonly float Amount;
    public readonly Vector2 Point, Direction;
    public readonly float Knockback;
    public readonly DamageType Type;
    public readonly bool Blockable;

    public DamageInfo(GameObject source, float amount, Vector2 point, Vector2 direction,
        float knockback, DamageType type, bool blockable) {
        Source = source;
        Amount = amount;
        Point = point;
        Direction = direction;
        Knockback = knockback;
        Type = type;
        Blockable = blockable;
    }
}

public interface IDamageable {
    DamageOutcome TakeDamage(DamageInfo damage);
}

public interface IParryable {
    void OnParried();
}
}
