using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OrangeCat.Tests {
public sealed class LoadingBayPlayModeTests {
    [UnityTest]
    public IEnumerator MovingLiftCarriesTheCatAndLetsItJumpOff() {
        yield return SceneManager.LoadSceneAsync("LoadingBay03");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var lift = GameObject.Find("Practice lift").GetComponent<LiftPlatform2D>();
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        body.position = (Vector2)lift.transform.position + Vector2.up * .74f;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => player.SupportingLift == lift, 20, "Stand on the practice deck");
        float y = body.position.y;
        yield return Until(() => body.position.y >= y + .28f && player.SupportingLift == lift,
            75, "Rise with the moving deck");
        Assert.That(player.Grounded, Is.True);
        Assert.That(lift.VerticalSpeed, Is.GreaterThan(0));
        player.Simulate(new PlayerCommand(0, true, true, false), .02f);
        Assert.That(player.SupportingLift, Is.Null);
        Assert.That(player.Grounded, Is.False);
        Assert.That(body.linearVelocityY, Is.GreaterThan(player.Definition.JumpSpeed));
    }

    [UnityTest]
    public IEnumerator CargoLiftWaitsForAPassengerWhoJumpsJustBeforeArrival() {
        yield return SceneManager.LoadSceneAsync("LoadingBay03");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var cargo = stage.Cargo;
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(cargo.transform.position.x, .12f);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        cargo.TryCall();
        yield return Until(() => cargo.Phase == CargoLiftPhase.Boarding, 95, "Wait for the called lift");

        body.position = (Vector2)cargo.Platform.transform.position + Vector2.up * .74f;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => cargo.Phase == CargoLiftPhase.Departing, 50, "Depart with the passenger");
        yield return Until(() => cargo.Platform.transform.position.y >= 2.75f, 85, "Approach the upper stop");
        Assert.That(player.SupportingLift, Is.SameAs(cargo.Platform));
        player.Simulate(new PlayerCommand(0, true, true, false), Time.fixedDeltaTime);
        yield return Until(() => cargo.Platform.AtTop, 14, "Reach the upper stop during the jump");
        yield return new WaitForFixedUpdate();
        Assert.That(cargo.Phase, Is.EqualTo(CargoLiftPhase.Departing));
        Assert.That(player.Grounded, Is.False);
        yield return Until(() => cargo.Escaped, 55, "Finish after landing on the same deck");
    }

    [UnityTest]
    public IEnumerator CampaignReachesRooftopAutomaticallyAfterRidingTheCargoLift() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            var warehouse = Object.FindFirstObjectByType<WarehouseRun>();
            yield return ClearCombatExit(warehouse, warehouse.Rat.Life, keyboard, "ColdStorage02");
            var coldStorage = Object.FindFirstObjectByType<WarehouseRun>();
            yield return ClearCombatExit(coldStorage, coldStorage.Fly.Life, keyboard, "LoadingBay03");
            var stage = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(stage.IsLoadingBay, Is.True);
            Assert.That(Object.FindFirstObjectByType<RatBrain2D>(), Is.Null);
            Assert.That(Object.FindFirstObjectByType<ColdFlyBrain2D>(), Is.Null);
            Assert.That(Object.FindObjectsByType<BellyLaughBrain2D>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var player = stage.Player;
            var body = player.GetComponent<Rigidbody2D>();
            var cargo = stage.Cargo;
            var life = player.GetComponent<Health>();
            Keys(keyboard);
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.E);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(cargo.Phase, Is.EqualTo(CargoLiftPhase.Waiting), "The entrance cannot call the distant lift.");

            Keys(keyboard, Key.D);
            yield return Until(() => body.position.x >= -13.24f, 80, "Run to the spike bed approach");
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= -10.90f, 45, "Jump over the left side of the bed");
            Keys(keyboard, Key.D);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= -6.40f, 48, "Double jump past the rest of the spikes");
            Assert.That(life.Current, Is.EqualTo(life.MaxHealth));

            Keys(keyboard, Key.D);
            yield return Until(() => body.position.x >= 7.50f, 125, "Run past the Belly Laugh monster");
            Keys(keyboard, Key.E);
            yield return Until(() => cargo.Phase == CargoLiftPhase.Descending, 15, "Call the lift at the panel");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LoadingBay03"));
            Keys(keyboard);
            yield return Until(() => cargo.Phase == CargoLiftPhase.Boarding, 95, "Wait for the lift to reach the floor");
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= 9.35f, 42, "Jump onto the cargo deck");
            Keys(keyboard);
            yield return Until(() => cargo.Phase == CargoLiftPhase.Departing, 85, "Stay on the deck until departure");
            yield return Until(() => cargo.Escaped, 95, "Ride to the upper exit");
            Assert.That(cargo.Platform.AtTop, Is.True);
            Assert.That(player.SupportingLift, Is.SameAs(cargo.Platform));
            Assert.That(life.IsDead, Is.False);
            Assert.That(stage.BellyLaugh.Life.Current, Is.EqualTo(stage.BellyLaugh.Life.MaxHealth));
            yield return Until(() => SceneManager.GetActiveScene().name == "Rooftop04", 125,
                "Continue from the cleared lift without pressing E again");
            var roof = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(roof.IsRooftop, Is.True);
            Assert.That(roof.Player.Definition.name, Is.EqualTo("HaoJu"));
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    [UnityTest]
    public IEnumerator BellyLaughRaisesItsPalmOnlyAfterSeeingThePlayer() {
        yield return SceneManager.LoadSceneAsync("LoadingBay03");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var monster = stage.BellyLaugh;
        var visual = monster.transform.Find("Belly Laugh visual").GetComponent<SpriteRenderer>();
        float x = monster.transform.position.x;
        PlacePlayer(stage.Player, x + 3.2f);
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(monster.State, Is.EqualTo(BellyLaughState.Waiting));
        Assert.That(visual.sprite.name, Is.EqualTo("IdleHandsOnBelly"));

        var blocker = new GameObject("Test sight blocker") { layer = LayerMask.NameToLayer("Ground") };
        blocker.transform.position = new Vector3(x - 1.6f, .75f, 0);
        blocker.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 2f);
        PlacePlayer(stage.Player, x - 3.2f);
        try {
            yield return new WaitForFixedUpdate();
            Assert.That(monster.State, Is.EqualTo(BellyLaughState.Waiting));
        } finally { Object.Destroy(blocker); }
        yield return Until(() => monster.State == BellyLaughState.Guarding, 20, "Raise the guard in clear sight");
        yield return null;
        Assert.That(visual.sprite.name, Is.EqualTo("Guard"));
        Assert.That(monster.GetComponent<AttackHitbox2D>().IsRunning, Is.False);

        PlacePlayer(stage.Player, x + 3.2f);
        yield return Until(() => monster.Facing == 1, 12, "Turn to follow a visible player");
        yield return null;
        Assert.That(visual.flipX, Is.True);
        PlacePlayer(stage.Player, x + 6.6f);
        yield return Until(() => monster.State == BellyLaughState.Waiting, 12, "Relax when the player leaves sight");
        yield return null;
        Assert.That(visual.sprite.name, Is.EqualTo("IdleHandsOnBelly"));
    }

    [UnityTest]
    public IEnumerator BellyBumpTelegraphsAndDealsOneDamage() {
        yield return SceneManager.LoadSceneAsync("LoadingBay03");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var monster = stage.BellyLaugh;
        PlacePlayer(stage.Player, monster.transform.position.x - 1.25f);
        var life = stage.Player.GetComponent<Health>();
        yield return Until(() => monster.State == BellyLaughState.Windup, 18, "Wind up before the belly bump");
        yield return null;
        Assert.That(monster.transform.Find("Belly bump warning").GetComponent<SpriteRenderer>().enabled, Is.True);
        Assert.That(life.Current, Is.EqualTo(life.MaxHealth));
        yield return Until(() => monster.State == BellyLaughState.Recovery, 45, "Recover from a completed belly bump");
        Assert.That(life.Current, Is.EqualTo(life.MaxHealth - 1));
    }

    [UnityTest]
    public IEnumerator BellyLaughReactsToClawsAndCancelsItsAttack() {
        yield return SceneManager.LoadSceneAsync("LoadingBay03");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var monster = stage.BellyLaugh;
        var player = stage.Player;
        var visual = monster.transform.Find("Belly Laugh visual").GetComponent<SpriteRenderer>();
        var spark = monster.transform.Find("Belly hit spark").GetComponent<SpriteRenderer>();
        PlacePlayer(player, monster.transform.position.x - 1.25f);
        yield return Until(() => monster.State == BellyLaughState.Windup, 18, "Start a windup that can be interrupted");
        player.Simulate(new PlayerCommand(1, false, false, false, true), Time.fixedDeltaTime);
        yield return Until(() => monster.State == BellyLaughState.Hurt, 18, "Player claw hits the monster hurtbox");
        yield return null;
        Assert.That(visual.sprite.name, Is.EqualTo("HitRecoil"));
        Assert.That(spark.enabled, Is.True);
        Assert.That(monster.GetComponent<AttackHitbox2D>().IsRunning, Is.False);
        Assert.That(monster.Life.Current, Is.EqualTo(monster.Life.MaxHealth - 2));
        yield return Until(() => visual.sprite.name == "LaughLow", 25, "Bend forward and clutch the belly");
        yield return Until(() => visual.sprite.name == "LaughHigh", 25, "Rebound while still laughing");
        Assert.That(monster.State, Is.EqualTo(BellyLaughState.Laughing));

        monster.Life.TakeDamage(new DamageInfo(player.gameObject, monster.Life.Current, monster.transform.position,
            Vector2.right, 0, DamageType.Claw, true));
        yield return null;
        Assert.That(monster.State, Is.EqualTo(BellyLaughState.Dead));
        Assert.That(visual.sprite.name, Is.EqualTo("Defeated"));
    }

    static void PlacePlayer(PlayerMotor2D player, float x) {
        var body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, .12f);
        body.transform.position = body.position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    static IEnumerator ClearCombatExit(WarehouseRun stage, Health enemy, Keyboard keyboard, string next) {
        // Fast-forward the earlier fights while exercising their actual exit triggers and scene loads.
        Keys(keyboard);
        enemy.TakeDamage(new DamageInfo(stage.Player.gameObject, enemy.Current, enemy.transform.position,
            Vector2.right, 0, DamageType.Claw, true));
        var body = stage.Player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(stage.Goal.transform.position.x, .12f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => stage.Goal.Collected, 30, "Collect the exit objective");
        Keys(keyboard, Key.E);
        yield return Until(() => SceneManager.GetActiveScene().name == next, 75, "Continue to " + next);
    }

    static IEnumerator Until(Func<bool> condition, int steps, string label) {
        for (int i = 0; i < steps && !condition(); i++) yield return new WaitForFixedUpdate();
        Assert.That(condition(), Is.True, label);
    }

    static void Keys(Keyboard keyboard, params Key[] keys) =>
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
}
}
