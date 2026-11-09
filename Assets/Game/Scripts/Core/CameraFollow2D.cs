using UnityEngine;
namespace OrangeCat {
public sealed class CameraFollow2D : MonoBehaviour {
    [SerializeField] Transform target;
    [SerializeField] float smoothing = 8;
    [SerializeField] bool limitHorizontal;
    [SerializeField] Vector2 horizontalBounds;
    Camera view;
    public void Configure(Transform value) { target = value; }
    public void SetHorizontalBounds(float left, float right) {
        limitHorizontal = true;
        horizontalBounds = new Vector2(left, right);
    }
    void Awake() { view = GetComponent<Camera>(); }
    void LateUpdate() {
        if (target == null) return;
        float x = target.position.x;
        if (limitHorizontal && view != null) {
            float halfWidth = view.orthographicSize * view.aspect;
            float left = horizontalBounds.x + halfWidth, right = horizontalBounds.y - halfWidth;
            x = left <= right ? Mathf.Clamp(x, left, right) : (horizontalBounds.x + horizontalBounds.y) * .5f;
        }
        Vector3 desired = new Vector3(x, Mathf.Max(1, target.position.y), -10);
        transform.position = Vector3.Lerp(transform.position, desired, 1-Mathf.Exp(-smoothing*Time.deltaTime));
    }
}
}
