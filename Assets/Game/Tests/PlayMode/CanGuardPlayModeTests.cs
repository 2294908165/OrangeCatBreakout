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
public sealed class CanGuardPlayModeTests {
    [UnityTest]
    public IEnumerator ChargeHitsOnceAndSlamWavesCanBeJumped() {
        yield return SceneManager.LoadSceneAsync("CanGuard05");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var boss = stage.Boss;
        var player = stage.Player;
        var health = player.GetComponent<Health>();
        var marker = boss.transform.Find("Boss danger marker").GetComponent<SpriteRenderer>();
        Place(player, -1.45f);

        yield return Until(() => boss.State == CanGuardState.ChargeTell, 50, "Show the charge tell");
        yield return null;
        Assert.That(marker.enabled, Is.True);
        Assert.That(health.Current, Is.EqualTo(health.MaxHealth));
        yield return Until(() => boss.State == CanGuardState.Recovering, 150, "Reach the left wall and recover");
        Assert.That(boss.transform.position.x, Is.LessThan(-8.7f));
        Assert.That(health.Current, Is.EqualTo(health.MaxHealth - 2));

        Place(player, -3.2f);
        yield return Until(() => boss.State == CanGuardState.SlamTell, 110, "Alternate into the slam tell");
        float landing = boss.LandingX;
        Place(player, landing + 2.8f);
        yield return Until(() => boss.State == CanGuardState.Airborne, 55, "Jump after the slam warning");
        Assert.That(boss.LandingX, Is.EqualTo(landing));
        yield return Until(() => boss.State == CanGuardState.Recovering, 60, "Land and launch both waves");
        var left = GameObject.Find("Left ground wave");
        var right = GameObject.Find("Right ground wave");
        Assert.That(left, Is.Not.Null);
        Assert.That(right, Is.Not.Null);
        Assert.That(player.Grounded, Is.True);
        yield return HoldJumpUntil(() => right.transform.position.x > landing + 3.80f);
        Assert.That(health.Current, Is.EqualTo(health.MaxHealth - 2));
        Place(player, right.transform.position.x + 1.45f);
        yield return Until(() => health.Current < health.MaxHealth - 2, 30, "A grounded player takes wave damage");
        Assert.That(health.Current, Is.EqualTo(health.MaxHealth - 3));

        HitBoss(boss, player, boss.Life.Current);
        yield return null;
        Assert.That(boss.State, Is.EqualTo(CanGuardState.Dead));
        Assert.That(left.activeSelf || right.activeSelf, Is.False);
        Assert.That(boss.transform.Find("Guard pose").GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("Defeated"));
    }

    [UnityTest]
    public IEnumerator ClawFinishesGuardAndLastCanCompletesTheDemo() {
        yield return SceneManager.LoadSceneAsync("CanGuard05");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        var player = stage.Player;
        var boss = stage.Boss;
        Assert.That(stage.IsBoss, Is.True);
        Place(player, stage.Goal.transform.position.x);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(stage.Cleared, Is.False, "The can stays locked while the guard lives.");
        HitBoss(boss, player, boss.Life.MaxHealth - 2);
        Place(player, boss.transform.position.x - 1.42f);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        player.Simulate(new PlayerCommand(1, false, false, false, true), Time.fixedDeltaTime);
        yield return Until(() => boss.Life.IsDead, 40, "The player's claw hits the guard's hurtbox");
        Assert.That(stage.Goal.IsOpen, Is.True);
        Assert.That(stage.Cleared, Is.False, "Beating the guard opens the door before collecting the can.");

        Place(player, stage.Goal.transform.position.x);
        yield return Until(() => stage.Cleared, 25, "Collect the last can");
        Assert.That(player.GetComponent<PlayerInputReader>().enabled, Is.False);
    }

    static void HitBoss(CanGuardBrain boss, PlayerMotor2D player, float amount) {
        Assert.That(boss.Life.TakeDamage(new DamageInfo(player.gameObject, amount, boss.transform.position,
            Vector2.right, 0, DamageType.Claw, true)), Is.EqualTo(DamageOutcome.Hit));
    }

    static void Place(PlayerMotor2D player, float x) {
        var body = player.GetComponent<Rigidbody2D>();
        var point = new Vector2(x, .12f);
        body.position = point;
        player.transform.position = point;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    static IEnumerator Until(Func<bool> condition, int steps, string label) {
        for (int i = 0; i < steps && !condition(); i++) yield return new WaitForFixedUpdate();
        Assert.That(condition(), Is.True, label);
    }

    static IEnumerator HoldJumpUntil(Func<bool> wavePassed) {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return Until(wavePassed, 42, "Jump over the right wave");
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }
}
}
