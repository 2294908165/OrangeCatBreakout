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
public sealed class WarehousePlaythroughTests {
    [UnityTest]
    public IEnumerator BothCatsClearWarehouseUsingNormalInputs() {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            foreach (bool jiang in new[] { false, true })
                yield return ClearWith(keyboard, jiang);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static IEnumerator ClearWith(Keyboard keyboard, bool jiang) {
        Keys(keyboard);
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        stage.SelectCharacter(jiang);
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        var life = player.GetComponent<Health>();
        var combat = player.GetComponent<PlayerCombat>();
        var ratBody = stage.Rat.GetComponent<Rigidbody2D>();
        yield return new WaitForFixedUpdate();

        Keys(keyboard, Key.D);
        yield return Until(() => body.position.x >= -13.15f, 80, "Cross the quiet steam vent");
        Keys(keyboard, Key.D, Key.Space);
        yield return Until(() => body.position.x >= -10.15f, 55, "Jump the spikes");
        Keys(keyboard, Key.D);
        yield return Until(() => body.position.x >= -8.78f, 30, "Reach the crate edge");
        Keys(keyboard, Key.D, Key.Space);
        yield return Until(() => body.position.x >= -4.2f, 55, "Clear the fan");
        Assert.That(life.Current, Is.EqualTo(life.MaxHealth));

        Keys(keyboard, Key.D);
        yield return Until(() => ratBody.position.x - body.position.x <= 1.05f, 70, "Close on the rat");
        Keys(keyboard);
        float cue = jiang ? .33f : .28f;
        yield return Until(() => stage.Rat.State == RatState.Windup && stage.Rat.Weapon.Elapsed >= cue,
            35, "Read the charge windup");

        if (jiang) {
            Keys(keyboard, Key.K);
            yield return Until(() => combat.CanCounterAttack, 18, "Perfect guard the charge");
            Keys(keyboard, Key.K, Key.J);
            yield return Until(() => stage.Rat.Life.Current <= 5, 22, "Land the counter");
        } else {
            Keys(keyboard, Key.LeftShift);
            yield return Until(() => player.States.Current == PlayerState.Rolling, 10, "Start the dodge roll");
            Keys(keyboard);
            yield return Until(() => player.States.Current == PlayerState.Free, 30, "Finish the dodge");
            Assert.That(body.position.x, Is.GreaterThan(ratBody.position.x));
        }
        Keys(keyboard);
        Assert.That(life.Current, Is.EqualTo(life.MaxHealth), "The charge should be avoided on both routes.");
        yield return new WaitForFixedUpdate();

        for (int attempt = 0; attempt < 12 && !stage.Rat.Life.IsDead; attempt++) {
            yield return Until(() => player.States.Current == PlayerState.Free, 45, "Return to neutral");
            for (int tick = 0; tick < 75 && Mathf.Abs(ratBody.position.x - body.position.x) > 1.25f; tick++) {
                Keys(keyboard, ratBody.position.x > body.position.x ? Key.D : Key.A);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(Mathf.Abs(ratBody.position.x - body.position.x), Is.LessThanOrEqualTo(1.25f));
            yield return Until(() => player.States.Current == PlayerState.Free, 28, "Wait for a legal attack");
            Keys(keyboard, ratBody.position.x > body.position.x ? Key.D : Key.A, Key.J);
            yield return Until(() => player.States.Current == PlayerState.Attacking, 15, "Start a normal claw");
            Keys(keyboard);
            yield return Until(() => player.States.Current != PlayerState.Attacking, 30, "Finish the claw");
            Assert.That(life.IsDead, Is.False);
        }
        Assert.That(stage.Rat.Life.IsDead, Is.True);
        Assert.That(stage.Goal.IsOpen, Is.True);
        Keys(keyboard, Key.D);
        yield return Until(() => stage.Goal.Collected, 165, "Walk through the open gate and take the can");
        if (jiang) {
            Keys(keyboard, Key.E);
            yield return Until(() => SceneManager.GetActiveScene().name == "ColdStorage02", 65,
                "Continue to the cold storage level");
            var next = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(next.IsColdStorage, Is.True);
            Assert.That(next.Player.Definition.SupportsGuard, Is.True, "Keep the selected cat between levels.");
        }
    }

    static IEnumerator Until(Func<bool> condition, int steps, string label) {
        for (int i = 0; i < steps && !condition(); i++) yield return new WaitForFixedUpdate();
        Assert.That(condition(), Is.True, label);
    }

    static void Keys(Keyboard keyboard, params Key[] keys) =>
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
}
}
