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
public sealed class RooftopPlayModeTests {
    [UnityTest]
    public IEnumerator PlayerCanCrossTheRoofPowerBothBreakersAndEscape() {
        yield return SceneManager.LoadSceneAsync("Rooftop04");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        Assert.That(stage.IsRooftop, Is.True);
        var roof = stage.Rooftop;
        var player = stage.Player;
        var body = player.GetComponent<Rigidbody2D>();
        var gate = GameObject.Find("Roof gate").GetComponent<BoxCollider2D>();
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            Keys(keyboard, Key.E);
            yield return new WaitForFixedUpdate();
            Assert.That(roof.PoweredCount, Is.Zero);
            Assert.That(gate.enabled, Is.True);

            Keys(keyboard, Key.D);
            yield return Until(() => body.position.x >= -16.25f, 65, "Reach the first vent approach");
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= -12.8f, 50, "Clear the first vent");
            Keys(keyboard, Key.D);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= -6.95f, 70, "Jump to the first breaker ledge");
            Keys(keyboard);
            yield return Until(() => player.Grounded, 65, "Land on the breaker ledge");

            Keys(keyboard, Key.E);
            yield return Until(() => roof.PoweredCount == 1, 15, "Power the first breaker");
            Assert.That(roof.ExitOpen, Is.False);
            Assert.That(gate.enabled, Is.True);
            Keys(keyboard);
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.E);
            yield return new WaitForFixedUpdate();
            Assert.That(roof.PoweredCount, Is.EqualTo(1), "A breaker only counts once.");

            Keys(keyboard, Key.D);
            yield return Until(() => body.position.x >= -5.05f, 45, "Reach the catwalk approach");
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= -1.7f, 50, "Cross the first catwalk vent");
            Keys(keyboard, Key.D);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= 1.35f, 60, "Clear the second catwalk vent");
            Keys(keyboard);
            yield return new WaitForFixedUpdate();
            yield return Until(() => player.Grounded, 65, "Land between the vent and the exit roof");
            Keys(keyboard, Key.D, Key.Space);
            yield return Until(() => body.position.x >= 4.70f, 60, "Jump to the second breaker ledge");
            Keys(keyboard);
            yield return Until(() => player.Grounded, 65, "Land on the second breaker ledge");

            Keys(keyboard, Key.E);
            yield return Until(() => roof.ExitOpen, 15, "Power the second breaker");
            Assert.That(gate.enabled, Is.False);
            Assert.That(stage.Cleared, Is.False);
            Keys(keyboard, Key.D);
            yield return Until(() => body.position.x >= 10.35f, 85, "Reach the roof exit");
            Keys(keyboard);
            yield return new WaitForFixedUpdate();
            Assert.That(roof.CanLeave, Is.True, "The exit can be used after stopping at its marker.");
            Keys(keyboard, Key.E);
            yield return Until(() => stage.Cleared, 15, "Use the exit");
            Assert.That(player.GetComponent<Health>().IsDead, Is.False);
            Assert.That(player.GetComponent<PlayerInputReader>().enabled, Is.False);

            Keys(keyboard, Key.R);
            yield return Until(() => stage == null, 70, "Retry the rooftop");
            var retry = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(retry.Rooftop.PoweredCount, Is.Zero);
            Assert.That(retry.Cleared, Is.False);
            Assert.That(GameObject.Find("Roof gate").GetComponent<BoxCollider2D>().enabled, Is.True);

            retry.SelectCharacter(true);
            var retryBody = retry.Player.GetComponent<Rigidbody2D>();
            Place(retryBody, GameObject.Find("Breaker B lamp").transform.position);
            retry.Rooftop.Interact();
            Place(retryBody, new Vector2(7.2f, 1.82f));
            Assert.That(retry.Rooftop.Prompt(), Does.Contain("Breaker A is left"));
            Assert.That(retry.Rooftop.CanLeave, Is.False);
            Keys(keyboard, Key.D);
            for (int i = 0; i < 14; i++) yield return new WaitForFixedUpdate();
            Assert.That(retryBody.position.x, Is.LessThan(8.25f), "The locked gate must block movement.");

            Place(retryBody, GameObject.Find("Breaker A lamp").transform.position);
            retry.Rooftop.Interact();
            Assert.That(retry.Rooftop.ExitOpen, Is.True);
            Place(retryBody, new Vector2(12.8f, 1.82f));
            Assert.That(retry.Rooftop.CanLeave, Is.True, "The exit still works after passing the marker.");
            Keys(keyboard, Key.E);
            yield return Until(() => retry.Cleared, 15, "Use the exit beside the right parapet");
            Assert.That(retry.Cleared, Is.True);
            Keys(keyboard);
            yield return new WaitForFixedUpdate();
            Keys(keyboard, Key.E);
            yield return Until(() => SceneManager.GetActiveScene().name == "CanGuard05", 75, "Continue to the boss room");
            var bossStage = Object.FindFirstObjectByType<WarehouseRun>();
            Assert.That(bossStage.IsBoss, Is.True);
            Assert.That(bossStage.Player.Definition.name, Is.EqualTo("JiangZhua"));
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static void Place(Rigidbody2D body, Vector2 position) {
        body.position = position;
        body.transform.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    static IEnumerator Until(Func<bool> condition, int steps, string label) {
        for (int i = 0; i < steps && !condition(); i++) yield return new WaitForFixedUpdate();
        Assert.That(condition(), Is.True, label);
    }

    static void Keys(Keyboard keyboard, params Key[] keys) =>
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
}
}
