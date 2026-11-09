using UnityEngine;

namespace OrangeCat {
[DefaultExecutionOrder(60)]
[RequireComponent(typeof(AttackHitbox2D))]
public sealed class BossShockwave : MonoBehaviour {
    [SerializeField] AttackDefinition attack;
    [SerializeField] SpriteRenderer visual;
    [SerializeField, Min(.1f)] float speed = 9f;
    [SerializeField] Vector2 arena;
    AttackHitbox2D weapon;
    int direction;

    public void Configure(AttackDefinition definition, SpriteRenderer renderer, Vector2 bounds) {
        attack = definition;
        visual = renderer;
        arena = bounds;
    }

    void Awake() { weapon = GetComponent<AttackHitbox2D>(); }

    public void Launch(Vector2 position, int facing) {
        direction = facing;
        transform.position = position;
        visual.flipX = direction < 0;
        gameObject.SetActive(true);
        weapon.Cancel();
        weapon.Begin(attack, direction);
    }

    public void Stop() => gameObject.SetActive(false);

    void FixedUpdate() {
        weapon.Step(Time.fixedDeltaTime);
        float next = transform.position.x + direction * speed * Time.fixedDeltaTime;
        if (!weapon.IsRunning || next < arena.x || next > arena.y) {
            Stop();
            return;
        }
        transform.position += Vector3.right * (next - transform.position.x);
    }
}
}
