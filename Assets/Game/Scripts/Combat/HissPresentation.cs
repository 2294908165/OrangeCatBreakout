using UnityEngine;

namespace OrangeCat {
[RequireComponent(typeof(AttackHitbox2D), typeof(AudioSource))]
public sealed class HissPresentation : MonoBehaviour {
    [SerializeField] AttackDefinition attack;
    [SerializeField] SpriteRenderer wave;
    [SerializeField] Sprite hissPose;
    [SerializeField] AudioClip clip;
    AttackHitbox2D weapon;
    AudioSource source;
    public Sprite HissPose => hissPose;

    public void Configure(AttackDefinition definition, SpriteRenderer effect, Sprite pose, AudioClip audioClip) {
        attack = definition;
        wave = effect;
        hissPose = pose;
        clip = audioClip;
    }

    void Awake() {
        weapon = GetComponent<AttackHitbox2D>();
        source = GetComponent<AudioSource>();
    }

    void OnEnable() { weapon.BecameActive += OnBecameActive; }

    void OnDisable() {
        weapon.BecameActive -= OnBecameActive;
        wave.enabled = false;
        source.Stop();
    }

    void OnBecameActive(AttackDefinition definition) {
        if (definition == attack && clip != null && Time.timeScale > 0)
            source.PlayOneShot(clip, .55f);
    }

    void LateUpdate() {
        bool active = weapon.Definition == attack && weapon.Stage == AttackStage.Active;
        wave.enabled = active;
        if (!active) return;

        float progress = Mathf.Clamp01((weapon.Elapsed - attack.Startup) / attack.Active);
        float width = attack.HitboxSize.x * Mathf.Lerp(.50f, 1, progress);
        float center = attack.HitboxOffset.x - (attack.HitboxSize.x - width) * .5f;
        wave.transform.localPosition = new Vector3(weapon.Facing * center, attack.HitboxOffset.y, 0);
        wave.transform.localScale = new Vector3(width / wave.sprite.bounds.size.x,
            attack.HitboxSize.y / wave.sprite.bounds.size.y, 1);
        wave.flipX = weapon.Facing < 0;
        wave.color = new Color(1, 1, 1, Mathf.Lerp(1, .55f, progress));
    }
}
}
