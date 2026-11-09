using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(AudioSource))]
public sealed class ClawSwingAudio : MonoBehaviour {
    [SerializeField] PlayerMotor2D owner;
    [SerializeField] AudioClip sweep, rise, drop;
    AttackHitbox2D weapon;
    AudioSource source;

    public void Configure(PlayerMotor2D player, AudioClip lowSweep, AudioClip risingClaw, AudioClip twinDrop) {
        owner = player;
        sweep = lowSweep;
        rise = risingClaw;
        drop = twinDrop;
    }

    void Awake() {
        weapon = owner.GetComponent<AttackHitbox2D>();
        source = GetComponent<AudioSource>();
    }

    void OnEnable() => weapon.BecameActive += PlaySwing;

    void OnDisable() {
        weapon.BecameActive -= PlaySwing;
        source.Stop();
    }

    void PlaySwing(AttackDefinition attack) {
        var data = owner.Definition;
        var clip = attack == data.PrimaryAttack ? sweep : attack == data.ComboAttack ? rise
            : attack == data.FinisherAttack ? drop : null;
        if (clip == null || Time.timeScale <= 0) return;
        source.clip = clip;
        source.Play();
    }
}
}
