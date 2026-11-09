using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class HissSkillBuilder {
    const string Art = "Assets/Game/Art/PixelPreview/";
    const string Sound = "Assets/Game/Art/Hiss.wav";
    const string HaoData = "Assets/Game/Data/Combat/HaoJu.asset";
    const string Prefabs = "Assets/Game/Prefabs/";

    [MenuItem("Orange Cat/Update HaoJu Hiss")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before updating the hiss skill.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var data = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(HaoData);
        if (data == null) throw new InvalidOperationException("Build CombatTest first.");
        BuildPose();
        BuildWave();
        ConfigureAttack(data);

        foreach (var name in new[] { "HaoJuCombat", "HaoJuPixel" }) {
            var path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                ConfigurePlayer(root, data.HissAttack);
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Could not save " + path);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var name in new[] { "CombatTest", "PixelPreview" }) {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/" + name + ".unity");
            var camera = Camera.main;
            if (camera.GetComponent<AudioListener>() == null) {
                camera.gameObject.AddComponent<AudioListener>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("HaoJu Hiss ready in CombatTest and PixelPreview.");
    }

    internal static void ConfigureAttack(CharacterDefinition data) {
        if (data.HissAttack != null) return;
        var attack = CombatTestBuilder.Attack("HaoJuHiss", "Hiss", 1, .10f, .12f, .24f, 2.8f,
            1.5f, .98f, DamageType.Sonic, height: .72f, verticalOffset: .23f);
        var fields = new SerializedObject(data);
        fields.FindProperty("hissAttack").objectReferenceValue = attack;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void ConfigurePlayer(GameObject root, AttackDefinition attack) {
        var waveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "HissWave.png");
        if (waveSprite == null) return;
        var existing = root.transform.Find("Hiss sound wave");
        var wave = existing != null ? existing.GetComponent<SpriteRenderer>()
            : new GameObject("Hiss sound wave").AddComponent<SpriteRenderer>();
        wave.transform.SetParent(root.transform, false);
        wave.sprite = waveSprite;
        wave.sortingOrder = 4;
        wave.enabled = false;
        if (!root.TryGetComponent<AudioSource>(out var audio))
            audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        if (!root.TryGetComponent<HissPresentation>(out var feedback))
            feedback = root.AddComponent<HissPresentation>();
        feedback.Configure(attack, wave, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "HaoJuHiss.png"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Sound));
    }

    static void BuildPose() {
        var image = PixelPreviewBuilder.ReadSource("HaoJuHiss");
        try {
            var pixels = image.GetPixels32();
            var bounds = PixelPreviewBuilder.OpaqueBounds(pixels, image.width,
                new RectInt(0, 0, image.width, image.height), 192);
            const int height = 42;
            int width = Mathf.RoundToInt(bounds.width * (float)height / bounds.height);
            var sprite = PixelPreviewBuilder.Resample(pixels, image.width, bounds, width, height, 192);
            PixelPreviewBuilder.SaveSprite("HaoJuHiss", sprite, width, height, new Vector2(.56f, 0));
        } finally { UnityEngine.Object.DestroyImmediate(image); }
    }

    static void BuildWave() {
        const int width = 48, height = 24;
        var pixels = new Color32[width * height];
        for (int arc = 0; arc < 3; arc++) {
            int radius = 5 + arc * 3;
            for (int y = -radius; y <= radius; y++) {
                float relative = y / (float)radius;
                int x = 3 + arc * 12 + Mathf.RoundToInt(7 * (1 - relative * relative));
                int index = (y + height / 2) * width + x;
                pixels[index] = new Color32(91, 191, 227, 255);
                pixels[index + 1] = new Color32(211, 246, 251, 255);
            }
        }
        PixelPreviewBuilder.SaveSprite("HissWave", pixels, width, height, new Vector2(.5f, .5f));
    }
}
}
