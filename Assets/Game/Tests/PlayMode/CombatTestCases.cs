using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class CombatTestCases {
    const float Dt = .02f;
    CombatTestSession session;
    PlayerMotor2D Player => session.Player;
    PlayerCombat Combat => Player.GetComponent<PlayerCombat>();
    Health HP => Player.GetComponent<Health>();
    Health Dummy => session.Dummy;
    Health Source => session.Source;
    AttackHitbox2D Weapon => Combat.Weapon;

    [UnitySetUp]
    public IEnumerator LoadCombatTest() {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("CombatTest");
        session = Object.FindFirstObjectByType<CombatTestSession>();
        Assert.That(session, Is.Not.Null);
        // Allow a real physics step, then use the same production fixed tick at exact times.
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Time.timeScale = 0;
        PlaceDummy(1);
    }

    [UnityTearDown]
    public IEnumerator RestoreClock() {
        Time.timeScale = 1;
        yield return null;
    }

    void Step(int ticks = 1, bool attack = false, bool guard = false, bool roll = false, float move = 0) {
        for (int i = 0; i < ticks; i++)
            Player.Simulate(new PlayerCommand(move, false, false, i == 0 && roll, i == 0 && attack, guard), Dt);
    }

    void PlaceDummy(float x) {
        Dummy.transform.position = Player.transform.position + Vector3.right * x;
        Physics2D.SyncTransforms();
    }

    DamageInfo Incoming(bool front = true, float amount = 2, bool blockable = true) {
        Source.transform.position = Player.transform.position + Vector3.right * (front ? 1 : -1);
        Physics2D.SyncTransforms();
        return new DamageInfo(Source.gameObject, amount, Player.transform.position,
            front ? Vector2.left : Vector2.right, 3, DamageType.Training, blockable);
    }

    void StartGuard() {
        session.SwitchCharacter(true);
        Physics2D.SyncTransforms();
        Step(guard: true);
        Assert.That(Combat.IsGuarding, Is.True);
    }

    void AssertHP(float value) => Assert.That(HP.Current, Is.EqualTo(value).Within(.0001f));

    [Test]
    public void T01_OneAttackOnlyHitsOnce() {
        Step(attack: true);
        long id = Weapon.InstanceId;
        Step(7);
        Assert.That(id, Is.Positive);
        Assert.That(Weapon.InstanceId, Is.EqualTo(id));
        Assert.That(Weapon.HitCount, Is.EqualTo(1));
        Step(15);
        Assert.That(Dummy.Current, Is.EqualTo(18));
        Assert.That(Weapon.IsRunning, Is.False);
    }

    [Test]
    public void T02_OneAttackHitsTwoTargets() {
        var other = Object.Instantiate(Dummy, Dummy.transform.position + Vector3.right * .25f, Quaternion.identity);
        try {
            Physics2D.SyncTransforms();
            Step(attack: true);
            Step(5);
            Assert.That(Weapon.HitCount, Is.EqualTo(2));
            Assert.That(Dummy.Current, Is.EqualTo(18));
            Assert.That(other.Current, Is.EqualTo(18));
        } finally { Object.Destroy(other.gameObject); }
    }

    [Test]
    public void T03_OverlappingHurtboxesShareOneHealth() {
        Assert.That(Dummy.GetComponentsInChildren<Hurtbox2D>().Length, Is.EqualTo(2));
        Step(attack: true);
        Step(6);
        Assert.That(Weapon.HitCount, Is.EqualTo(1));
        Assert.That(Dummy.Current, Is.EqualTo(18));
    }

    [Test]
    public void T04_OnlyActiveWindowCanHit() {
        Step(attack: true);
        Step(3);
        Assert.That(Weapon.Stage, Is.EqualTo(AttackStage.Startup));
        Assert.That(Dummy.Current, Is.EqualTo(20));
        PlaceDummy(4);
        Step(6);
        Assert.That(Weapon.Stage, Is.EqualTo(AttackStage.Recovery));
        PlaceDummy(1);
        Step(12);
        Assert.That(Dummy.Current, Is.EqualTo(20));
    }

    [Test]
    public void T05_ThreeClawsUseFreshInstancesAndCannotBufferAFourth() {
        Step(attack: true);
        long first = Weapon.InstanceId;
        Step(8);
        Step(attack: true);
        Step(8);
        Assert.That(Weapon.InstanceId, Is.GreaterThan(first));
        long second = Weapon.InstanceId;
        Assert.That(Weapon.Definition, Is.EqualTo(Player.Definition.ComboAttack));
        Step(7);
        Assert.That(Dummy.Current, Is.EqualTo(16));
        Step(attack: true);
        Step(12);
        Assert.That(Weapon.InstanceId, Is.GreaterThan(second));
        Assert.That(Weapon.Definition, Is.EqualTo(Player.Definition.FinisherAttack));
        Step(7);
        Assert.That(Dummy.Current, Is.EqualTo(13));
        Step(attack: true);
        Step(25);
        Assert.That(Weapon.IsRunning, Is.False);
        Assert.That(Dummy.Current, Is.EqualTo(13));
    }

    [Test]
    public void T06_SinglePressDoesNotAutoCombo() {
        Step(attack: true);
        Step(40);
        Assert.That(Dummy.Current, Is.EqualTo(18));
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Free));
    }

    [Test]
    public void T07_RollCancelOnlyInLateRecovery() {
        Step(attack: true);
        Step(roll: true);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Attacking));
        Step(12);
        Assert.That(Combat.CanRollCancel, Is.True);
        Step(roll: true);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Rolling));
        Assert.That(Weapon.InstanceId, Is.Zero);
        Step(23);
        Assert.That(Dummy.Current, Is.EqualTo(18));
    }

    [Test]
    public void T08_RollIFramesIgnoreDamage() {
        Step(roll: true);
        Step(6);
        Assert.That(Combat.IsInvulnerable, Is.True);
        Assert.That(HP.TakeDamage(Incoming()), Is.EqualTo(DamageOutcome.Invulnerable));
        AssertHP(4);
        Assert.That(Player.GetComponent<BoxCollider2D>().enabled, Is.True);
    }

    [Test]
    public void T09_RollStartupTakesDamage() {
        Step(roll: true);
        Step(3);
        Assert.That(HP.TakeDamage(Incoming()), Is.EqualTo(DamageOutcome.Hit));
        AssertHP(2);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void T10_FrontNormalGuardKeepsFractionalHealth() {
        StartGuard();
        Step(9, guard: true);
        Assert.That(HP.TakeDamage(Incoming(amount: 1)), Is.EqualTo(DamageOutcome.Blocked));
        AssertHP(5.7f);
        Assert.That(Combat.IsGuarding, Is.True);
    }

    [UnityTest]
    public IEnumerator T11_RearAttackBypassesGuard() {
        StartGuard();
        Assert.That(HP.TakeDamage(Incoming(false)), Is.EqualTo(DamageOutcome.Hit));
        AssertHP(4);
        Step(12, guard: true);
        Assert.That(Combat.IsGuarding, Is.False);
        // Integrate the knockback before testing a fresh ground guard.
        var body = Player.GetComponent<Rigidbody2D>();
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Ground") };
        var hits = new RaycastHit2D[4];
        var collider = Player.GetComponent<BoxCollider2D>();
        var previousMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        try {
            for (int i = 0; i < 50; i++) {
                Step(guard: true);
                Physics2D.Simulate(Dt);
                if (body.linearVelocity.y <= .1f && collider.Cast(Vector2.down, filter, hits, .06f) > 0)
                    break;
            }
        } finally { Physics2D.simulationMode = previousMode; }
        Assert.That(Combat.IsGuarding, Is.False);
        Step();
        Step(guard: true);
        Assert.That(Combat.IsPerfectGuardWindow, Is.True);
        yield break;
    }

    [Test]
    public void T12_TrainingStrikeTriggersPerfectGuardAndStagger() {
        StartGuard();
        Incoming();
        var source = Source.GetComponent<TrainingAttackSource>();
        for (int i = 0; i < 30; i++) source.Step(Dt);
        AssertHP(6);
        Assert.That(source.IsStaggered, Is.True);
        Assert.That(source.Stage, Is.EqualTo(AttackStage.Idle));
        Assert.That(Combat.CounterRemaining, Is.EqualTo(.8f).Within(.001f));
    }

    [Test]
    public void T13_HeldGuardDoesNotRefreshPerfectWindow() {
        StartGuard();
        Step(8, guard: true);
        Assert.That(Combat.IsPerfectGuardWindow, Is.False);
        Assert.That(HP.TakeDamage(Incoming()), Is.EqualTo(DamageOutcome.Blocked));
        AssertHP(5.4f);
        Assert.That(Combat.CanCounterAttack, Is.False);
    }

    [Test]
    public void T14_CounterConsumesWindowAndDealsThree() {
        StartGuard();
        PlaceDummy(1);
        Assert.That(HP.TakeDamage(Incoming()), Is.EqualTo(DamageOutcome.PerfectGuard));
        Step(attack: true, guard: true);
        Assert.That(Combat.CanCounterAttack, Is.False);
        Assert.That(Combat.IsGuarding, Is.False);
        Assert.That(Weapon.Definition, Is.EqualTo(Player.Definition.CounterAttack));
        Step(7, guard: true);
        Assert.That(Dummy.Current, Is.EqualTo(17));
    }

    [Test]
    public void T15_ExpiredCounterFallsBackToNormalAttack() {
        StartGuard();
        PlaceDummy(1);
        HP.TakeDamage(Incoming());
        Step(41);
        Assert.That(Combat.CanCounterAttack, Is.False);
        Step(attack: true);
        Step(8);
        Assert.That(Dummy.Current, Is.EqualTo(19));
    }

    [Test]
    public void T16_DeathIsTerminalAndRaisedOnce() {
        int deaths = 0;
        HP.Died += () => deaths++;
        var lethal = Incoming(amount: 10);
        HP.TakeDamage(lethal);
        HP.TakeDamage(lethal);
        HP.Heal(3);
        AssertHP(0);
        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Dead));
        Step(attack: true, roll: true, move: 1);
        Assert.That(Player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.Zero);
        Assert.That(Weapon.IsRunning, Is.False);
    }

    [Test]
    public void T17_DeathCancelsInFlightAttack() {
        Step(attack: true);
        Assert.That(Weapon.IsRunning, Is.True);
        HP.TakeDamage(Incoming(amount: 10));
        Assert.That(Weapon.IsRunning, Is.False);
        Step(25);
        Assert.That(Dummy.Current, Is.EqualTo(20));
    }

    [Test]
    public void T18_SwitchCharacterRebuildsCombatAndInput() {
        StartGuard();
        HP.TakeDamage(Incoming());
        var old = Player;
        session.SwitchCharacter(false);
        Assert.That(old.gameObject.activeSelf, Is.False);
        Assert.That(Player.Definition.SupportsGuard, Is.False);
        Assert.That(Player.Definition.ComboAttack, Is.Not.Null);
        AssertHP(4);
        Assert.That(Combat.CanCounterAttack, Is.False);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Free));
        Step(guard: true);
        Assert.That(Combat.IsGuarding, Is.False);
        session.SwitchCharacter(true);
        AssertHP(6);
        Assert.That(Player.Definition.ComboAttack, Is.Null);
        Assert.That(Player.GetComponent<PlayerInputReader>().Actions.enabled, Is.True);
    }

    [Test]
    public void T19_HurtInterruptsThenRestoresMovementAndAttack() {
        Step(attack: true);
        HP.TakeDamage(Incoming());
        Assert.That(Weapon.IsRunning, Is.False);
        Step(8, attack: true);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Hurt));
        Step(move: 1);
        Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Free));
        Assert.That(Combat.IsInvulnerable, Is.True);
        Step(attack: true);
        Assert.That(Weapon.IsRunning, Is.True);
        Step(27);
        Assert.That(Combat.IsInvulnerable, Is.False);
    }

    [UnityTest]
    public IEnumerator T20_PauseFreezesAttackAndReloadClearsState() {
        Step(attack: true);
        var manager = Object.FindFirstObjectByType<GameManager>();
        manager.SetPaused(true);
        float elapsed = Weapon.Elapsed;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(Weapon.Elapsed, Is.EqualTo(elapsed));
        Assert.That(Dummy.Current, Is.EqualTo(20));
        session.Restart();
        yield return null;
        yield return null;
        session = Object.FindFirstObjectByType<CombatTestSession>();
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(Weapon.IsRunning, Is.False);
        Assert.That(Combat.IsInvulnerable, Is.False);
        AssertHP(4);
    }

    [Test]
    public void T21_PhysicsStepCanCrossEntireActiveWindow() {
        Step(attack: true);
        Weapon.Step(.30f);
        Assert.That(Dummy.Current, Is.EqualTo(18));
        Assert.That(Weapon.Stage, Is.EqualTo(AttackStage.Recovery));
    }

    [Test]
    public void T22_LeftFacingAttackAndDeadDummyReset() {
        PlaceDummy(-1);
        Step(attack: true, move: -1);
        Step(6);
        Assert.That(Dummy.Current, Is.EqualTo(18));
        Dummy.TakeDamage(new DamageInfo(Player.gameObject, 20, Dummy.transform.position,
            Vector2.left, 0, DamageType.Claw, true));
        Assert.That(Dummy.IsDead, Is.True);
        Step(18);
        Step(attack: true, move: -1);
        Step(6);
        Assert.That(Weapon.HitCount, Is.Zero);
        Dummy.ResetTarget();
        Assert.That(Dummy.Current, Is.EqualTo(20));
    }

    [UnityTest]
    public IEnumerator T23_KeyboardAttackUsesRealUpdateAndFixedUpdate() {
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var editorRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        // The batch Editor has no focused Game view.
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            Time.timeScale = 1;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J));
            for (int i = 0; i < 30 && Dummy.Current == 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(keyboard.jKey.isPressed, Is.True, "The injected J event must reach the virtual keyboard.");
            Assert.That(Dummy.Current, Is.EqualTo(18));
            // Holding the original key press must not buffer a second strike.
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            Assert.That(Dummy.Current, Is.EqualTo(18));
            Assert.That(Player.States.Current, Is.EqualTo(PlayerState.Free));
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorRouting;
#endif
        }
    }
}
}
