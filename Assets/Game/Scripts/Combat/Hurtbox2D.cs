using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(BoxCollider2D))]
public sealed class Hurtbox2D : MonoBehaviour {
    [SerializeField] Health owner;
    public Health Owner => owner;
    public void Configure(Health health) { owner = health; }

    void OnDrawGizmos() {
        if (!TryGetComponent<BoxCollider2D>(out var box)) return;
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = previous;
    }
}
}
