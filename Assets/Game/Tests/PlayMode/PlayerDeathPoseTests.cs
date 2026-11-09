using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class PlayerDeathPoseTests {
    [UnityTest]
    public IEnumerator BothCatsCollapseAndOnlyLieDownOnTheFloor() {
        foreach (bool airborne in new[] { false, true }) {
            yield return SceneManager.LoadSceneAsync("Warehouse01");
            var stage = Object.FindFirstObjectByType<WarehouseRun>();
            stage.SelectCharacter(airborne);
            var player = stage.Player;
            var body = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<BoxCollider2D>();
            var renderer = player.GetComponentInChildren<SpriteRenderer>();
            if (airborne) {
                body.position = new Vector2(body.position.x, 3);
                Physics2D.SyncTransforms();
            }
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(player.Grounded, Is.EqualTo(!airborne));
            var life = player.GetComponent<Health>();
            life.TakeDamage(new DamageInfo(stage.Rat.gameObject, life.MaxHealth, body.position,
                Vector2.left, 4, DamageType.RatCharge, true));
            yield return null;
            Assert.That(player.States.Current, Is.EqualTo(PlayerState.Dead));
            Assert.That(renderer.sprite.name, Is.EqualTo("DeathFall"));
            float fallHeight = renderer.sprite.bounds.size.y;
            if (airborne) {
                yield return new WaitForSeconds(.19f);
                Assert.That(player.Grounded, Is.False);
                Assert.That(renderer.sprite.name, Is.EqualTo("DeathFall"));
            }
            for (int i = 0; i < 65 && renderer.sprite.name != "DeathDown"; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.Grounded, Is.True);
            Assert.That(collider.enabled, Is.True);
            Assert.That(renderer.sprite.name, Is.EqualTo("DeathDown"));
            Assert.That(renderer.sprite.bounds.size.y, Is.LessThan(fallHeight));
            yield return new WaitForSeconds(.15f);
            Assert.That(renderer.sprite.name, Is.EqualTo("DeathDown"));
        }
    }
}
}
