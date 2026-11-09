using NUnit.Framework;
using UnityEngine;
namespace OrangeCat.Tests {
public class StateAndDefinitionTests {
    [Test] public void RollWindowHasExactBoundaries() {
        var data = ScriptableObject.CreateInstance<CharacterDefinition>();
        try {
            Assert.That(data.IsRollInvincible(.099f),Is.False);
            Assert.That(data.IsRollInvincible(.10f),Is.True);
            Assert.That(data.IsRollInvincible(.299f),Is.True);
            Assert.That(data.IsRollInvincible(.30f),Is.False);
            Assert.That(data.RollDuration,Is.EqualTo(.42f)); Assert.That(data.RollSpeed,Is.EqualTo(9));
            Assert.That(data.CoyoteTime,Is.EqualTo(.10f)); Assert.That(data.JumpBuffer,Is.EqualTo(.12f));
        } finally { Object.DestroyImmediate(data); }
    }
    [Test] public void DeadIsTerminalAndRollCannotBeCancelledIntoAttack() {
        var fsm = new PlayerStateMachine(); Assert.That(fsm.TryEnter(PlayerState.Rolling),Is.True);
        fsm.Tick(.12f); Assert.That(fsm.Elapsed,Is.EqualTo(.12f));
        Assert.That(fsm.TryEnter(PlayerState.Attacking),Is.False);
        Assert.That(fsm.TryEnter(PlayerState.Hurt),Is.True); Assert.That(fsm.Elapsed,Is.Zero);
        Assert.That(fsm.TryEnter(PlayerState.Dead),Is.True); Assert.That(fsm.TryEnter(PlayerState.Free),Is.False);
    }
}
}
