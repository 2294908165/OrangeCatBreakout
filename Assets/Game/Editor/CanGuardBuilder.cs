using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrangeCat.Editor {
public static class CanGuardBuilder {
    const string Template = "Assets/Game/Scenes/PixelPreview.unity";
    const string ScenePath = "Assets/Game/Scenes/CanGuard05.unity";
    const string Art = "Assets/Game/Art/PixelPreview";
    const string MusicPath = "Assets/Game/Audio/CanGuardBGM.mp3";
    const float FloorY = -.5f;
    const float GuardHeight = 2.18f;
    static readonly Vector2 Arena = new(-8.9f, 8.10f);

    [MenuItem("Orange Cat/Build Can Guard 05")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building CanGuard05.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Template)) throw new FileNotFoundException("Build PixelPreview first.", Template);
        if (!AssetDatabase.IsValidFolder(Art + "/CanGuard")) AssetDatabase.CreateFolder(Art, "CanGuard");
        var background = ImportBackdrop();
        var idle = ImportIdle();
        var poses = ImportPoses();
        var charge = CombatTestBuilder.Attack("CanGuardCharge", "Shoulder Charge", 2,
            .70f, 1.50f, 1.55f, 5.6f, 1.94f, .58f, DamageType.BossCharge, height: 1.68f, verticalOffset: -.12f);
        var wave = CombatTestBuilder.Attack("CanGuardWave", "Ground Wave", 1,
            0, 2.45f, 0, 4.1f, .97f, 0, DamageType.BossShockwave, height: .48f, verticalOffset: .24f);
        var stripe = BuildWarningStripe();
        var ripple = BuildWaveSprite();

        File.Copy(Template, ScenePath, true);
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roots = scene.GetRootGameObjects();
        var session = roots.Select(root => root.GetComponent<CombatTestSession>()).Single(component => component != null);
        var fields = new SerializedObject(session);
        var hao = (PlayerMotor2D)fields.FindProperty("haoJu").objectReferenceValue;
        var jiang = (PlayerMotor2D)fields.FindProperty("jiangZhua").objectReferenceValue;
        var spawn = (Transform)fields.FindProperty("spawn").objectReferenceValue;
        var camera = (CameraFollow2D)fields.FindProperty("cameraFollow").objectReferenceValue;
        var manager = (GameManager)fields.FindProperty("manager").objectReferenceValue;
        Object.DestroyImmediate(session.Dummy.gameObject);
        Object.DestroyImmediate(session.Source.gameObject);
        Object.DestroyImmediate(session.gameObject);
        foreach (var root in roots) {
            if (root == null) continue;
            switch (root.name) {
                case "Floor":
                case "Floor pixel skin":
                case "Jump platform":
                case "Jump platform pixel skin":
                case "Left wall":
                case "Left wall pixel skin":
                case "Right wall":
                case "Right wall pixel skin":
                case "Rain warehouse backdrop":
                    Object.DestroyImmediate(root);
                    break;
            }
        }
        spawn.position = new Vector3(-8.1f, .12f, 0);
        camera.transform.position = new Vector3(-4.7f, 1, -10);
        camera.SetHorizontalBounds(-11.2f, 13.3f);
        if (!camera.TryGetComponent<AudioListener>(out _)) camera.gameObject.AddComponent<AudioListener>();
        var backdrop = Draw("Machine room backdrop", background, new Vector3(1, 2.8f, -.57f), -20);
        backdrop.transform.localScale = new Vector3(28 / background.bounds.size.x, 12.5f / background.bounds.size.y, 1);
        Solid("Machine room floor", new Vector2(.95f, -1.1f), new Vector2(23.5f, 1.2f));
        Solid("Left impact wall", new Vector2(-10.2f, 2.55f), new Vector2(.66f, 6.1f));
        Solid("Far wall", new Vector2(12.1f, 2.55f), new Vector2(.66f, 6.1f));
        var guard = CreateGuard(idle, poses, charge, wave, stripe, ripple);
        var goal = WarehouseBuilder.CreateExit(guard.GetComponent<Health>(), gateX: 9.4f, exitX: 11.05f);
        new GameObject("Can Guard run").AddComponent<WarehouseRun>()
            .ConfigureBoss(hao, jiang, spawn, guard, goal, camera, manager);
        AddMusic();

        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save CanGuard05.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath)
            .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("CanGuard05 ready: defeat the Can Guard and collect the last can.");
    }

    internal static void AddMusic() {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath)
            ?? throw new FileNotFoundException("Missing Can Guard music.", MusicPath);
        var source = new GameObject("Can Guard BGM").AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = true;
        source.loop = true;
        source.spatialBlend = 0;
        source.volume = .38f;
    }

    static CanGuardBrain CreateGuard(Sprite idle, Sprite[] poses, AttackDefinition charge, AttackDefinition wave,
        Sprite stripe, Sprite ripple) {
        var root = new GameObject("Can Guard") { layer = LayerMask.NameToLayer("Enemy") };
        root.transform.position = new Vector3(2.65f, FloorY + GuardHeight * .5f, 0);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        root.AddComponent<BoxCollider2D>().size = new Vector2(1.9f, GuardHeight);
        var health = root.AddComponent<Health>();
        health.Configure(32);

        var hurtbox = new GameObject("Guard hurtbox") { layer = LayerMask.NameToLayer("EnemyHurtbox") };
        hurtbox.transform.SetParent(root.transform, false);
        var box = hurtbox.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.16f, GuardHeight);
        box.isTrigger = true;
        hurtbox.AddComponent<Hurtbox2D>().Configure(health);
        root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), charge, -1);
        var boss = root.AddComponent<CanGuardBrain>();
        boss.Configure(charge, CreateWave("Left ground wave", ripple, wave), CreateWave("Right ground wave", ripple, wave), Arena, FloorY);
        var visual = Draw("Guard pose", idle, Vector3.zero, 3);
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.down * (GuardHeight * .5f);
        var warning = Draw("Boss danger marker", stripe, Vector3.zero, 2);
        warning.transform.SetParent(root.transform, false);
        warning.drawMode = SpriteDrawMode.Tiled;
        warning.enabled = false;
        var spark = Draw("Guard hit spark", Load("Warehouse/HitSpark"), Vector3.zero, 5);
        spark.transform.SetParent(root.transform, false);
        spark.enabled = false;
        root.AddComponent<AudioSource>().playOnAwake = false;
        root.AddComponent<CanGuardPresentation>().Configure(visual, warning, spark, idle, poses,
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Art/RatHit.wav"));
        return boss;
    }

    static BossShockwave CreateWave(string name, Sprite ripple, AttackDefinition attack) {
        var root = new GameObject(name) { layer = LayerMask.NameToLayer("EnemyHitbox") };
        root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), attack);
        var visual = Draw("Wave pixels", ripple, Vector3.zero, 4);
        visual.transform.SetParent(root.transform, false);
        var wave = root.AddComponent<BossShockwave>();
        wave.Configure(attack, visual, new Vector2(-10.1f, 9.1f));
        root.SetActive(false);
        return wave;
    }

    static void Solid(string name, Vector2 point, Vector2 size) {
        var visual = Draw(name, Load("Wet pavement"), point, 0);
        visual.gameObject.layer = LayerMask.NameToLayer("Ground");
        visual.color = new Color(.83f, .83f, 1);
        visual.drawMode = SpriteDrawMode.Tiled;
        visual.size = size;
        visual.gameObject.AddComponent<BoxCollider2D>().size = size;
    }

    static SpriteRenderer Draw(string name, Sprite sprite, Vector3 point, int sortingOrder) {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.position = point;
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{name}.png")
        ?? throw new FileNotFoundException($"Missing sprite {name}.");

    static Sprite ImportBackdrop() {
        var source = PixelPreviewBuilder.ReadSource("CanGuardRoom");
        try {
            const int width = 1152, height = 384;
            var pixels = PixelPreviewBuilder.Resample(source.GetPixels32(), source.width,
                new RectInt(0, 0, source.width, source.height), width, height);
            return PixelPreviewBuilder.SaveSprite("CanGuard/Machine room", pixels, width, height, new Vector2(.5f, .5f));
        } finally { Object.DestroyImmediate(source); }
    }

    static Sprite ImportIdle() {
        var source = PixelPreviewBuilder.ReadSource("CanGuardIdle");
        try {
            var pixels = source.GetPixels32();
            var crop = PixelPreviewBuilder.OpaqueBounds(pixels, source.width,
                new RectInt(0, 0, source.width, source.height), 192);
            return SavePose("Idle", pixels, source.width, crop, 86f / crop.height);
        } finally { Object.DestroyImmediate(source); }
    }

    static Sprite[] ImportPoses() {
        var source = PixelPreviewBuilder.ReadSource("CanGuardPoses");
        try {
            var pixels = source.GetPixels32();
            string[] names = { "Crouch", "Charge", "Stagger", "Leap", "Slam", "Defeated" };
            var bounds = new RectInt[names.Length];
            int split = source.height / 2;
            // The bottom poses are wider, so the gutters are slightly left of the top row's.
            float[] upperCuts = { 0, .325f, .665f, 1 };
            float[] lowerCuts = { 0, .320f, .652f, 1 };
            for (int i = 0; i < names.Length; i++) {
                int col = i % 3, y = i < 3 ? split : 0;
                var cuts = i < 3 ? upperCuts : lowerCuts;
                int left = Mathf.RoundToInt(cuts[col] * source.width);
                int right = Mathf.RoundToInt(cuts[col + 1] * source.width);
                var cell = new RectInt(left, y, right - left, i < 3 ? source.height - split : split);
                bounds[i] = PixelPreviewBuilder.OpaqueBounds(pixels, source.width, cell, 192);
            }
            float scale = 81f / bounds[2].height;
            return names.Select((name, i) => SavePose(name, pixels, source.width, bounds[i], scale)).ToArray();
        } finally { Object.DestroyImmediate(source); }
    }

    static Sprite SavePose(string name, Color32[] pixels, int sourceWidth, RectInt crop, float scale) {
        int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(crop.height * scale));
        var sampled = PixelPreviewBuilder.Resample(pixels, sourceWidth, crop, width, height, 192);
        float origin = name == "Defeated" ? .51f : PixelPreviewBuilder.FeetCenter(sampled, width, height);
        return PixelPreviewBuilder.SaveSprite("CanGuard/" + name, sampled, width, height, new Vector2(origin, 0));
    }

    static Sprite BuildWarningStripe() {
        const int width = 16, height = 5;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) {
                byte value = (x + y) % 8 < 4 ? (byte)255 : (byte)108;
                pixels[y * width + x] = new Color32(value, value, value, 255);
            }
        return PixelPreviewBuilder.SaveSprite("CanGuard/Danger stripe", pixels, width, height, new Vector2(.5f, .5f));
    }

    static Sprite BuildWaveSprite() {
        const int width = 31, height = 15;
        var pixels = new Color32[width * height];
        for (int x = 0; x < width; x++) {
            int peak = 2 + Mathf.RoundToInt(11 * Mathf.Max(0, 1 - Mathf.Abs(x - 22) / 12f));
            for (int y = 0; y < peak; y++) {
                var color = y > peak - 3 ? new Color32(240, 228, 255, 255)
                    : y > 1 ? new Color32(174, 111, 255, 245) : new Color32(98, 54, 168, 220);
                pixels[y * width + x] = color;
            }
        }
        return PixelPreviewBuilder.SaveSprite("CanGuard/Ground wave", pixels, width, height, new Vector2(.5f, 0));
    }
}
}
