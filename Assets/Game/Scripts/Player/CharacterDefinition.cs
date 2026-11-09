using UnityEngine;
namespace OrangeCat {
[CreateAssetMenu(menuName="Orange Cat/Character Definition")]
public sealed class CharacterDefinition : ScriptableObject {
    [SerializeField] string displayName = "哈橘";
    [SerializeField, Min(0)] float moveSpeed = 5, acceleration = 35, deceleration = 45;
    [SerializeField, Min(0)] float jumpSpeed = 10, gravityScale = 3, fallGravityMultiplier = 1.5f;
    [SerializeField, Range(0,1)] float jumpCut = .45f;
    [SerializeField, Min(1)] int maxJumps = 2;
    [SerializeField, Min(0)] float coyoteTime = .10f, jumpBuffer = .12f;
    [SerializeField, Min(.01f)] float rollDuration = .42f;
    [SerializeField, Min(0)] float rollSpeed = 9, invincibleStart = .10f, invincibleEnd = .30f;
    [SerializeField, Min(1)] float maxHealth = 4;
    [SerializeField] AttackDefinition primaryAttack, comboAttack, counterAttack;
    [SerializeField] AttackDefinition finisherAttack;
    [SerializeField] AttackDefinition hissAttack;
    [SerializeField, Min(0)] float hissCooldown = 1.2f;
    [SerializeField, Min(0)] float comboInputStart = .15f;
    [SerializeField] bool supportsGuard;
    [SerializeField, Range(0,1)] float guardSpeedMultiplier = .25f, guardDamageMultiplier = .30f;
    [SerializeField, Min(0)] float perfectGuardDuration = .15f, counterDuration = .80f;
    [SerializeField, Min(0)] float hurtDuration = .18f, hurtInvulnerability = .55f;
    public float MaxHealth => maxHealth;
    public AttackDefinition PrimaryAttack => primaryAttack;
    public AttackDefinition ComboAttack => comboAttack;
    public AttackDefinition FinisherAttack => finisherAttack;
    public AttackDefinition CounterAttack => counterAttack;
    public AttackDefinition HissAttack => hissAttack;
    public float HissCooldown => hissCooldown;
    public float ComboInputStart => comboInputStart;
    public bool SupportsGuard => supportsGuard;
    public float GuardSpeedMultiplier => guardSpeedMultiplier;
    public float GuardDamageMultiplier => guardDamageMultiplier;
    public float PerfectGuardDuration => perfectGuardDuration;
    public float CounterDuration => counterDuration;
    public float HurtDuration => hurtDuration;
    public float HurtInvulnerability => hurtInvulnerability;
    public string DisplayName => displayName;
    public float MoveSpeed => moveSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float JumpSpeed => jumpSpeed;
    public float GravityScale => gravityScale;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float JumpCut => jumpCut;
    public int MaxJumps => maxJumps;
    public float CoyoteTime => coyoteTime;
    public float JumpBuffer => jumpBuffer;
    public float RollDuration => rollDuration;
    public float RollSpeed => rollSpeed;
    public bool IsRollInvincible(float elapsed) => elapsed >= invincibleStart && elapsed < invincibleEnd && elapsed < rollDuration;
    void OnValidate() {
        invincibleStart = Mathf.Clamp(invincibleStart, 0, rollDuration);
        invincibleEnd = Mathf.Clamp(invincibleEnd, invincibleStart, rollDuration);
    }
}
}
