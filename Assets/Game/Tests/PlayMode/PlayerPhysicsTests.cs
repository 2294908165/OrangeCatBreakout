using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OrangeCat.Tests {
public class PlayerPhysicsTests {
    [UnityTest] public IEnumerator GeneratedSceneSupportsJumpAndRollWithoutDisablingBody() {
        yield return SceneManager.LoadSceneAsync("PhaseOne");
        var motor = Object.FindFirstObjectByType<PlayerMotor2D>(); Assert.That(motor,Is.Not.Null);
        // Disable automatic input tick by pausing scaled time; use the production simulation seam.
        for(int i=0;i<80;i++) yield return new WaitForFixedUpdate();
        Assert.That(motor.Grounded,Is.True);
        Time.timeScale = 0;
        try {
            var collider = motor.GetComponent<BoxCollider2D>();
            motor.Simulate(new PlayerCommand(0,false,false,true),.02f);
            Assert.That(motor.States.Current,Is.EqualTo(PlayerState.Rolling));
            for(int i=0;i<6;i++) motor.Simulate(new PlayerCommand(),.02f);
            Assert.That(motor.Invincible,Is.True); Assert.That(collider.enabled,Is.True);
            Assert.That(Mathf.Abs(motor.GetComponent<Rigidbody2D>().linearVelocity.x),Is.EqualTo(9).Within(.01f));
            for(int i=0;i<16;i++) motor.Simulate(new PlayerCommand(),.02f);
            Assert.That(motor.States.Current,Is.EqualTo(PlayerState.Free)); Assert.That(motor.Invincible,Is.False);
            motor.Simulate(new PlayerCommand(0,true,true,false),.02f);
            Assert.That(motor.JumpsUsed,Is.EqualTo(1));
            Assert.That(motor.GetComponent<Rigidbody2D>().linearVelocity.y,Is.GreaterThan(0));
            motor.Simulate(new PlayerCommand(0,true,true,false),.02f);
            Assert.That(motor.JumpsUsed,Is.EqualTo(2));
            motor.Simulate(new PlayerCommand(0,true,true,false),.02f);
            Assert.That(motor.JumpsUsed,Is.EqualTo(2));
        } finally { Time.timeScale = 1; }
    }
}
}
