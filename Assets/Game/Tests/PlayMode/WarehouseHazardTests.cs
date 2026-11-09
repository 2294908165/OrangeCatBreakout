using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class WarehouseHazardTests {
    [UnityTest]
    public IEnumerator SteamWarnsBeforeItCanDealDamage() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var run = Object.FindFirstObjectByType<WarehouseRun>();
        var life = run.Player.GetComponent<Health>();
        var vent = FindHazard(WarehouseHazardKind.SteamVent);
        PutPlayerAt(run, vent.transform.position.x);
        Assert.That(vent.IsDangerous, Is.False);
        for (int i = 0; i < 90 && vent.Phase == WarehouseHazardPhase.Safe; i++) yield return new WaitForFixedUpdate();
        Assert.That(vent.Phase, Is.EqualTo(WarehouseHazardPhase.Warning));
        Assert.That(vent.transform.Find("Steam warning").GetComponent<SpriteRenderer>().enabled, Is.True);
        Assert.That(life.Current, Is.EqualTo(4));
        for (int i = 0; i < 45 && life.Current == 4; i++) yield return new WaitForFixedUpdate();
        Assert.That(vent.Phase, Is.EqualTo(WarehouseHazardPhase.Active));
        Assert.That(vent.transform.Find("Steam plume").GetComponent<SpriteRenderer>().enabled, Is.True);
        Assert.That(life.Current, Is.EqualTo(3));
        for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
        Assert.That(life.Current, Is.EqualTo(3));
        Assert.That(run.Rat.Engaged, Is.False);
    }

    [UnityTest]
    public IEnumerator SpikesAndTurningFanHurtOnContact() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var run = Object.FindFirstObjectByType<WarehouseRun>();
        var life = run.Player.GetComponent<Health>();
        PutPlayerAt(run, FindHazard(WarehouseHazardKind.Spikes).transform.position.x);
        for (int i = 0; i < 8 && life.Current == 4; i++) yield return new WaitForFixedUpdate();
        Assert.That(life.Current, Is.EqualTo(3));
        yield return new WaitForSeconds(.65f);

        var fan = FindHazard(WarehouseHazardKind.Fan);
        var rotor = fan.transform.Find("Fan rotor");
        var rotation = rotor.localRotation;
        PutPlayerAt(run, fan.transform.position.x);
        for (int i = 0; i < 8 && life.Current == 3; i++) yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(Quaternion.Angle(rotation, rotor.localRotation), Is.GreaterThan(1));
        Assert.That(life.Current, Is.EqualTo(2));
        Assert.That(run.Rat.Engaged, Is.False);
    }

    [UnityTest]
    public IEnumerator FullRouteCanBeCrossedWithoutDamage() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var run = Object.FindFirstObjectByType<WarehouseRun>();
        var player = run.Player.GetComponent<Rigidbody2D>();
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            for (int i = 0; i < 80 && player.position.x < -13.15f; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.position.x, Is.GreaterThanOrEqualTo(-13.15f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            for (int i = 0; i < 50 && player.position.x < -10.15f; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.position.x, Is.GreaterThanOrEqualTo(-10.15f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            for (int i = 0; i < 30 && player.position.x < -8.78f; i++) yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            for (int i = 0; i < 55 && player.position.x < -4.2f; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.position.x, Is.GreaterThanOrEqualTo(-4.2f));
            Assert.That(run.Player.GetComponent<Health>().Current, Is.EqualTo(4));
            Assert.That(run.Rat.Engaged, Is.False);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static WarehouseHazard FindHazard(WarehouseHazardKind kind) {
        foreach (var hazard in Object.FindObjectsByType<WarehouseHazard>(FindObjectsSortMode.None))
            if (hazard.Kind == kind) return hazard;
        throw new AssertionException("Missing warehouse hazard " + kind);
    }

    static void PutPlayerAt(WarehouseRun run, float x) {
        var body = run.Player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, .12f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
}
}
