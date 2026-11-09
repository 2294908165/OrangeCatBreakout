using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class PlayerIdleTests {
    [UnityTest]
    public IEnumerator BothCatsIdleThroughFourFramesWithoutMovingAndFreezeWhenPaused() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        foreach (bool jiang in new[] { false, true }) {
            stage.SelectCharacter(jiang);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            var player = stage.Player;
            var art = player.GetComponentInChildren<SpriteRenderer>();
            var body = player.GetComponent<Rigidbody2D>();
            var x = body.position.x;
            var scale = art.transform.localScale;
            var seen = new HashSet<string>();
            string prefix = jiang ? "JiangZhuaIdle" : "HaoJuIdle";
            for (int i = 0; i < 65 && seen.Count < 4; i++) {
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.States.Current, Is.EqualTo(PlayerState.Free));
                Assert.That(art.sprite.name, Does.StartWith(prefix));
                Assert.That(art.sprite.pivot.y, Is.Zero);
                Assert.That(art.transform.localScale, Is.EqualTo(scale));
                seen.Add(art.sprite.name);
            }
            Assert.That(seen.Count, Is.EqualTo(4));
            Assert.That(body.position.x, Is.EqualTo(x).Within(.001f));
            float previous = Time.timeScale;
            Time.timeScale = 0;
            try {
                yield return null;
                var frozen = art.sprite;
                yield return new WaitForSecondsRealtime(.32f);
                Assert.That(art.sprite, Is.SameAs(frozen));
            } finally { Time.timeScale = previous; }
        }
    }
}
}
