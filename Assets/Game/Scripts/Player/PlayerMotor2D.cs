using System;
using UnityEngine;
namespace OrangeCat {
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerMotor2D : MonoBehaviour {
    [SerializeField] CharacterDefinition definition;
    [SerializeField] PlayerInputReader input;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] LayerMask groundMask;
    [SerializeField, Min(.01f)] float groundProbe = .06f;
    Rigidbody2D body;
    BoxCollider2D bodyCollider;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    ContactFilter2D groundFilter;
    float coyoteRemaining, bufferRemaining;
    int facing = 1, rollDirection;
    PlayerCombat combat;
    Vector2 pendingKnockback;
    public int Facing => facing;
    public void QueueKnockback(Vector2 velocity) { pendingKnockback = velocity; }
    bool wasGrounded, hasSimulated;
    public PlayerStateMachine States { get; } = new PlayerStateMachine();
    public bool Grounded { get; private set; }
    public float SurfaceVelocity { get; private set; }
    public LiftPlatform2D SupportingLift { get; private set; }
    float previousLiftSpeed;
    public int JumpsUsed { get; private set; }
    public event Action AirJumped;
    public event Action Landed;
    public bool Invincible => States.Current == PlayerState.Rolling && definition.IsRollInvincible(States.Elapsed + .000001f);
    public CharacterDefinition Definition => definition;
    public void Configure(CharacterDefinition data, PlayerInputReader reader, SpriteRenderer renderer, LayerMask mask) {
        definition = data; input = reader; visual = renderer; groundMask = mask;
    }
    void Awake() {
        body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<BoxCollider2D>();
        if (definition == null || input == null || visual == null || groundMask.value == 0) {
            Debug.LogError("PlayerMotor2D requires definition, input, visual and ground mask.", this); enabled = false; return;
        }
        combat = GetComponent<PlayerCombat>();
        groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
        body.freezeRotation = true; body.gravityScale = definition.GravityScale;
    }
    void FixedUpdate() { Simulate(input.Consume(), Time.fixedDeltaTime); }
    // Test seam: exactly the same tick used by FixedUpdate; never call alongside enabled simulation.
    public void Simulate(PlayerCommand command, float dt) {
        if (!enabled || dt <= 0) return;
        bool didAirJump = false;
        Grounded = false;
        float groundDistance = float.PositiveInfinity;
        float groundSpeed = 0;
        LiftPlatform2D lift = null;
        bool centeredOnGround = false;
        int count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundProbe);
        for (int i = 0; i < count; i++) {
            var hit = groundHits[i];
            if (hit.normal.y <= .65f || hit.distance > groundDistance + .001f) continue;
            var bounds = hit.collider.bounds;
            hit.collider.TryGetComponent<LiftPlatform2D>(out var candidateLift);
            float referenceSpeed = candidateLift == SupportingLift ? previousLiftSpeed
                : candidateLift != null ? candidateLift.VerticalSpeed : 0;
            if (body.linearVelocity.y - referenceSpeed > .1f) continue;
            if (candidateLift != null && bodyCollider.bounds.min.y < bounds.max.y - .08f) continue;
            bool centered = body.position.x >= bounds.min.x && body.position.x <= bounds.max.x;
            if (Mathf.Abs(hit.distance - groundDistance) <= .001f && (centeredOnGround || !centered)) continue;
            Grounded = true;
            groundDistance = hit.distance;
            centeredOnGround = centered;
            groundSpeed = hit.collider.TryGetComponent<ConveyorSurface2D>(out var conveyor) ? conveyor.Speed : 0;
            lift = candidateLift;
        }
        bool landed = hasSimulated && Grounded && !wasGrounded;
        if (Grounded) { coyoteRemaining = definition.CoyoteTime; if (!wasGrounded) JumpsUsed = 0; }
        else coyoteRemaining = Mathf.Max(0, coyoteRemaining - dt);
        bufferRemaining = command.JumpPressed ? definition.JumpBuffer : Mathf.Max(0, bufferRemaining - dt);
        States.Tick(dt);
        if (States.Current == PlayerState.Rolling && States.Elapsed + .000001f >= definition.RollDuration) States.TryEnter(PlayerState.Free);
        if (States.Current == PlayerState.Free || States.Current == PlayerState.Guarding)
            UpdateFacing(command.Move);
        bool canCancel = combat != null && combat.CanRollCancel;
        if (command.RollPressed && Grounded &&
            (States.Current == PlayerState.Free || canCancel) &&
            States.TryEnter(PlayerState.Rolling, canCancel)) {
            rollDirection = facing;
            bufferRemaining = 0;
        }
        if (combat != null && combat.isActiveAndEnabled) combat.Step(command, dt);
        var velocity = body.linearVelocity;
        // Apply acceleration relative to the floor, keeping belt speed on the takeoff tick.
        velocity.x -= SurfaceVelocity;
        float liftSpeed = lift != null ? lift.VerticalSpeed : 0;
        if (lift != null) velocity.y = 0;
        if (States.Current == PlayerState.Rolling) velocity.x = rollDirection * definition.RollSpeed;
        else if (States.Current == PlayerState.Free) {
            float move = Mathf.Clamp(command.Move, -1, 1);
            velocity.x = Mathf.MoveTowards(velocity.x, move * definition.MoveSpeed,
                (Mathf.Abs(move) > .01f ? definition.Acceleration : definition.Deceleration) * dt);
            // Leaving a ledge consumes the ground jump after coyote time expires.
            if (!Grounded && coyoteRemaining <= 0 && JumpsUsed == 0) JumpsUsed = 1;
            if (bufferRemaining > 0 && JumpsUsed < definition.MaxJumps) {
                canCutJump = true; velocity.y = definition.JumpSpeed; JumpsUsed++; coyoteRemaining = bufferRemaining = 0; Grounded = false;
                didAirJump = JumpsUsed > 1;
            }
        } else if (States.Current == PlayerState.Guarding) {
            float target = Mathf.Clamp(command.Move, -1, 1) * definition.MoveSpeed * definition.GuardSpeedMultiplier;
            velocity.x = Mathf.MoveTowards(velocity.x, target, definition.Deceleration * dt);
        } else {
            float drag = Grounded ? definition.Deceleration : definition.Deceleration * .20f;
            velocity.x = Mathf.MoveTowards(velocity.x, 0, drag * dt);
        }
        if (States.Current == PlayerState.Hurt && pendingKnockback != Vector2.zero)
            velocity = new Vector2(pendingKnockback.x, Mathf.Max(velocity.y, pendingKnockback.y));
        pendingKnockback = Vector2.zero;
        if (velocity.y <= 0 || States.Current == PlayerState.Hurt || States.Current == PlayerState.Dead)
            canCutJump = false;
        if (canCutJump && !command.JumpHeld) {
            velocity.y *= definition.JumpCut;
            canCutJump = false;
        }
        body.gravityScale = definition.GravityScale * (velocity.y < 0 ? definition.FallGravityMultiplier : 1);
        if (States.Current == PlayerState.Dead) {
            velocity.x = 0;
            groundSpeed = 0;
        } else velocity.x += groundSpeed;
        velocity.y += liftSpeed;
        SurfaceVelocity = Grounded ? groundSpeed : 0;
        SupportingLift = Grounded ? lift : null;
        previousLiftSpeed = Grounded ? liftSpeed : 0;
        body.linearVelocity = velocity;
        wasGrounded = Grounded;
        hasSimulated = true;
        if (didAirJump) AirJumped?.Invoke();
        if (landed && Grounded) Landed?.Invoke();
    }
    void UpdateFacing(float movement) {
        if (Mathf.Abs(movement) <= .01f) return;
        facing = movement > 0 ? 1 : -1;
        visual.flipX = facing < 0;
    }

    void OnDrawGizmos() {
        if (!TryGetComponent<BoxCollider2D>(out var box)) return;
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = previous;
    }

    bool canCutJump;
}
}
