using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class PixelMotionTests {
    [UnityTest]
    public IEnumerator HaoJuPosesFollowRunRollAndAllThreeClawWindows() {
        yield return SceneManager.LoadSceneAsync("PixelPreview");
        var session = Object.FindFirstObjectByType<CombatTestSession>();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var motor = session.Player;
        var sprite = motor.GetComponentInChildren<SpriteRenderer>();
        float previous = Time.timeScale;
        Time.timeScale = 0;
        try {
            motor.Simulate(new PlayerCommand(1, false, false, false), .02f);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Run1"));
            motor.Simulate(new PlayerCommand(0, false, false, true), .02f);
            Advance(motor, 6);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Roll2"));
            Advance(motor, 16);

            var trails = motor.transform.Find("Claw trails").GetComponent<SpriteRenderer>();
            motor.Simulate(new PlayerCommand(-1, false, false, false, true), .02f);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw1Ready"));
            Assert.That(trails.enabled, Is.False);
            Advance(motor, 4);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw1Active"));
            Assert.That(trails.enabled && trails.flipX, Is.True);
            Assert.That(trails.sprite.name, Is.EqualTo("ClawSweepTrail"));

            Advance(motor, 5);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw1Recover"));
            Assert.That(trails.enabled, Is.False);
            motor.Simulate(new PlayerCommand(0, false, false, false, true), .02f);
            Advance(motor, 7);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw2Ready"));
            Advance(motor, 5);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw2Active"));
            Assert.That(trails.enabled && trails.flipX, Is.True);
            Assert.That(trails.sprite.name, Is.EqualTo("ClawRiseTrail"));
            Advance(motor, 3);
            motor.Simulate(new PlayerCommand(0, false, false, false, true), .02f);
            Advance(motor, 3);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw2Recover"));
            Assert.That(trails.enabled, Is.False);
            Advance(motor, 8);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw3Ready"));
            Assert.That(trails.enabled, Is.False);
            Advance(motor, 7);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw3Active"));
            Assert.That(trails.enabled && trails.flipX, Is.True);
            Assert.That(trails.sprite.name, Is.EqualTo("ClawDropTrail"));
            Assert.That(trails.transform.localPosition.x, Is.LessThan(0));
            Advance(motor, 6);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Claw3Recover"));
            Assert.That(trails.enabled, Is.False);
        } finally { Time.timeScale = previous; }
    }

    [UnityTest]
    public IEnumerator JiangZhuaGuardAndCounterSelectDistinctPoses() {
        yield return SceneManager.LoadSceneAsync("PixelPreview");
        var session = Object.FindFirstObjectByType<CombatTestSession>();
        session.SwitchCharacter(true);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var motor = session.Player;
        var sprite = motor.GetComponentInChildren<SpriteRenderer>();
        float previous = Time.timeScale;
        Time.timeScale = 0;
        try {
            motor.Simulate(new PlayerCommand(0, false, false, false, false, true), .02f);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("Guard"));

            session.Source.transform.position = motor.transform.position + Vector3.right;
            var damage = new DamageInfo(session.Source.gameObject, 2, motor.transform.position,
                Vector2.left, 1, DamageType.Training, true);
            Assert.That(motor.GetComponent<Health>().TakeDamage(damage), Is.EqualTo(DamageOutcome.PerfectGuard));
            motor.Simulate(new PlayerCommand(0, false, false, false, true, true), .02f);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("CounterReady"));
            Advance(motor, 5);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("CounterActive"));
            Advance(motor, 7);
            yield return null;
            Assert.That(sprite.sprite.name, Is.EqualTo("JiangZhua"));
        } finally { Time.timeScale = previous; }
    }

    static void Advance(PlayerMotor2D motor, int ticks) {
        for (int i = 0; i < ticks; i++) motor.Simulate(default, .02f);
    }
}
}
