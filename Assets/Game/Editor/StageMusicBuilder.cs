using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrangeCat.Editor {
internal static class StageMusicBuilder {
    internal static void Configure(Scene scene) {
        var (file, volume) = scene.name switch {
            "Warehouse01" => ("WarehouseRain.wav", .35f),
            "ColdStorage02" => ("ColdConveyor.wav", .35f),
            "LoadingBay03" => ("LoadingBayHappy.mp3", .32f),
            "Rooftop04" => ("RooftopDawn.wav", .35f),
            _ => throw new ArgumentException($"No music configured for {scene.name}.")
        };
        string path = "Assets/Game/Audio/" + file;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path)
            ?? throw new FileNotFoundException("Missing stage music.", path);
        var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Stage BGM");
        if (root == null) {
            root = new GameObject("Stage BGM");
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        if (!root.TryGetComponent<AudioSource>(out var source)) source = root.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = true;
        source.loop = true;
        source.spatialBlend = 0;
        source.volume = volume;
    }
}
}
