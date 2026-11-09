using UnityEngine;

namespace OrangeCat {
public sealed class RooftopEscape : MonoBehaviour {
    [SerializeField] SpriteRenderer[] breakerLamps;
    [SerializeField] BoxCollider2D gate;
    [SerializeField] Transform gateVisual;
    [SerializeField] SpriteRenderer exitLamp;
    [SerializeField, Min(.1f)] float interactionRadius = 1.25f;

    Health player;
    bool[] powered;
    Vector3 closedPosition;
    Camera view;
    GUIStyle markerStyle;
    static readonly Color WaitingColor = new(1, .52f, .29f);
    static readonly Color ReadyColor = new(.49f, 1, .72f);

    public int PoweredCount { get; private set; }
    public int BreakerCount => breakerLamps.Length;
    public bool ExitOpen => PoweredCount == BreakerCount;
    public bool Escaped { get; private set; }
    public bool CanLeave {
        get {
            if (!ExitOpen || player == null || player.IsDead) return false;
            Vector2 offset = player.transform.position - exitLamp.transform.position;
            // The exit zone reaches the right parapet so a player who walks past the lamp can still leave.
            return offset.x >= -interactionRadius && Mathf.Abs(offset.y) <= interactionRadius;
        }
    }

    public void Configure(SpriteRenderer[] breakers, BoxCollider2D blocker, Transform door, SpriteRenderer indicator) {
        breakerLamps = breakers;
        gate = blocker;
        gateVisual = door;
        exitLamp = indicator;
    }

    public void Track(Health activePlayer) => player = activePlayer;

    void Awake() {
        powered = new bool[BreakerCount];
        closedPosition = gateVisual.localPosition;
        view = Camera.main;
        foreach (var lamp in breakerLamps) lamp.color = WaitingColor;
        exitLamp.color = WaitingColor;
    }

    bool InRange(Transform target) => player != null && !player.IsDead &&
        ((Vector2)(player.transform.position - target.position)).sqrMagnitude <= interactionRadius * interactionRadius;

    int NearbyBreaker() {
        for (int i = 0; i < BreakerCount; i++)
            if (!powered[i] && InRange(breakerLamps[i].transform)) return i;
        return -1;
    }

    public string Prompt() {
        if (CanLeave) return "E  /  Use the exit to the boss room";
        if (ExitOpen) return "Exit unlocked  /  Follow the green exit sign";
        int nearby = NearbyBreaker();
        if (nearby >= 0) return $"E  /  Turn on breaker {BreakerLabel(nearby)}";
        for (int i = 0; i < BreakerCount; i++) {
            if (powered[i]) continue;
            string direction = player != null && player.transform.position.x > breakerLamps[i].transform.position.x
                ? "left" : "right";
            return $"Gate locked {PoweredCount}/{BreakerCount}  /  Breaker {BreakerLabel(i)} is {direction}";
        }
        return "Follow the amber breaker signs";
    }

    public void Interact() {
        if (Escaped) return;
        if (CanLeave) {
            Escaped = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            return;
        }

        int breaker = NearbyBreaker();
        if (breaker < 0) return;
        powered[breaker] = true;
        breakerLamps[breaker].color = ReadyColor;
        PoweredCount++;
        if (!ExitOpen) return;
        gate.enabled = false;
        exitLamp.color = ReadyColor;
    }

    void Update() {
        if (ExitOpen)
            gateVisual.localPosition = Vector3.MoveTowards(gateVisual.localPosition,
                closedPosition + Vector3.up * 2.5f, 6 * Time.deltaTime);
    }

    void OnGUI() {
        if (Event.current.type != EventType.Repaint || view == null || player == null || player.IsDead || Escaped) return;
        markerStyle ??= new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        for (int i = 0; i < BreakerCount; i++) {
            string label = $"BREAKER {BreakerLabel(i)}";
            if (powered[i]) label += "  ON";
            else if (InRange(breakerLamps[i].transform)) label += "  (E)";
            DrawMarker(breakerLamps[i].transform.position + Vector3.up * .42f, label, powered[i] ? ReadyColor : WaitingColor);
        }
        if (ExitOpen)
            DrawMarker(exitLamp.transform.position + Vector3.up * .75f, "BOSS EXIT  (E)", ReadyColor);
        else
            DrawMarker(gate.transform.position, $"LOCKED  {PoweredCount}/{BreakerCount}", WaitingColor);
    }

    void DrawMarker(Vector3 position, string label, Color color) {
        Vector3 point = view.WorldToScreenPoint(position);
        const float width = 134;
        if (point.z <= 0 || point.x < width * .5f || point.x > Screen.width - width * .5f) return;
        var area = new Rect(point.x - width * .5f, Screen.height - point.y - 11, width, 22);
        GUI.Box(area, GUIContent.none);
        markerStyle.normal.textColor = color;
        GUI.Label(area, label, markerStyle);
    }

    static char BreakerLabel(int index) => (char)('A' + index);
}
}
