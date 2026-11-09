using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OrangeCat.Editor {
internal static class BellyLaughBuilder {
    const string PixelArt = "Assets/Game/Art/PixelPreview";
    const string Parent = PixelArt + "/LoadingBay";
    const string Prefab = "Assets/Game/Prefabs/BellyLaugh.prefab";
    const string LaughSound = "Assets/Game/Audio/BellyLaugh.wav";
    const float BodyHeight = 1.88f;

    internal static GameObject BuildPrefab() {
        if (!AssetDatabase.IsValidFolder(Parent + "/BellyLaugh")) AssetDatabase.CreateFolder(Parent, "BellyLaugh");
        var idle = Import("BellyLaughProfileIdle", new[] { "IdleHandsOnBelly" }, 1, 1, 64)[0];
        var guard = Import("BellyLaughStances", new[] { "Guard" }, 1, 1, 59, leftTrim: .43f)[0];
        var reactions = Import("BellyLaughReactions", new[] { "Idle", "HitRecoil", "LaughLow", "LaughHigh" }, 2, 2, 64);
        var actions = Import("BellyLaughActions", new[] { "BumpReady", "BumpActive", "Defeated" }, 3, 1, 61);
        var laughClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LaughSound)
            ?? throw new FileNotFoundException("Missing Belly Laugh sound.", LaughSound);
        var attack = CombatTestBuilder.Attack("BellyLaughBump", "Belly Bump", 1,
            .40f, .18f, .54f, 3.3f, 1.47f, .67f, DamageType.BellyBump, height: 1.16f, verticalOffset: -.23f);

        var root = new GameObject("Belly Laugh") { layer = LayerMask.NameToLayer("Enemy") };
        try {
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.freezeRotation = true;
            root.AddComponent<BoxCollider2D>().size = new Vector2(.98f, BodyHeight);
            var life = root.AddComponent<Health>();
            life.Configure(8);

            var hurtbox = new GameObject("Belly Laugh hurtbox") { layer = LayerMask.NameToLayer("EnemyHurtbox") };
            hurtbox.transform.SetParent(root.transform, false);
            var hit = hurtbox.AddComponent<BoxCollider2D>();
            hit.isTrigger = true;
            hit.size = new Vector2(1.31f, BodyHeight);
            hurtbox.AddComponent<Hurtbox2D>().Configure(life);
            root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), attack, -1);
            root.AddComponent<BellyLaughBrain2D>().Configure(attack, LayerMask.GetMask("Ground"));

            var visual = Draw(root.transform, "Belly Laugh visual", idle, 3);
            visual.transform.localPosition = Vector3.down * (BodyHeight * .5f);
            var warning = Draw(root.transform, "Belly bump warning", Load("Warning"), 2);
            warning.transform.localScale = new Vector3(1.75f / warning.sprite.bounds.size.x, .14f / warning.sprite.bounds.size.y, 1);
            warning.enabled = false;
            var spark = Draw(root.transform, "Belly hit spark", Load("HitSpark"), 5);
            spark.enabled = false;
            root.AddComponent<AudioSource>().playOnAwake = false;
            var voice = new GameObject("Belly Laugh voice").AddComponent<AudioSource>();
            voice.transform.SetParent(root.transform, false);
            voice.playOnAwake = false;
            voice.clip = laughClip;
            voice.volume = .65f;
            root.AddComponent<BellyLaughPresentation>().Configure(visual, warning, spark, idle, guard, reactions, actions,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Art/RatHit.wav"), voice);
            return PrefabUtility.SaveAsPrefabAsset(root, Prefab)
                ?? throw new InvalidOperationException("Could not save the Belly Laugh prefab.");
        } finally { Object.DestroyImmediate(root); }
    }

    internal static BellyLaughBrain2D Place(GameObject prefab, Scene scene) {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.position = new Vector3(-.70f, -.5f + BodyHeight * .5f, 0);
        return instance.GetComponent<BellyLaughBrain2D>();
    }

    static SpriteRenderer Draw(Transform parent, string name, Sprite sprite, int order) {
        var art = new GameObject(name).AddComponent<SpriteRenderer>();
        art.transform.SetParent(parent, false);
        art.sprite = sprite;
        art.sortingOrder = order;
        return art;
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt}/Warehouse/{name}.png")
        ?? throw new FileNotFoundException($"Missing warehouse sprite {name}.");

    static Sprite[] Import(string sourceName, string[] names, int columns, int rows, int firstHeight, float leftTrim = 0) {
        const int alpha = 192;
        var source = PixelPreviewBuilder.ReadSource(sourceName);
        try {
            var pixels = source.GetPixels32();
            var bounds = new RectInt[names.Length];
            int firstX = Mathf.RoundToInt(source.width * leftTrim);
            int stripWidth = source.width - firstX;
            for (int i = 0; i < names.Length; i++) {
                int column = i % columns, row = rows - 1 - i / columns;
                int x = firstX + column * stripWidth / columns, y = row * source.height / rows;
                var cell = new RectInt(x, y, firstX + (column + 1) * stripWidth / columns - x,
                    (row + 1) * source.height / rows - y);
                bounds[i] = PixelPreviewBuilder.OpaqueBounds(pixels, source.width, cell, alpha);
                if (bounds[i].height < cell.height / 4)
                    throw new InvalidOperationException($"Missing pose {names[i]} in {sourceName}.");
            }
            float scale = firstHeight / (float)bounds[0].height;
            return names.Select((name, i) => {
                var crop = bounds[i];
                int w = Mathf.RoundToInt(crop.width * scale), h = Mathf.RoundToInt(crop.height * scale);
                var art = PixelPreviewBuilder.Resample(pixels, source.width, crop, w, h, alpha);
                float pivot = name == "Defeated" ? .5f : PixelPreviewBuilder.FeetCenter(art, w, h);
                return PixelPreviewBuilder.SaveSprite("LoadingBay/BellyLaugh/" + name, art, w, h, new Vector2(pivot, 0));
            }).ToArray();
        } finally { Object.DestroyImmediate(source); }
    }
}
}
