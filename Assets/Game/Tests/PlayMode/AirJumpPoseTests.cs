using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class AirJumpPoseTests {
    [UnityTest]
    public IEnumerator BothCatsReuseRollPosesOnSecondSpacePressWithoutScaling() {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            foreach (bool useJiangZhua in new[] { false, true }) {
                yield return SceneManager.LoadSceneAsync("PixelPreview");
                var session = Object.FindFirstObjectByType<CombatTestSession>();
                if (useJiangZhua) session.SwitchCharacter(true);
                var motor = session.Player;
                var sprite = motor.GetComponentInChildren<SpriteRenderer>();
                var collider = motor.GetComponent<BoxCollider2D>();
                var size = collider.size;
                var visualScale = sprite.transform.localScale;
                var standingSprite = sprite.sprite;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                for (int i = 0; i < 25 && motor.JumpsUsed < 1; i++) yield return new WaitForFixedUpdate();
                Assert.That(motor.JumpsUsed, Is.EqualTo(1));
                yield return null;
                Assert.That(sprite.sprite.name, Is.EqualTo("JumpRise"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForFixedUpdate();
                Assert.That(keyboard.spaceKey.isPressed, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                for (int i = 0; i < 25 && motor.JumpsUsed < 2; i++) yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(motor.JumpsUsed, Is.EqualTo(2));
                Assert.That(sprite.sprite.name, Is.EqualTo("Roll2"));
                Assert.That(collider.enabled, Is.True);
                Assert.That(collider.size, Is.EqualTo(size));

                // Walk through the same sized curled and recovery frames before landing.
                bool sawTurn = false, sawUncurl = false;
                for (int i = 0; i < 20 && !sawUncurl; i++) {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    sawTurn |= sprite.sprite.name == "Roll3";
                    sawUncurl |= sprite.sprite.name == "Roll4";
                    Assert.That(sprite.transform.localScale, Is.EqualTo(visualScale));
                    Assert.That(sprite.sprite.pixelsPerUnit, Is.EqualTo(standingSprite.pixelsPerUnit));
                }
                Assert.That(sawTurn && sawUncurl, Is.True);
                Assert.That(motor.Grounded, Is.False);
                Assert.That(motor.States.Current, Is.EqualTo(PlayerState.Free));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                for (int i = 0; i < 75 && !motor.Grounded; i++) yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(motor.Grounded, Is.True);
                Assert.That(motor.JumpsUsed, Is.Zero);
                Assert.That(sprite.sprite.name, Is.EqualTo("JumpLand"));
                yield return new WaitForSeconds(.14f);
                Assert.That(sprite.sprite.name, Does.StartWith(useJiangZhua ? "JiangZhuaIdle" : "HaoJuIdle"));
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
