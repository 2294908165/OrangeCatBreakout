using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class PlayerHitFeedbackTests {
    [UnityTest]
    public IEnumerator RatAndHazardHitsShowHurtPoseAndImpactOnBothCats() {
        foreach (bool useJiangZhua in new[] { false, true }) {
            yield return SceneManager.LoadSceneAsync("Warehouse01");
            var stage = Object.FindFirstObjectByType<WarehouseRun>();
            stage.SelectCharacter(useJiangZhua);
            yield return new WaitForFixedUpdate();
            var player = stage.Player;
            var life = player.GetComponent<Health>();
            var renderer = player.GetComponentInChildren<SpriteRenderer>();
            var spark = player.transform.Find("Player hit spark").GetComponent<SpriteRenderer>();
            var body = player.GetComponent<Rigidbody2D>();
            float x = useJiangZhua ? GameObject.Find("Floor spikes").transform.position.x
                : stage.Rat.GetComponent<Rigidbody2D>().position.x - 1.05f;
            body.position = new Vector2(x, .12f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.That(spark.enabled, Is.False);
            for (int i = 0; i < 50 && life.Current == life.MaxHealth; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(life.Current, Is.EqualTo(useJiangZhua ? 5 : 2));
            Assert.That(player.States.Current, Is.EqualTo(PlayerState.Hurt));
            Assert.That(renderer.sprite.name, Is.EqualTo("Hurt"));
            Assert.That(renderer.color.g, Is.LessThan(.6f));
            Assert.That(spark.enabled, Is.True);

            yield return new WaitForSeconds(.22f);
            Assert.That(renderer.sprite.name, Is.Not.EqualTo("Hurt"));
            Assert.That(renderer.color, Is.EqualTo(Color.white));
            Assert.That(spark.enabled, Is.False);
        }
    }

    [UnityTest]
    public IEnumerator GuardAndRollDoNotShowAFalseHurtSpark() {
        yield return SceneManager.LoadSceneAsync("Warehouse01");
        var stage = Object.FindFirstObjectByType<WarehouseRun>();
        stage.SelectCharacter(true);
        yield return new WaitForFixedUpdate();
        float before = Time.timeScale;
        Time.timeScale = 0;
        try {
            var player = stage.Player;
            for (int i = 0; i < 10; i++)
                player.Simulate(new PlayerCommand(0, false, false, false, guardHeld: true), .02f);
            Assert.That(player.GetComponent<Health>().TakeDamage(Charge(stage)), Is.EqualTo(DamageOutcome.Blocked));
            yield return null;
            var renderer = player.GetComponentInChildren<SpriteRenderer>();
            Assert.That(renderer.sprite.name, Is.EqualTo("Guard"));
            Assert.That(renderer.color.r, Is.LessThan(.8f));
            Assert.That(player.transform.Find("Player hit spark").GetComponent<SpriteRenderer>().enabled, Is.False);

            stage.SelectCharacter(false);
            player = stage.Player;
            Physics2D.SyncTransforms();
            player.Simulate(new PlayerCommand(0, false, false, true), .02f);
            for (int i = 0; i < 6; i++) player.Simulate(default, .02f);
            Assert.That(player.GetComponent<Health>().TakeDamage(Charge(stage)), Is.EqualTo(DamageOutcome.Invulnerable));
            yield return null;
            Assert.That(player.GetComponentInChildren<SpriteRenderer>().sprite.name, Is.EqualTo("Roll2"));
            Assert.That(player.transform.Find("Player hit spark").GetComponent<SpriteRenderer>().enabled, Is.False);
        } finally { Time.timeScale = before; }
    }

    static DamageInfo Charge(WarehouseRun stage) => new DamageInfo(stage.Rat.gameObject, 2,
        stage.Player.transform.position, Vector2.left, 4, DamageType.RatCharge, true);
}
}
