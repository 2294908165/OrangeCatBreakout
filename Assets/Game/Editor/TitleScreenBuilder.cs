using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrangeCat.Editor {
public static class TitleScreenBuilder {
    internal const string ScenePath = "Assets/Game/Scenes/TitleScreen.unity";
    const string Art = "Assets/Game/Art/PixelPreview/Title/";

    [MenuItem("Orange Cat/Build Title Screen")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the title screen.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/TitleDance.mp3")
            ?? throw new FileNotFoundException("Missing title music.");
        var background = ImportBackdrop();
        var poses = ImportDance();
        var layout = Texture("TitleType");
        var start = Texture("StartLabel");
        var on = Texture("MusicOn");
        var off = Texture("MusicOff");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var view = new GameObject("Title camera").AddComponent<Camera>();
        view.gameObject.tag = "MainCamera";
        view.transform.position = new Vector3(0, 0, -10);
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color32(10, 14, 25, 255);
        view.orthographic = true;
        view.orthographicSize = 4.5f;
        view.gameObject.AddComponent<AudioListener>();
        var backdrop = Draw("Rainy loading dock", background, Vector3.zero, -10);
        backdrop.transform.localScale = new Vector3(16 / background.bounds.size.x, 9 / background.bounds.size.y, 1);
        var hao = Draw("HaoJu dance", poses[0], new Vector3(2.1f, -2.43f, 0), 1);
        var jiang = Draw("JiangZhua dance", poses[4], new Vector3(5.65f, -2.43f, 0), 1);

        var music = new GameObject("Title BGM").AddComponent<AudioSource>();
        music.clip = clip;
        music.playOnAwake = true;
        music.loop = true;
        music.volume = .42f;
        var root = new GameObject("Title menu");
        root.AddComponent<TitleScreen>().Configure(music, layout, start, on, off);
        root.AddComponent<TitleDance>().Configure(music, view, hao, jiang, poses.Take(4).ToArray(), poses.Skip(4).ToArray());

        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save the title scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(item => item.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
    }

    static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name + ".png")
        ?? throw new FileNotFoundException("Missing title artwork " + name);

    static SpriteRenderer Draw(string name, Sprite sprite, Vector3 position, int order) {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.transform.position = position;
        renderer.sortingOrder = order;
        return renderer;
    }

    static Sprite ImportBackdrop() {
        var source = PixelPreviewBuilder.ReadSource("TitleBackdrop");
        try {
            var pixels = PixelPreviewBuilder.Resample(source.GetPixels32(), source.width,
                new RectInt(0, 0, source.width, source.height), 640, 360);
            return PixelPreviewBuilder.SaveSprite("Title/Dock", pixels, 640, 360, new Vector2(.5f, .5f));
        } finally { Object.DestroyImmediate(source); }
    }

    static Sprite[] ImportDance() {
        var source = PixelPreviewBuilder.ReadSource("TitleDance");
        try {
            const int alpha = 192;
            var pixels = source.GetPixels32();
            var frames = new Sprite[8];
            int split = Mathf.RoundToInt(source.height * .52f);
            for (int row = 0; row < 2; row++) {
                float scale = 0;
                int y = row == 0 ? split : 0, height = row == 0 ? source.height - split : split;
                for (int i = 0; i < 4; i++) {
                    int x = i * source.width / 4;
                    var crop = PixelPreviewBuilder.OpaqueBounds(pixels, source.width,
                        new RectInt(x, y, (i + 1) * source.width / 4 - x, height), alpha);
                    if (i == 0) scale = (row == 0 ? 101f : 112f) / crop.height;
                    int w = Mathf.RoundToInt(crop.width * scale), h = Mathf.RoundToInt(crop.height * scale);
                    var art = PixelPreviewBuilder.Resample(pixels, source.width, crop, w, h, alpha);
                    float anchor = PixelPreviewBuilder.FeetCenter(art, w, h);
                    frames[row * 4 + i] = PixelPreviewBuilder.SaveSprite($"Title/{(row == 0 ? "Hao" : "Jiang")}{i}",
                        art, w, h, new Vector2(anchor, 0));
                }
            }
            return frames;
        } finally { Object.DestroyImmediate(source); }
    }
}
}
