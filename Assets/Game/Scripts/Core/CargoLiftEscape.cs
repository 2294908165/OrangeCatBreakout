using UnityEngine;

namespace OrangeCat {
public enum CargoLiftPhase { Waiting, Descending, Boarding, Departing, Escaped }

[DefaultExecutionOrder(10)]
public sealed class CargoLiftEscape : MonoBehaviour {
    [SerializeField] LiftPlatform2D platform;
    [SerializeField] SpriteRenderer callLamp;
    [SerializeField, Min(.1f)] float callRadius = 1.45f;
    [SerializeField, Min(.02f)] float boardingDelay = .36f;
    [SerializeField, Min(.02f)] float topGraceTime = .90f;
    PlayerMotor2D player;
    Health life;
    float boardingTime, timeAtTop;

    public CargoLiftPhase Phase { get; private set; }
    public bool Escaped => Phase == CargoLiftPhase.Escaped;
    public LiftPlatform2D Platform => platform;

    public void Configure(LiftPlatform2D lift, SpriteRenderer indicator) {
        platform = lift;
        callLamp = indicator;
    }

    public void Track(PlayerMotor2D activePlayer) {
        player = activePlayer;
        life = activePlayer.GetComponent<Health>();
    }

    public bool CanCall => Phase == CargoLiftPhase.Waiting && player != null && !life.IsDead &&
        ((Vector2)(player.transform.position - transform.position)).sqrMagnitude <= callRadius * callRadius;

    public void TryCall() {
        if (!CanCall) return;
        platform.GoTo(false);
        Phase = CargoLiftPhase.Descending;
    }

    void FixedUpdate() {
        bool riding = player != null && !life.IsDead && player.Grounded && player.SupportingLift == platform;
        switch (Phase) {
            case CargoLiftPhase.Descending:
                if (platform.AtBottom) Phase = CargoLiftPhase.Boarding;
                break;
            case CargoLiftPhase.Boarding:
                boardingTime = riding ? boardingTime + Time.fixedDeltaTime : 0;
                if (boardingTime < boardingDelay) break;
                boardingTime = 0;
                platform.GoTo(true);
                Phase = CargoLiftPhase.Departing;
                break;
            case CargoLiftPhase.Departing:
                if (!platform.AtTop) break;
                if (riding) {
                    Phase = CargoLiftPhase.Escaped;
                    player.GetComponent<PlayerInputReader>().enabled = false;
                } else if ((timeAtTop += Time.fixedDeltaTime) >= topGraceTime) {
                    timeAtTop = 0;
                    platform.GoTo(false);
                    Phase = CargoLiftPhase.Descending;
                }
                break;
        }
    }

    void Update() {
        callLamp.color = Phase == CargoLiftPhase.Waiting
            ? new Color(1, .75f + Mathf.PingPong(Time.time * .8f, .25f), .60f)
            : new Color(.58f, 1, .76f);
    }
}
}
