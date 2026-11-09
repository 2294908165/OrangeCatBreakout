using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(ColdFlyBrain2D))]
public sealed class ColdFlyPresentation : MonoBehaviour {
    [SerializeField] SpriteRenderer art, trace;
    [SerializeField] Sprite[] poses;
    ColdFlyBrain2D fly;

    public void Configure(SpriteRenderer renderer, SpriteRenderer telegraph, Sprite[] frames) {
        art = renderer;
        trace = telegraph;
        poses = frames;
    }

    void Awake() { fly = GetComponent<ColdFlyBrain2D>(); }

    void LateUpdate() {
        int frame = fly.State switch {
            ColdFlyState.Windup => 4,
            ColdFlyState.Dive => 5,
            ColdFlyState.Recovery or ColdFlyState.Hurt => 6,
            ColdFlyState.Dead => 7,
            _ => Mathf.FloorToInt(Time.time * 16) % 4
        };
        art.sprite = poses[frame];
        art.flipX = fly.Facing < 0;
        art.color = fly.State == ColdFlyState.Hurt ? new Color(1, .60f, .55f)
            : fly.State == ColdFlyState.Dead ? Color.gray : Color.white;
        trace.enabled = fly.State == ColdFlyState.Windup;
        if (!trace.enabled) return;
        Vector2 path = fly.DiveEndpoint - (Vector2)transform.position;
        trace.transform.localPosition = path * .5f;
        trace.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(path.y, path.x) * Mathf.Rad2Deg);
        trace.transform.localScale = new Vector3(path.magnitude, .14f, 1);
        trace.color = new Color(1, 1, 1, .60f + Mathf.PingPong(fly.Weapon.Elapsed * 4, .4f));
    }
}
}
