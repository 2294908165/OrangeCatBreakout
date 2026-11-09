using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class PixelPreviewTests {
    [UnityTest]
    public IEnumerator PixelSceneSpawnsBothSpritesAndUsesCombat() {
        yield return SceneManager.LoadSceneAsync("PixelPreview");
        var session = Object.FindFirstObjectByType<CombatTestSession>();
        Assert.That(session, Is.Not.Null);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        AssertSprite(session.Player, "HaoJu");
        Assert.That(session.Player.Grounded, Is.True);
        session.Dummy.transform.position = session.Player.transform.position + Vector3.right;
        Physics2D.SyncTransforms();
        var previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        try {
            session.Player.Simulate(new PlayerCommand(0, false, false, false, true), .02f);
            for (int i = 0; i < 6; i++) session.Player.Simulate(default, .02f);
            Assert.That(session.Dummy.Current, Is.EqualTo(18));

            session.SwitchCharacter(true);
            AssertSprite(session.Player, "JiangZhua");
            Assert.That(session.Player.GetComponent<Health>().Current, Is.EqualTo(6));
        } finally { Time.timeScale = previousTimeScale; }
    }

    static void AssertSprite(PlayerMotor2D player, string name) {
        var renderer = player.GetComponentInChildren<SpriteRenderer>();
        var body = player.GetComponent<BoxCollider2D>();
        Assert.That(renderer.sprite.name, Does.StartWith(name));
        Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(32));
        Assert.That(renderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(renderer.sprite.pivot.y, Is.Zero);
        Assert.That(renderer.transform.localPosition.y, Is.EqualTo(body.offset.y - body.size.y * .5f).Within(.002f));
    }
}
}
