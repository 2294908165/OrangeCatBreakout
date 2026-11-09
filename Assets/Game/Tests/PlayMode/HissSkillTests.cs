using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class HissSkillTests {
    [UnityTest]
    public IEnumerator HissHasOneMirroredLocalHitAndCooldown() {
        yield return SceneManager.LoadSceneAsync("PixelPreview");
        yield return new WaitForFixedUpdate();
        var session = Object.FindFirstObjectByType<CombatTestSession>();
        var player = session.Player;
        Vector3 center = player.transform.position;
        session.Dummy.transform.position = center + Vector3.left;
        session.Source.transform.position = center + Vector3.right;
        var far = Object.Instantiate(session.Dummy, center + Vector3.left * 3, Quaternion.identity);
        Physics2D.SyncTransforms();
        var body = player.GetComponent<PlayerCombat>();
        var art = player.GetComponentInChildren<SpriteRenderer>();
        var ripple = player.transform.Find("Hiss sound wave").GetComponent<SpriteRenderer>();
        float previous = Time.timeScale;
        Time.timeScale = 0;
        try {
            player.Simulate(new PlayerCommand(-1, false, false, false, hissPressed: true), .02f);
            Assert.That(body.Weapon.Definition.Type, Is.EqualTo(DamageType.Sonic));
            Advance(player, 4);
            Assert.That(session.Dummy.Current, Is.EqualTo(20), "Startup must not hit.");
            Advance(player, 3);
            yield return null;
            Assert.That(session.Dummy.Current, Is.EqualTo(19));
            Assert.That(session.Source.Current, Is.EqualTo(20), "Rear target must not be hit.");
            Assert.That(far.Current, Is.EqualTo(20), "Distant target must not be hit.");
            Assert.That(ripple.enabled && ripple.flipX, Is.True);
            Assert.That(art.sprite.name, Is.EqualTo("HaoJuHiss"));
            Advance(player, 20);
            yield return null;
            Assert.That(ripple.enabled, Is.False);
            Assert.That(session.Dummy.Current, Is.EqualTo(19), "Overlapping Hurtboxes share one damage attempt.");
            player.Simulate(new PlayerCommand(0, false, false, false, hissPressed: true), .02f);
            Assert.That(body.Weapon.IsRunning, Is.False);
            Advance(player, 70);
            player.Simulate(new PlayerCommand(0, false, false, false, hissPressed: true), .02f);
            Assert.That(body.Weapon.IsRunning, Is.True);

            session.SwitchCharacter(true);
            Physics2D.SyncTransforms();
            session.Player.Simulate(new PlayerCommand(0, false, false, false, hissPressed: true), .02f);
            Assert.That(session.Player.GetComponent<PlayerCombat>().Weapon.IsRunning, Is.False);
        } finally {
            Object.Destroy(far.gameObject);
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator RealQInputCastsOnceEvenWhenHeld() {
        yield return SceneManager.LoadSceneAsync("PixelPreview");
        var session = Object.FindFirstObjectByType<CombatTestSession>();
        session.Dummy.transform.position = session.Player.transform.position + Vector3.right;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            Assert.That(Object.FindFirstObjectByType<AudioListener>(), Is.Not.Null);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
            for (int i = 0; i < 80; i++) yield return new WaitForFixedUpdate();
            Assert.That(keyboard.qKey.isPressed, Is.True);
            Assert.That(session.Dummy.Current, Is.EqualTo(19));
            Assert.That(session.Player.GetComponent<PlayerCombat>().HissCooldownRemaining, Is.Zero);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }

    static void Advance(PlayerMotor2D player, int count) {
        for (int i = 0; i < count; i++) player.Simulate(default, .02f);
    }
}
}
