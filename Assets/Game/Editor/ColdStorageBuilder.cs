using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class ColdStorageBuilder {
    const string Template = "Assets/Game/Scenes/PixelPreview.unity";
    const string ScenePath = "Assets/Game/Scenes/ColdStorage02.unity";
    const string Art = "Assets/Game/Art/PixelPreview/ColdStorage";

    [MenuItem("Orange Cat/Build Cold Storage 02")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before rebuilding ColdStorage02.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Template)) throw new FileNotFoundException("Build PixelPreview first.", Template);
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Game/Art/PixelPreview", "ColdStorage");
        ImportBackdrop();
        ImportFly();
        BuildConveyorArt();
        BuildExitMarker();
        CombatTestBuilder.Attack("ColdFlyDive", "Fly dive", 2, .46f, .32f, .72f, 3.8f,
            .98f, .32f, DamageType.FlyDive, height: .74f, verticalOffset: -.18f);

        File.Copy(Template, ScenePath, true);
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roots = scene.GetRootGameObjects();
        var controls = roots.Select(root => root.GetComponent<CombatTestSession>()).Single(item => item != null);
        var fields = new SerializedObject(controls);
        var hao = (PlayerMotor2D)fields.FindProperty("haoJu").objectReferenceValue;
        var jiang = (PlayerMotor2D)fields.FindProperty("jiangZhua").objectReferenceValue;
        var spawn = (Transform)fields.FindProperty("spawn").objectReferenceValue;
        var follow = (CameraFollow2D)fields.FindProperty("cameraFollow").objectReferenceValue;
        var manager = (GameManager)fields.FindProperty("manager").objectReferenceValue;
        if (!follow.TryGetComponent<AudioListener>(out _)) follow.gameObject.AddComponent<AudioListener>();
        UnityEngine.Object.DestroyImmediate(controls.Dummy.gameObject);
        UnityEngine.Object.DestroyImmediate(controls.Source.gameObject);
        UnityEngine.Object.DestroyImmediate(controls.gameObject);
        spawn.position = new Vector3(-18, .11f, 0);
        follow.transform.position = new Vector3(-17, 1, -10);
        follow.SetHorizontalBounds(-34.4f, 13.6f);

        foreach (var root in roots) {
            if (root == null) continue;
            switch (root.name) {
                case "Floor":
                case "Floor pixel skin":
                case "Jump platform":
                case "Jump platform pixel skin":
                    UnityEngine.Object.DestroyImmediate(root);
                    break;
                case "Left wall":
                case "Left wall pixel skin":
                    root.transform.position = new Vector3(-21, 2, 0);
                    break;
                case "Rain warehouse backdrop":
                    root.name = "Cold storage backdrop";
                    root.transform.position = new Vector3(-10.4f, 2.75f, -.57f);
                    var background = root.GetComponent<SpriteRenderer>();
                    background.sprite = SpriteAt("Cold backdrop");
                    root.transform.localScale = new Vector3(48 / background.sprite.bounds.size.x,
                        14 / background.sprite.bounds.size.y, 1);
                    break;
            }
        }

        var beltFrames = Enumerable.Range(0, 4).Select(i => SpriteAt("Belt" + i)).ToArray();
        AddFloor("Entrance floor", -21, -15);
        AddFloor("Practice conveyor", -15, -10.5f, 1.65f, beltFrames);
        AddFloor("Transfer floor", -10.5f, -3.5f);
        AddFloor("Combat conveyor", -3.5f, 3.7f, -1.45f, beltFrames);
        AddFloor("Loading floor", 3.7f, 12);
        var enemy = CreateFly();
        var exit = WarehouseBuilder.CreateExit(enemy.GetComponent<Health>(), SpriteAt("Exit beacon"));
        new GameObject("Cold storage run").AddComponent<WarehouseRun>()
            .ConfigureColdStorage(hao, jiang, spawn, enemy, exit, follow, manager);

        EditorSceneManager.MarkSceneDirty(scene);
        StageMusicBuilder.Configure(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save ColdStorage02.");
        var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ColdStorage02 ready: ride the conveyors, defeat the cold fly, and reach the loading exit.");
    }

    static void AddFloor(string name, float start, float end, float carry = 0, Sprite[] beltFrames = null) {
        var floor = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = new Vector3((start + end) * .5f, -1, 0);
        var size = new Vector2(end - start, 1);
        floor.AddComponent<BoxCollider2D>().size = size;
        var visual = floor.AddComponent<SpriteRenderer>();
        visual.sprite = beltFrames != null ? beltFrames[0] : AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Game/Art/PixelPreview/Wet pavement.png");
        visual.drawMode = SpriteDrawMode.Tiled;
        visual.size = size;
        visual.sortingOrder = -5;
        if (beltFrames == null) visual.color = new Color(.72f, .85f, .94f);
        else {
            visual.flipX = carry < 0;
            floor.AddComponent<ConveyorSurface2D>().Configure(carry, visual, beltFrames);
        }
    }

    internal static ColdFlyBrain2D CreateFly() {
        var root = new GameObject("Cold fly") { layer = LayerMask.NameToLayer("Enemy") };
        root.transform.position = new Vector3(.7f, 1.65f, 0);
        var body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(.58f, .44f);
        box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Game/Data/PlayerNoFriction.physicsMaterial2D");
        root.AddComponent<Health>().Configure(6);
        var hurtbox = new GameObject("Fly hurtbox") { layer = LayerMask.NameToLayer("EnemyHurtbox") };
        hurtbox.transform.SetParent(root.transform, false);
        var hit = hurtbox.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        hit.size = new Vector2(.85f, .66f);
        hurtbox.AddComponent<Hurtbox2D>().Configure(root.GetComponent<Health>());

        var dive = AssetDatabase.LoadAssetAtPath<AttackDefinition>("Assets/Game/Data/Combat/ColdFlyDive.asset");
        root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), dive, -1);
        var fly = root.AddComponent<ColdFlyBrain2D>();
        fly.Configure(dive);
        var poses = Enumerable.Range(0, 8).Select(i => SpriteAt("Fly" + i)).ToArray();
        var visual = Child(root.transform, "Fly visual", poses[0], new Vector3(0, -.22f, 0), 2);
        visual.flipX = true;
        var warning = Child(root.transform, "Dive trace", AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Game/Art/PixelPreview/Warehouse/Warning.png"), Vector3.zero, 1);
        warning.enabled = false;
        root.AddComponent<ColdFlyPresentation>().Configure(visual, warning, poses);
        return fly;
    }

    static SpriteRenderer Child(Transform parent, string name, Sprite sprite, Vector3 position, int order) {
        var child = new GameObject(name).AddComponent<SpriteRenderer>();
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.sprite = sprite;
        child.sortingOrder = order;
        return child;
    }

    static Sprite SpriteAt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + name + ".png");

    static void ImportBackdrop() {
        var image = PixelPreviewBuilder.ReadSource("ColdStorageBackdrop");
        try {
            const int width = 1344, height = 448;
            var pixels = PixelPreviewBuilder.Resample(image.GetPixels32(), image.width,
                new RectInt(0, 0, image.width, image.height), width, height);
            PixelPreviewBuilder.SaveSprite("ColdStorage/Cold backdrop", pixels, width, height, new Vector2(.5f, .5f));
        } finally { UnityEngine.Object.DestroyImmediate(image); }
    }

    static void ImportFly() {
        var image = PixelPreviewBuilder.ReadSource("ColdFly");
        try {
            const int alpha = 192;
            var pixels = image.GetPixels32();
            var crops = new RectInt[8];
            for (int i = 0; i < crops.Length; i++) {
                int col = i % 4, row = 1 - i / 4;
                int x = col * image.width / 4, y = row * image.height / 2;
                crops[i] = PixelPreviewBuilder.OpaqueBounds(pixels, image.width, new RectInt(x, y,
                    (col + 1) * image.width / 4 - x, (row + 1) * image.height / 2 - y), alpha);
            }
            float scale = 28f / crops[0].height;
            for (int i = 0; i < crops.Length; i++) {
                var crop = crops[i];
                int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(crop.height * scale));
                var frame = PixelPreviewBuilder.Resample(pixels, image.width, crop, width, height, alpha);
                PixelPreviewBuilder.SaveSprite("ColdStorage/Fly" + i, frame, width, height, new Vector2(.55f, 0));
            }
        } finally { UnityEngine.Object.DestroyImmediate(image); }
    }

    static void BuildConveyorArt() {
        const int size = 32;
        for (int frame = 0; frame < 4; frame++) {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
                    int track = (x + 32 - frame * 4) % 16;
                    bool arrow = y >= 17 && y <= 25 && Mathf.Abs(track - (10 - Mathf.Abs(y - 21))) <= 1;
                    pixels[y * size + x] = y >= 29 ? new Color32(155, 197, 209, 255)
                        : arrow ? new Color32(132, 225, 217, 255)
                        : y > 12 ? new Color32(35, 64, 76, 255)
                        : y == 12 || y == 3 ? new Color32(78, 113, 127, 255)
                        : new Color32(25, 43, 57, 255);
                }
            PixelPreviewBuilder.SaveSprite("ColdStorage/Belt" + frame, pixels, size, size, new Vector2(.5f, .5f));
        }
    }

    static void BuildExitMarker() {
        const int width = 28, height = 26;
        var pixels = new Color32[width * height];
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++) {
                bool frame = x < 3 || x > width - 4 || y < 3 || y > height - 4;
                bool arrow = x >= 6 && x <= 21 &&
                    ((y >= 11 && y <= 15) || (x >= 15 && Mathf.Abs(y - 13) <= 22 - x));
                pixels[y * width + x] = frame ? new Color32(172, 132, 76, 255)
                    : arrow ? new Color32(255, 219, 124, 255) : new Color32(45, 50, 58, 255);
            }
        PixelPreviewBuilder.SaveSprite("ColdStorage/Exit beacon", pixels, width, height, new Vector2(.5f, .5f));
    }
}
}
