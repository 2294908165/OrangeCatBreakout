using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WarehouseExit : MonoBehaviour {
    [SerializeField] Health rat;
    [SerializeField] BoxCollider2D gate;
    [SerializeField] Transform gateVisual;
    [SerializeField] SpriteRenderer canVisual;
    [SerializeField] float lift = 4.8f;
    Health player;
    Vector3 closedPosition;
    public bool IsOpen { get; private set; }
    public bool Collected { get; private set; }

    public void Configure(Health enemy, BoxCollider2D blocker, Transform door, SpriteRenderer can) {
        rat = enemy;
        gate = blocker;
        gateVisual = door;
        canVisual = can;
    }

    public void Track(Health activePlayer) { player = activePlayer; }
    void Awake() { closedPosition = gateVisual.localPosition; }
    void OnEnable() { rat.Died += Open; }
    void OnDisable() { rat.Died -= Open; }

    void Open() {
        IsOpen = true;
        gate.enabled = false;
    }

    void Update() {
        if (IsOpen)
            gateVisual.localPosition = Vector3.MoveTowards(gateVisual.localPosition,
                closedPosition + Vector3.up * lift, 8 * Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D other) {
        if (!IsOpen || Collected || player == null || player.IsDead) return;
        if (other.attachedRigidbody == null || other.attachedRigidbody.gameObject != player.gameObject) return;
        Collected = true;
        canVisual.enabled = false;
        player.GetComponent<PlayerInputReader>().enabled = false;
    }
}
}
