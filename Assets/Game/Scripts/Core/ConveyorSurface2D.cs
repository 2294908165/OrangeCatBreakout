using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ConveyorSurface2D : MonoBehaviour {
    [SerializeField] float speed = 1.6f;
    [SerializeField] SpriteRenderer surface;
    [SerializeField] Sprite[] motion;
    float phase;
    public float Speed => speed;

    public void Configure(float carrySpeed, SpriteRenderer renderer, Sprite[] frames) {
        speed = carrySpeed;
        surface = renderer;
        motion = frames;
    }

    void Update() {
        if (surface == null || motion == null || motion.Length == 0) return;
        phase = Mathf.Repeat(phase + Time.deltaTime * Mathf.Abs(speed) * 2, 1);
        surface.sprite = motion[Mathf.Min(motion.Length - 1, (int)(phase * motion.Length))];
    }
}
}
