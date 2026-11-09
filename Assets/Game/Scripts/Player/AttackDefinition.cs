using UnityEngine;

namespace OrangeCat {
[CreateAssetMenu(menuName = "Orange Cat/Attack Definition")]
public sealed class AttackDefinition : ScriptableObject {
    [SerializeField] string attackName = "Claw";
    [SerializeField, Min(0)] float startup = .12f, recovery = .20f;
    [SerializeField, Min(.001f)] float active = .10f;
    [SerializeField, Min(0)] int damage = 1;
    [SerializeField] Vector2 hitboxSize = new Vector2(1, .6f), hitboxOffset = new Vector2(.8f, 0);
    [SerializeField, Min(0)] float knockback = 3;
    [SerializeField] DamageType damageType;
    [SerializeField] bool blockable = true, allowRollCancel;
    [SerializeField, Min(0)] float rollCancelStart = .25f, rollCancelEnd = .34f;

    public string AttackName => attackName;
    public float Startup => startup;
    public float Active => Mathf.Max(.001f, active);
    public float Recovery => recovery;
    public float ActiveEnd => Startup + Active;
    public float Duration => ActiveEnd + Recovery;
    public int Damage => damage;
    public Vector2 HitboxSize => hitboxSize;
    public Vector2 HitboxOffset => hitboxOffset;
    public float Knockback => knockback;
    public DamageType Type => damageType;
    public bool Blockable => blockable;
    public bool CanRollCancel(float time) => allowRollCancel &&
        time >= Mathf.Max(ActiveEnd, rollCancelStart) && time < Mathf.Min(Duration, rollCancelEnd);
}
}
