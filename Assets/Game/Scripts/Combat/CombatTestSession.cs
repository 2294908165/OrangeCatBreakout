using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OrangeCat {
public sealed class CombatTestSession : MonoBehaviour {
    [SerializeField] PlayerMotor2D haoJu, jiangZhua;
    [SerializeField] Transform spawn;
    [SerializeField] GameManager manager;
    [SerializeField] CameraFollow2D cameraFollow;
    [SerializeField] Health dummy, source;
    PlayerCombat combat;
    Health playerHealth;
    CombatFeedback playerFeedback, dummyFeedback, sourceFeedback;
    TrainingAttackSource device;
    [SerializeField] bool expanded = true;
    bool isJiangZhua;
    public PlayerMotor2D Player { get; private set; }
    public Health Dummy => dummy;
    public Health Source => source;

    public void Configure(PlayerMotor2D first, PlayerMotor2D second, Transform start,
        GameManager gameManager, CameraFollow2D follow, Health target, Health attacker) {
        haoJu = first;
        jiangZhua = second;
        spawn = start;
        manager = gameManager;
        cameraFollow = follow;
        dummy = target;
        source = attacker;
    }

    void Awake() {
        // Scenes saved before the hiss migration may still lack the listener.
        if (!cameraFollow.TryGetComponent<AudioListener>(out _))
            cameraFollow.gameObject.AddComponent<AudioListener>();
        dummyFeedback = dummy.GetComponent<CombatFeedback>();
        sourceFeedback = source.GetComponent<CombatFeedback>();
        device = source.GetComponent<TrainingAttackSource>();
        SwitchCharacter(false);
    }

    public void SwitchCharacter(bool useJiangZhua) {
        if (Player != null) {
            Player.gameObject.SetActive(false);
            Destroy(Player.gameObject);
        }
        isJiangZhua = useJiangZhua;
        Player = Instantiate(useJiangZhua ? jiangZhua : haoJu, spawn.position, spawn.rotation);
        combat = Player.GetComponent<PlayerCombat>();
        playerHealth = Player.GetComponent<Health>();
        playerFeedback = Player.GetComponent<CombatFeedback>();
        manager.Configure(Player.GetComponent<PlayerInputReader>());
        cameraFollow.Configure(Player.transform);
    }

    public void Restart() {
        manager.SetPaused(false);
        SceneManager.LoadScene(gameObject.scene.buildIndex);
    }

    void Update() {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f1Key.wasPressedThisFrame) expanded = !expanded;
        if (keyboard.rKey.wasPressedThisFrame) Restart();
    }

    void OnGUI() {
        if (Player == null) return;
        GUILayout.BeginArea(new Rect(12, 12, 410, 340), GUI.skin.box);
        GUILayout.Label($"{(isJiangZhua ? "JiangZhua" : "HaoJu")}  |  HP {playerHealth.Current:0.##}/{playerHealth.MaxHealth:0.##}  |  {Player.States.Current}");
        GUILayout.Label($"Dummy {dummy.Current:0.##}/{dummy.MaxHealth:0.##}  {dummyFeedback.LastResult}");
        GUILayout.Label($"Device {source.Current:0.##}/{source.MaxHealth:0.##}  {(device.IsStaggered ? "Staggered" : device.Stage.ToString())}  {sourceFeedback.LastResult}");
        GUILayout.Label(playerFeedback.LastResult);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("HaoJu")) SwitchCharacter(false);
        if (GUILayout.Button("JiangZhua")) SwitchCharacter(true);
        if (GUILayout.Button("Reset targets")) { dummy.ResetTarget(); source.ResetTarget(); }
        if (GUILayout.Button("Restart (R)")) Restart();
        GUILayout.EndHorizontal();
        GUILayout.Label("A/D move · Space jump · J attack · K guard · Shift roll");
        if (Player.Definition.HissAttack != null)
            GUILayout.Label(combat.HissCooldownRemaining > 0
                ? $"Q hiss · {combat.HissCooldownRemaining:0.0}s"
                : "Q hiss · Ready");
        GUILayout.Label("Esc pause · F1 debug · R restart");
        if (expanded) {
            var weapon = combat.Weapon;
            GUILayout.Label($"Attack: {weapon.Definition?.AttackName ?? "-"}  |  {weapon.Stage}  #{weapon.InstanceId}  |  Targets {weapon.HitCount}");
            GUILayout.Label($"Grounded {Player.Grounded}  |  Jumps {Player.JumpsUsed}  |  Facing {Player.Facing}");
            GUILayout.Label($"IsInvulnerable {combat.IsInvulnerable}  |  IsGuarding {combat.IsGuarding}");
            GUILayout.Label($"IsPerfectGuardWindow {combat.IsPerfectGuardWindow}");
            GUILayout.Label($"CanCounterAttack {combat.CanCounterAttack}  |  {combat.CounterRemaining:0.00}s");
        }
        GUILayout.EndArea();
    }
}
}
