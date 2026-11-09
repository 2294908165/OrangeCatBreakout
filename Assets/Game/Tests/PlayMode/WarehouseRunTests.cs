using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class WarehouseRunTests {
    [UnityTest]
    public IEnumerator RatTelegraphsThenChargesAndDamagesAnUnguardedPlayer() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        yield return new WaitForFixedUpdate();
        Assert.That(stage.Rat.State, Is.EqualTo(RatState.Waiting));
        Assert.That(stage.Goal.IsOpen, Is.False);
        var playerLife = stage.Player.GetComponent<Health>();
        PutPlayerAt(stage, stage.Rat.transform.position.x - 1.05f);
        for (int i = 0; i < 20 && stage.Rat.State != RatState.Windup; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Rat.State, Is.EqualTo(RatState.Windup));
        yield return null;
        Assert.That(stage.Rat.transform.Find("Charge warning").GetComponent<SpriteRenderer>().enabled, Is.True);
        Assert.That(playerLife.Current, Is.EqualTo(4));
        for (int i = 0; i < 40 && stage.Rat.State != RatState.Charge; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Rat.State, Is.EqualTo(RatState.Charge));
        Assert.That(stage.Rat.GetComponent<Rigidbody2D>().linearVelocityX, Is.LessThan(0));
        Assert.That(playerLife.Current, Is.EqualTo(2));
        for (int i = 0; i < 24 && stage.Rat.State != RatState.Recovery; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Rat.State, Is.EqualTo(RatState.Recovery));
    }

    [UnityTest]
    public IEnumerator PerfectGuardInterruptsChargeAndCounterHitsRat() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        stage.SelectCharacter(true);
        var combat = stage.Player.GetComponent<PlayerCombat>();
        yield return new WaitForFixedUpdate();
        PutPlayerAt(stage, stage.Rat.transform.position.x - 1f);

        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            for (int i = 0; i < 35 && stage.Rat.Weapon.Elapsed < .33f; i++) yield return new WaitForFixedUpdate();
            Assert.That(stage.Rat.State, Is.EqualTo(RatState.Windup));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
            for (int i = 0; i < 15 && !combat.CanCounterAttack; i++) yield return new WaitForFixedUpdate();
            Assert.That(combat.CanCounterAttack, Is.True);
            Assert.That(stage.Player.GetComponent<Health>().Current, Is.EqualTo(6));
            Assert.That(stage.Rat.State, Is.EqualTo(RatState.Recovery));
            Assert.That(stage.Rat.Weapon.IsRunning, Is.False);

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K, Key.J));
            for (int i = 0; i < 20 && stage.Rat.Life.Current == 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(stage.Rat.Life.Current, Is.EqualTo(5));
            Assert.That(combat.CanCounterAttack, Is.False);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    [UnityTest]
    public IEnumerator ClawTriggersRatHitPoseSparkAndPhysicalRecoil() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        yield return new WaitForFixedUpdate();
        var ratBody = stage.Rat.GetComponent<Rigidbody2D>();
        PutPlayerAt(stage, ratBody.position.x - .98f);
        yield return new WaitForFixedUpdate();
        var visual = stage.Rat.transform.Find("Rat visual").GetComponent<SpriteRenderer>();
        var spark = stage.Rat.transform.Find("Rat hit spark").GetComponent<SpriteRenderer>();
        Assert.That(spark.enabled, Is.False);
        float beforeHit = ratBody.position.x;
        float previous = Time.timeScale;
        Time.timeScale = 0;
        try {
            stage.Player.Simulate(new PlayerCommand(0, false, false, false, attackPressed: true), .02f);
            for (int i = 0; i < 5; i++) stage.Player.Simulate(default, .02f);
            yield return null;
            Assert.That(stage.Rat.Life.Current, Is.EqualTo(6));
            Assert.That(stage.Rat.State, Is.EqualTo(RatState.Hurt));
            Assert.That(visual.sprite.name, Is.EqualTo("RatHurt"));
            Assert.That(spark.enabled, Is.True);
            Assert.That(spark.transform.position.x, Is.LessThan(beforeHit));
        } finally { Time.timeScale = previous; }

        var hitPoint = spark.transform.position;
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(ratBody.position.x, Is.GreaterThan(beforeHit));
        Assert.That(Vector3.Distance(spark.transform.position, hitPoint), Is.LessThan(.001f));
        yield return new WaitForSeconds(.15f);
        Assert.That(spark.enabled, Is.False);
    }

    [UnityTest]
    public IEnumerator CanCannotBeCollectedUntilRatIsDefeated() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        yield return new WaitForFixedUpdate();
        PutPlayerAt(stage, stage.Goal.transform.position.x);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(stage.Goal.Collected, Is.False);
        Assert.That(GameObject.Find("Locked warehouse gate").GetComponent<BoxCollider2D>().enabled, Is.True);

        PutPlayerAt(stage, stage.Rat.transform.position.x - 1.05f);
        yield return new WaitForFixedUpdate();
        float previous = Time.timeScale;
        Time.timeScale = 0;
        try {
            // Four real claw instances hit the rat's Hurtbox, once each.
            for (int hit = 0; hit < 4; hit++) {
                stage.Player.Simulate(new PlayerCommand(0, false, false, false, attackPressed: true), .02f);
                for (int i = 0; i < 22; i++) stage.Player.Simulate(default, .02f);
            }
            Assert.That(stage.Rat.State, Is.EqualTo(RatState.Dead));
            Assert.That(stage.Goal.IsOpen, Is.True);
            Assert.That(stage.Goal.Collected, Is.False);
            Assert.That(GameObject.Find("Locked warehouse gate").GetComponent<BoxCollider2D>().enabled, Is.False);
        } finally { Time.timeScale = previous; }
        PutPlayerAt(stage, stage.Goal.transform.position.x);
        for (int i = 0; i < 10 && !stage.Goal.Collected; i++) yield return new WaitForFixedUpdate();
        Assert.That(stage.Goal.Collected, Is.True);
    }

    static void PutPlayerAt(WarehouseRun stage, float x) {
        var body = stage.Player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, .12f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
}
}
