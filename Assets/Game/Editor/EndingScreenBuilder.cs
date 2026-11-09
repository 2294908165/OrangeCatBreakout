using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class EndingScreenBuilder {
    const string ScenePath = "Assets/Game/Scenes/EndingScreen.unity";
    const string Art = "Assets/Game/Art/PixelPreview/Ending/";

    [MenuItem("Orange Cat/Build Ending Screen")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the ending.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var backdrop = Texture("Dawn");
        var artwork = Texture("EndingType");
        var replay = Texture("Replay");
        var title = Texture("Title");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/EndingDawn.wav")
            ?? throw new FileNotFoundException("Missing ending music.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Ending camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(9, 14, 23, 255);
        camera.cullingMask = 0;
        camera.gameObject.AddComponent<AudioListener>();
        var music = new GameObject("Ending BGM").AddComponent<AudioSource>();
        music.clip = clip;
        music.playOnAwake = true;
        music.loop = true;
        music.volume = .32f;
        new GameObject("Ending menu").AddComponent<EndingScreen>().Configure(backdrop, artwork, replay, title);
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save ending scene.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath)
            .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        AssetDatabase.SaveAssets();
    }

    static Texture2D Texture(string name) {
        string path = Art + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter
            ?? throw new FileNotFoundException($"Missing ending artwork {name}.", path);
        var filter = name == "Dawn" ? FilterMode.Point : FilterMode.Bilinear;
        if (importer.filterMode != filter || importer.npotScale != TextureImporterNPOTScale.None ||
            importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled) {
            importer.filterMode = filter;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
}
