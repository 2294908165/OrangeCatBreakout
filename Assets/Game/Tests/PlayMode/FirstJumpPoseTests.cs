using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class FirstJumpPoseTests {
    [UnityTest]
    public IEnumerator BothCatsRiseFallAndSplashOnLanding() {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            foreach (bool jiang in new[] { false, true }) {
                yield return SceneManager.LoadSceneAsync("Warehouse01");
                var stage = Object.FindFirstObjectByType<WarehouseRun>();
                stage.SelectCharacter(jiang);
                yield return new WaitForFixedUpdate();
                var player = stage.Player;
                var visual = player.GetComponentInChildren<SpriteRenderer>();
                var splash = player.transform.Find("Landing splash").GetComponent<SpriteRenderer>();
                var scale = visual.transform.localScale;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                for (int i = 0; i < 30 && player.JumpsUsed == 0; i++) yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(player.JumpsUsed, Is.EqualTo(1));
                Assert.That(visual.sprite.name, Is.EqualTo("JumpRise"));
                Assert.That(splash.enabled, Is.False);
                for (int i = 0; i < 40 && visual.sprite.name != "JumpFall"; i++) yield return new WaitForFixedUpdate();
                Assert.That(visual.sprite.name, Is.EqualTo("JumpFall"));
                Assert.That(player.Grounded, Is.False);
                for (int i = 0; i < 40 && !player.Grounded; i++) yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(player.Grounded, Is.True);
                Assert.That(visual.sprite.name, Is.EqualTo("JumpLand"));
                Assert.That(visual.transform.localScale, Is.EqualTo(scale));
                Assert.That(splash.enabled, Is.True);
                Assert.That(splash.transform.position.y, Is.LessThan(-.30f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSeconds(.20f);
                Assert.That(visual.sprite.name, Does.StartWith(jiang ? "JiangZhuaIdle" : "HaoJuIdle"));
                Assert.That(splash.enabled, Is.False);
            }
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
