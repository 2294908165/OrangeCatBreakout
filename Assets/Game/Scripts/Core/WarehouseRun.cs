using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OrangeCat {
public sealed class WarehouseRun : MonoBehaviour {
    const string ColdStorageScene = "ColdStorage02";
    const string LoadingBayScene = "LoadingBay03";
    const string RooftopScene = "Rooftop04";
    const string BossScene = "CanGuard05";
    const string FirstScene = "Warehouse01";
    const float CargoExitDelay = .8f;
    [SerializeField] PlayerMotor2D haoJu, jiangZhua;
    [SerializeField] Transform spawn;
    [SerializeField] RatBrain2D rat;
    [SerializeField] ColdFlyBrain2D fly;
    [SerializeField] BellyLaughBrain2D bellyLaugh;
    [SerializeField] WarehouseExit goal;
    [SerializeField] CargoLiftEscape cargo;
    [SerializeField] RooftopEscape rooftop;
    [SerializeField] CanGuardBrain boss;
    [SerializeField] CameraFollow2D follow;
    [SerializeField] GameManager manager;
    Health life;
    PlayerCombat combat;
    InputAction interact;
    bool isJiangZhua;
    bool loading;
    float cargoExitElapsed;
    static string nextStage;
    static bool nextStageJiang;

    public PlayerMotor2D Player { get; private set; }
    public RatBrain2D Rat => rat;
    public ColdFlyBrain2D Fly => fly;
    public BellyLaughBrain2D BellyLaugh => bellyLaugh;
    public bool IsColdStorage => fly != null && cargo == null;
    public bool IsLoadingBay => cargo != null;
    public bool IsRooftop => rooftop != null;
    public bool IsBoss => boss != null;
    public WarehouseExit Goal => goal;
    public CargoLiftEscape Cargo => cargo;
    public RooftopEscape Rooftop => rooftop;
    public CanGuardBrain Boss => boss;
    public bool Cleared => IsRooftop ? rooftop.Escaped : IsLoadingBay ? cargo.Escaped : goal.Collected;
    string NextScene => IsBoss ? FirstScene : IsRooftop ? BossScene : IsLoadingBay ? RooftopScene
        : IsColdStorage ? LoadingBayScene : ColdStorageScene;
    bool EnemyEngaged => (rat != null && rat.Engaged) || (fly != null && fly.Engaged) ||
        (bellyLaugh != null && bellyLaugh.Engaged) || (boss != null && boss.Engaged);
    Health EnemyLife => IsColdStorage ? fly.Life : rat.Life;
    bool CanSelectCharacter => !EnemyEngaged && !Cleared
        && (!IsLoadingBay || cargo.Phase == CargoLiftPhase.Waiting)
        && (!IsRooftop || rooftop.PoweredCount == 0);

    public void Configure(PlayerMotor2D first, PlayerMotor2D second, Transform start, RatBrain2D enemy,
        WarehouseExit exit, CameraFollow2D cameraFollow, GameManager gameManager) {
        haoJu = first;
        jiangZhua = second;
        spawn = start;
        rat = enemy;
        goal = exit;
        follow = cameraFollow;
        manager = gameManager;
    }

    public void ConfigureColdStorage(PlayerMotor2D first, PlayerMotor2D second, Transform start, ColdFlyBrain2D enemy,
        WarehouseExit exit, CameraFollow2D cameraFollow, GameManager gameManager) {
        Configure(first, second, start, null, exit, cameraFollow, gameManager);
        fly = enemy;
    }

    public void ConfigureLoadingBay(PlayerMotor2D first, PlayerMotor2D second, Transform start, BellyLaughBrain2D enemy,
        CargoLiftEscape exit, CameraFollow2D cameraFollow, GameManager gameManager) {
        Configure(first, second, start, null, null, cameraFollow, gameManager);
        ConfigureLoadingBayEnemy(enemy);
        cargo = exit;
    }

    public void ConfigureLoadingBayEnemy(BellyLaughBrain2D enemy) {
        rat = null;
        fly = null;
        bellyLaugh = enemy;
    }

    public void ConfigureRooftop(PlayerMotor2D first, PlayerMotor2D second, Transform start, RooftopEscape exit,
        CameraFollow2D cameraFollow, GameManager gameManager) {
        Configure(first, second, start, null, null, cameraFollow, gameManager);
        rooftop = exit;
    }

    public void ConfigureBoss(PlayerMotor2D first, PlayerMotor2D second, Transform start, CanGuardBrain guard,
        WarehouseExit exit, CameraFollow2D cameraFollow, GameManager gameManager) {
        Configure(first, second, start, null, exit, cameraFollow, gameManager);
        boss = guard;
    }

    void Awake() {
        bool carryJiang = nextStage == gameObject.scene.name && nextStageJiang;
        nextStage = null;
        SelectCharacter(carryJiang);
    }

    public void SelectCharacter(bool jiang) {
        if (Player != null && (!CanSelectCharacter || isJiangZhua == jiang)) return;
        if (Player != null) {
            Player.gameObject.SetActive(false);
            Destroy(Player.gameObject);
        }
        isJiangZhua = jiang;
        Player = Instantiate(jiang ? jiangZhua : haoJu, spawn.position, spawn.rotation);
        life = Player.GetComponent<Health>();
        combat = Player.GetComponent<PlayerCombat>();
        if (fly != null) fly.Track(life);
        if (rat != null) rat.Track(life);
        if (bellyLaugh != null) bellyLaugh.Track(life);
        if (goal != null) goal.Track(life);
        if (cargo != null) cargo.Track(Player);
        if (rooftop != null) rooftop.Track(life);
        if (boss != null) boss.Track(life);
        follow.Configure(Player.transform);
        var reader = Player.GetComponent<PlayerInputReader>();
        manager.Configure(reader);
        interact = reader.Actions.FindAction("Gameplay/Interact", true);
    }

    void Update() {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
        else if (Cleared) {
            if (IsLoadingBay) cargoExitElapsed += Time.deltaTime;
            if ((keyboard != null && keyboard.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) ||
                (IsLoadingBay && cargoExitElapsed >= CargoExitDelay)) ContinueStage();
        } else if (!manager.IsPaused && interact.WasPressedThisFrame()) {
            if (IsLoadingBay) cargo.TryCall();
            if (IsRooftop) rooftop.Interact();
        }
    }

    void Restart() => LoadStage(gameObject.scene.name);
    void ContinueStage() => LoadStage(NextScene);

    void LoadStage(string scene) {
        if (loading) return;
        loading = true;
        nextStage = scene;
        nextStageJiang = isJiangZhua;
        manager.SetPaused(false);
        SceneManager.LoadScene(scene);
    }

    void OnGUI() {
        if (life == null) return;
        GUILayout.BeginArea(new Rect(12, 12, 322, 192), GUI.skin.box);
        GUILayout.Label(IsBoss ? "05  /  CAN GUARD" : IsRooftop ? "04  /  ROOFTOP MAINTENANCE" : IsLoadingBay ? "03  /  LOADING BAY"
            : IsColdStorage ? "02  /  COLD STORAGE" : "01  /  RAIN WAREHOUSE");
        GUILayout.Label($"{(isJiangZhua ? "JiangZhua" : "HaoJu")}  |  HP {life.Current:0.#}/{life.MaxHealth:0.#}");
        if (Cleared) GUILayout.Label(IsBoss ? "DEMO COMPLETE  /  The last can is yours!"
            : IsRooftop ? "ROOFTOP CLEAR  /  Enter the machine room"
            : IsLoadingBay ? "LOADING BAY CLEAR  /  Entering the rooftop..."
            : IsColdStorage ? "COLD STORAGE CLEAR  /  Exit reached!" : "WAREHOUSE CLEAR  /  Supply can collected!");
        else if (life.IsDead) GUILayout.Label("OUT OF HP  /  Press R to retry");
        else if (IsBoss) {
            GUILayout.Label($"Can Guard  |  HP {boss.Life.Current:0.#}/{boss.Life.MaxHealth:0.#}");
            GUILayout.Label(boss.State switch {
                CanGuardState.ChargeTell => "Charge incoming  /  Roll or jump",
                CanGuardState.SlamTell or CanGuardState.Airborne => "Ground waves  /  Jump over them",
                CanGuardState.Recovering => "Opening  /  Strike now",
                CanGuardState.Dead => "Guard defeated  /  Take the last can",
                _ => "Approach the guard"
            });
        }
        else if (IsRooftop) GUILayout.Label(rooftop.Prompt());
        else if (IsLoadingBay) {
            GUILayout.Label(cargo.Phase switch {
                CargoLiftPhase.Descending => "Cargo lift descending",
                CargoLiftPhase.Boarding => "Jump onto the lift and stand still",
                CargoLiftPhase.Departing => "Stay on the lift  /  Rooftop exit above",
                _ => cargo.CanCall ? "E  /  Call the cargo lift" : "Cross the lifts  /  Find the call panel"
            });
            if (bellyLaugh != null && bellyLaugh.Engaged && !bellyLaugh.Life.IsDead)
                GUILayout.Label($"Belly Laugh  |  HP {bellyLaugh.Life.Current:0.#}/{bellyLaugh.Life.MaxHealth:0.#}");
        }
        else if (goal.IsOpen) GUILayout.Label(IsColdStorage ? "Door open  /  Leave through the loading bay" : "Door open  /  Collect the supply can");
        else if (!EnemyEngaged) GUILayout.Label(IsColdStorage ? "Ride the belt  /  Follow the arrows" : "Cross the vents, spikes and fan");
        else GUILayout.Label($"{(IsColdStorage ? "Cold fly" : "Stray rat")}  |  {EnemyLife.Current:0.#}/{EnemyLife.MaxHealth:0.#}");
        GUILayout.Label(isJiangZhua ? "A/D move  ·  Space jump  ·  J claw  ·  Shift roll"
            : "A/D move  ·  Space jump  ·  J x3 claws  ·  Shift roll");
        if (isJiangZhua) GUILayout.Label("K guard  ·  J counter after perfect guard");
        else GUILayout.Label(combat.HissCooldownRemaining > 0
            ? $"Q hiss  ·  {combat.HissCooldownRemaining:0.0}s" : "Q hiss  ·  Ready");
        GUILayout.BeginHorizontal();
        if (CanSelectCharacter) {
            if (GUILayout.Button("HaoJu")) SelectCharacter(false);
            if (GUILayout.Button("JiangZhua")) SelectCharacter(true);
        }
        if (Cleared && GUILayout.Button(IsBoss ? "Play again (E)" : "Next stage (E)")) ContinueStage();
        if (GUILayout.Button("Restart (R)")) Restart();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
}
