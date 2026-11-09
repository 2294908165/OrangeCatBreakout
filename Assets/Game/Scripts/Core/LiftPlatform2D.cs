using UnityEngine;

namespace OrangeCat {
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class LiftPlatform2D : MonoBehaviour {
    [SerializeField] float low, high;
    [SerializeField, Min(.01f)] float speed = 1.7f;
    [SerializeField, Min(0)] float dwell = .55f;
    [SerializeField] bool automatic, startsHigh;
    Rigidbody2D body;
    bool goingHigh;
    float wait;

    public float VerticalSpeed { get; private set; }
    public bool AtBottom => Mathf.Abs(body.position.y - low) < .01f;
    public bool AtTop => Mathf.Abs(body.position.y - high) < .01f;

    public void Configure(float bottom, float top, float travelSpeed, bool cycles, bool startAtTop) {
        low = bottom;
        high = top;
        speed = travelSpeed;
        automatic = cycles;
        startsHigh = startAtTop;
    }

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        goingHigh = startsHigh;
    }

    public void GoTo(bool top) {
        goingHigh = top;
        wait = 0;
    }

    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;
        var position = body.position;
        float destination = goingHigh ? high : low;
        if (Mathf.Abs(position.y - destination) < .0001f && automatic) {
            wait += dt;
            if (wait >= dwell) {
                goingHigh = !goingHigh;
                destination = goingHigh ? high : low;
                wait = 0;
            }
        }
        float next = Mathf.MoveTowards(position.y, destination, speed * dt);
        VerticalSpeed = (next - position.y) / dt;
        body.MovePosition(new Vector2(position.x, next));
    }
}
}
