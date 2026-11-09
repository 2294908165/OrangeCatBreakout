using UnityEngine;

namespace OrangeCat {
public sealed class TitleDance : MonoBehaviour {
    [SerializeField] AudioSource music;
    [SerializeField] Camera view;
    [SerializeField] SpriteRenderer haoJu, jiangZhua;
    [SerializeField] Sprite[] haoFrames, jiangFrames;
    [SerializeField, Min(.05f)] float secondsPerBeat = .6425f;
    [SerializeField] float firstBeat = .42f;
    Vector3 haoRest, jiangRest;
    int lastWidth, lastHeight;

    public void Configure(AudioSource source, Camera camera, SpriteRenderer hao, SpriteRenderer jiang,
        Sprite[] haoPoses, Sprite[] jiangPoses) {
        music = source;
        view = camera;
        haoJu = hao;
        jiangZhua = jiang;
        haoFrames = haoPoses;
        jiangFrames = jiangPoses;
    }

    void Awake() {
        haoRest = haoJu.transform.localPosition;
        jiangRest = jiangZhua.transform.localPosition;
        FitViewport();
    }

    void LateUpdate() {
        if (Screen.width != lastWidth || Screen.height != lastHeight) FitViewport();
        float seconds = music.isPlaying ? music.timeSamples / (float)music.clip.frequency : Time.unscaledTime;
        float beat = Mathf.Max(0, (seconds - firstBeat) / secondsPerBeat);
        Pose(haoJu, haoFrames, haoRest, beat, .14f);
        Pose(jiangZhua, jiangFrames, jiangRest, beat + .5f, .23f);
    }

    static void Pose(SpriteRenderer renderer, Sprite[] frames, Vector3 rest, float beat, float bounce) {
        int step = Mathf.FloorToInt(beat * 2) % 8;
        int frame = step switch { 1 or 7 => 1, 3 or 5 => 2, 4 => 3, _ => 0 };
        renderer.sprite = frames[frame];
        float rise = Mathf.Abs(Mathf.Sin(Mathf.PI * beat));
        renderer.transform.localPosition = rest + new Vector3(Mathf.Sin(beat * Mathf.PI) * .15f, rise * bounce, 0);
    }

    void FitViewport() {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        float scale = Mathf.Min(lastWidth / 1280f, lastHeight / 720f);
        float width = 1280 * scale / lastWidth, height = 720 * scale / lastHeight;
        view.rect = new Rect((1 - width) * .5f, (1 - height) * .5f, width, height);
    }
}
}
