using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrangeCat {
public enum AttackStage { Idle, Startup, Active, Recovery }

public sealed class AttackHitbox2D : MonoBehaviour {
    [SerializeField] LayerMask targetLayers;
    [SerializeField] AttackDefinition preview;
    [SerializeField] int previewFacing = 1;
    readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    readonly HashSet<Health> hit = new HashSet<Health>();
    static long nextId;
    AttackDefinition attack;
    int facing;

    public AttackDefinition Definition => attack;
    public float Elapsed { get; private set; }
    public long InstanceId { get; private set; }
    public int HitCount => hit.Count;
    public int Facing => facing;
    public bool IsRunning => attack != null;
    public AttackStage Stage => !IsRunning ? AttackStage.Idle
        : Elapsed < attack.Startup ? AttackStage.Startup
        : Elapsed < attack.ActiveEnd ? AttackStage.Active : AttackStage.Recovery;
    public event Action<DamageOutcome> Contact;
    public event Action<AttackDefinition> BecameActive;
    public event Action<bool> Finished;
    bool landed, activeStarted;

    public void Configure(LayerMask layers, AttackDefinition previewAttack, int direction = 1) {
        targetLayers = layers;
        preview = previewAttack;
        previewFacing = direction;
    }

    public bool Begin(AttackDefinition definition, int direction) {
        if (!isActiveAndEnabled || IsRunning || definition == null) return false;
        attack = definition;
        facing = direction < 0 ? -1 : 1;
        InstanceId = ++nextId;
        Elapsed = 0;
        landed = activeStarted = false;
        return true;
    }

    public void Step(float dt) {
        if (!IsRunning || dt <= 0) return;
        var definition = attack;
        long id = InstanceId;
        float previous = Elapsed;
        Elapsed += dt;
        // Query every physics step intersecting the active interval, including a short window
        // crossed entirely in one step. The set is shared by all queries in this instance.
        if (Elapsed >= definition.Startup && previous < definition.ActiveEnd) {
            if (!activeStarted) {
                activeStarted = true;
                BecameActive?.Invoke(definition);
            }
            if (InstanceId == id) Query(definition, id);
        }
        if (InstanceId != id || Elapsed + .00001f < definition.Duration) return;
        bool didLand = landed;
        Cancel();
        Finished?.Invoke(didLand);
    }

    void Query(AttackDefinition definition, long id) {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = true };
        Vector2 center = Center(definition, facing);
        Physics2D.OverlapBox(center, definition.HitboxSize, 0, filter, overlaps);
        for (int i = 0; i < overlaps.Count && InstanceId == id; i++) {
            var collider = overlaps[i];
            if (collider == null || !collider.TryGetComponent<Hurtbox2D>(out var hurtbox)) continue;
            var target = hurtbox.Owner;
            if (target == null || target.gameObject == gameObject || target.IsDead || !hit.Add(target)) continue;
            var damage = new DamageInfo(gameObject, definition.Damage, collider.ClosestPoint(center),
                new Vector2(facing, 0), definition.Knockback, definition.Type, definition.Blockable);
            var outcome = target.TakeDamage(damage);
            // A synchronous parry can cancel this instance during TakeDamage.
            if (InstanceId != id) break;
            landed |= outcome == DamageOutcome.Hit || outcome == DamageOutcome.Blocked;
            Contact?.Invoke(outcome);
        }
        overlaps.Clear();
    }

    Vector2 Center(AttackDefinition definition, int direction) => (Vector2)transform.position +
        new Vector2(definition.HitboxOffset.x * direction, definition.HitboxOffset.y);

    public void Cancel() {
        attack = null;
        InstanceId = 0;
        Elapsed = 0;
        hit.Clear();
    }

    void OnDisable() { Cancel(); }

    void OnDrawGizmos() {
        var definition = attack != null ? attack : preview;
        if (definition == null) return;
        Gizmos.color = Stage == AttackStage.Active ? Color.red : new Color(1, .7f, .1f);
        Gizmos.DrawWireCube(Center(definition, IsRunning ? facing : previewFacing), definition.HitboxSize);
    }
}
}
