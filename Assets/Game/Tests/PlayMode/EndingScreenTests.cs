using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class EndingScreenTests {
    [UnityTest]
    public IEnumerator EndingAndDefeatBothHaveWorkingExitRoutes() {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            yield return SceneManager.LoadSceneAsync("EndingScreen");
            yield return PressAndLoad(keyboard, Key.Enter, "Warehouse01");
            var stage = Object.FindFirstObjectByType<WarehouseRun>();
            KillPlayer(stage);
            yield return new WaitForSecondsRealtime(.65f);
            yield return PressAndLoad(keyboard, Key.T, "TitleScreen");
            yield return SceneManager.LoadSceneAsync("EndingScreen");
            yield return PressAndLoad(keyboard, Key.Escape, "TitleScreen");
            yield return SceneManager.LoadSceneAsync("ColdStorage02");
            stage = Object.FindFirstObjectByType<WarehouseRun>();
            KillPlayer(stage);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            float timeout = Time.realtimeSinceStartup + 4;
            while (stage != null && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(stage == null, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ColdStorage02"));
            Assert.That(Object.FindFirstObjectByType<WarehouseRun>().Player.GetComponent<Health>().IsDead, Is.False);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static void KillPlayer(WarehouseRun stage) {
        var player = stage.Player;
        var life = player.GetComponent<Health>();
        life.TakeDamage(new DamageInfo(null, life.Current, player.transform.position,
            Vector2.left, 0, DamageType.Hazard, false));
        Assert.That(life.IsDead, Is.True);
    }

    static IEnumerator PressAndLoad(Keyboard keyboard, Key key, string scene) {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        float timeout = Time.realtimeSinceStartup + 4;
        while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }
}
}
