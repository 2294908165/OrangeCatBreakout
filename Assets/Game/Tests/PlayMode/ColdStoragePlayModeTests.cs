using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class ColdStoragePlayModeTests {
    [UnityTest]
    public IEnumerator BeltsCarryTheIdleCatStopAtTheSeamAndPreserveTakeoffMomentum() {
        yield return SceneManager.LoadSceneAsync("ColdStorage02");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        Assert.That(stage.IsColdStorage, Is.True);
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        PutPlayer(body, -11.10f);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(player.SurfaceVelocity, Is.EqualTo(1.65f).Within(.01f));
        Assert.That(body.position.x, Is.GreaterThan(-11.05f));
        yield return null;
        Assert.That(player.GetComponentInChildren<SpriteRenderer>().sprite.name, Does.StartWith("HaoJuIdle"));
        for (int i = 0; i < 30 && player.SurfaceVelocity > 0; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position.x, Is.GreaterThanOrEqualTo(-10.52f));
        Assert.That(player.SurfaceVelocity, Is.Zero);
        Assert.That(body.linearVelocityX, Is.EqualTo(0).Within(.01f));

        PutPlayer(body, 0);
        for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();
        Assert.That(player.SurfaceVelocity, Is.EqualTo(-1.45f).Within(.01f));
        Assert.That(body.position.x, Is.LessThan(0));
        player.Simulate(new PlayerCommand(0, true, true, false), .02f);
        Assert.That(player.Grounded, Is.False);
        Assert.That(player.SurfaceVelocity, Is.Zero);
        Assert.That(body.linearVelocityX, Is.EqualTo(-1.45f).Within(.01f));
    }

    [UnityTest]
    public IEnumerator FlyingEnemyWarnsLocksItsDiveAndCanHurtAnUnguardedCat() {
        yield return SceneManager.LoadSceneAsync("ColdStorage02");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var body = stage.Player.GetComponent<Rigidbody2D>();
        var health = stage.Player.GetComponent<Health>();
        PutPlayer(body, -1.1f);
        for (int i = 0; i < 75 && stage.Fly.State != ColdFlyState.Windup; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Fly.State, Is.EqualTo(ColdFlyState.Windup));
        var end = stage.Fly.DiveEndpoint;
        yield return null;
        Assert.That(stage.Fly.transform.Find("Dive trace").GetComponent<SpriteRenderer>().enabled, Is.True);
        Assert.That(health.Current, Is.EqualTo(4));
        for (int i = 0; i < 48 && health.Current == 4; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Fly.DiveEndpoint, Is.EqualTo(end));
        Assert.That(health.Current, Is.EqualTo(2));
        Assert.That(stage.Goal.IsOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator JiangZhuaCanParryTheDiveCounterAndClearTheLoadingExit() {
        yield return SceneManager.LoadSceneAsync("ColdStorage02");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        stage.SelectCharacter(true);
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        var health = player.GetComponent<Health>();
        var combat = player.GetComponent<PlayerCombat>();
        var flyBody = stage.Fly.GetComponent<Rigidbody2D>();
        PutPlayer(body, -1.1f);
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            for (int i = 0; i < 125 && !(stage.Fly.State == ColdFlyState.Dive && flyBody.position.y <= 1.48f); i++)
                yield return new WaitForFixedUpdate();
            Assert.That(stage.Fly.State, Is.EqualTo(ColdFlyState.Dive));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
            for (int i = 0; i < 20 && !combat.CanCounterAttack; i++) yield return new WaitForFixedUpdate();
            Assert.That(health.Current, Is.EqualTo(6));
            Assert.That(combat.CanCounterAttack, Is.True);
            Assert.That(stage.Fly.State, Is.EqualTo(ColdFlyState.Recovery));
            Assert.That(stage.Fly.Weapon.IsRunning, Is.False);
            for (int i = 0; i < 20 && flyBody.position.y > .72f; i++) yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K, Key.J));
            for (int i = 0; i < 24 && stage.Fly.Life.Current == 6; i++) yield return new WaitForFixedUpdate();
            Assert.That(stage.Fly.Life.Current, Is.EqualTo(3));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());

            float previous = Time.timeScale;
            Time.timeScale = 0;
            try {
                // Use real claw hitboxes at ground-level recovery range.
                for (int i = 0; i < 24; i++) player.Simulate(default, .02f);
                for (int strike = 0; strike < 3; strike++) {
                    player.Simulate(new PlayerCommand(0, false, false, false, attackPressed: true), .02f);
                    for (int i = 0; i < 24; i++) player.Simulate(default, .02f);
                }
                Assert.That(stage.Fly.Life.IsDead, Is.True);
                Assert.That(stage.Goal.IsOpen, Is.True);
            } finally { Time.timeScale = previous; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            for (int i = 0; i < 180 && !stage.Goal.Collected; i++) yield return new WaitForFixedUpdate();
            Assert.That(stage.Goal.Collected, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            for (int i = 0; i < 65 && SceneManager.GetActiveScene().name != "LoadingBay03"; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LoadingBay03"));
            var next = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(next.IsLoadingBay, Is.True);
            Assert.That(next.Player.Definition.SupportsGuard, Is.True);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static void PutPlayer(Rigidbody2D body, float x) {
        body.position = new Vector2(x, .12f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
}
}
