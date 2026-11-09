using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class WarehouseBuilder {
    const string Template = "Assets/Game/Scenes/PixelPreview.unity";
    const string ScenePath = "Assets/Game/Scenes/Warehouse01.unity";
    const string Art = "Assets/Game/Art/PixelPreview/Warehouse";
    const string Prefabs = "Assets/Game/Prefabs/";

    [MenuItem("Orange Cat/Build Warehouse 01")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before rebuilding the warehouse.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Template)) throw new FileNotFoundException("Build PixelPreview first.", Template);
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Game/Art/PixelPreview", "Warehouse");
        ImportRat();
        ImportRatHurt();
        ImportHazards();
        BuildProps();
        CombatTestBuilder.Attack("StrayRatCharge", "Rat charge", 2, .42f, .28f, .62f, 4.4f,
            1.05f, .61f, DamageType.RatCharge, height: .64f, verticalOffset: .05f);

        File.Copy(Template, ScenePath, true);
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roots = scene.GetRootGameObjects();
        var controls = roots.Select(root => root.GetComponent<CombatTestSession>()).Single(item => item != null);
        var data = new SerializedObject(controls);
        var hao = (PlayerMotor2D)data.FindProperty("haoJu").objectReferenceValue;
        var jiang = (PlayerMotor2D)data.FindProperty("jiangZhua").objectReferenceValue;
        var spawn = (Transform)data.FindProperty("spawn").objectReferenceValue;
        var follow = (CameraFollow2D)data.FindProperty("cameraFollow").objectReferenceValue;
        var manager = (GameManager)data.FindProperty("manager").objectReferenceValue;
        if (!follow.TryGetComponent<AudioListener>(out _))
            follow.gameObject.AddComponent<AudioListener>();
        UnityEngine.Object.DestroyImmediate(controls.Dummy.gameObject);
        UnityEngine.Object.DestroyImmediate(controls.Source.gameObject);
        UnityEngine.Object.DestroyImmediate(controls.gameObject);
        spawn.position = new Vector3(-18.2f, .11f, 0);
        follow.transform.position = new Vector3(-17, 1, -10);
        ExtendRoute(roots);
        CreateHazards();

        var enemy = CreateRat();
        var goal = CreateExit(enemy.GetComponent<Health>());
        new GameObject("Warehouse run").AddComponent<WarehouseRun>()
            .Configure(hao, jiang, spawn, enemy, goal, follow, manager);
        EditorSceneManager.MarkSceneDirty(scene);
        StageMusicBuilder.Configure(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Warehouse01.");
        var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Warehouse01 ready: cross three obstacles, defeat the stray rat, and collect the last can.");
    }

    static void ExtendRoute(GameObject[] roots) {
        foreach (var root in roots) {
            if (root == null) continue;
            switch (root.name) {
                case "Floor":
                    root.transform.position = new Vector3(-5.2f, -1, 0);
                    root.transform.localScale = new Vector3(34.4f, 1, 1);
                    break;
                case "Floor pixel skin":
                    root.transform.position = new Vector3(-5.2f, -1, 0);
                    root.GetComponent<SpriteRenderer>().size = new Vector2(34.4f, 1);
                    break;
                case "Left wall":
                case "Left wall pixel skin":
                    root.transform.position = new Vector3(-22.4f, 2, 0);
                    break;
                case "Jump platform":
                    root.transform.position = new Vector3(-9.5f, -.25f, 0);
                    root.transform.localScale = new Vector3(1.5f, .5f, 1);
                    break;
                case "Jump platform pixel skin":
                    root.transform.position = new Vector3(-9.5f, -.25f, 0);
                    root.GetComponent<SpriteRenderer>().size = new Vector2(1.5f, .5f);
                    break;
                case "Rain warehouse backdrop":
                    root.transform.position = new Vector3(-4.5f, 2.75f, root.transform.position.z);
                    root.transform.localScale = new Vector3(1.30f, 1, 1);
                    break;
            }
        }
    }

    static void CreateHazards() {
        int layer = LayerMask.NameToLayer("Hazard");
        var vent = new GameObject("Steam vent") { layer = layer };
        vent.transform.position = new Vector3(-15.25f, -.5f, 0);
        var ventContact = vent.AddComponent<BoxCollider2D>();
        ventContact.isTrigger = true;
        ventContact.size = new Vector2(1.25f, .88f);
        ventContact.offset = new Vector2(0, .44f);
        ventContact.enabled = false;
        var grate = RenderChild(vent.transform, "Manhole", LoadSprite("Manhole"), new Vector3(0, .06f, 0), 1);
        Size(grate, 1.42f, .20f);
        var steam = RenderChild(vent.transform, "Steam plume", LoadSprite("Steam"), new Vector3(0, .46f, 0), 3);
        Size(steam, 1.25f, .91f);
        steam.enabled = false;
        var amber = RenderChild(vent.transform, "Steam warning", LoadSprite("Warning"), new Vector3(0, .19f, 0), 2);
        Size(amber, 1.25f, .10f);
        amber.enabled = false;
        vent.AddComponent<WarehouseHazard>().Configure(WarehouseHazardKind.SteamVent, ventContact, steam, amber);

        var spikes = new GameObject("Floor spikes") { layer = layer };
        spikes.transform.position = new Vector3(-11.65f, -.5f, 0);
        var teeth = spikes.AddComponent<BoxCollider2D>();
        teeth.isTrigger = true;
        teeth.size = new Vector2(1.53f, .32f);
        teeth.offset = new Vector2(0, .16f);
        var spikesArt = RenderChild(spikes.transform, "Spikes", LoadSprite("Spikes"), new Vector3(0, .16f, 0), 2);
        Size(spikesArt, 1.60f, .35f);
        spikes.AddComponent<WarehouseHazard>().Configure(WarehouseHazardKind.Spikes, teeth);

        var fan = new GameObject("Industrial fan") { layer = layer };
        fan.transform.position = new Vector3(-6.75f, .22f, 0);
        var blade = fan.AddComponent<CircleCollider2D>();
        blade.isTrigger = true;
        blade.radius = .70f;
        var housing = RenderChild(fan.transform, "Fan housing", LoadSprite("FanHousing"), Vector3.zero, 1);
        Size(housing, 1.68f, 1.68f);
        var rotor = RenderChild(fan.transform, "Fan rotor", LoadSprite("Fan"), Vector3.zero, 2);
        Size(rotor, 1.43f, 1.43f);
        fan.AddComponent<WarehouseHazard>().Configure(WarehouseHazardKind.Fan, blade, rotor);
    }

    static void Size(SpriteRenderer renderer, float width, float height) {
        renderer.transform.localScale = new Vector3(width / renderer.sprite.bounds.size.x,
            height / renderer.sprite.bounds.size.y, 1);
    }

    internal static RatBrain2D CreateRat() {
        var root = new GameObject("Stray Rat") { layer = LayerMask.NameToLayer("Enemy") };
        root.transform.position = new Vector3(1.7f, -.14f, 0);
        var body = root.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 3;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.02f, .70f);
        collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Game/Data/PlayerNoFriction.physicsMaterial2D");
        var health = root.AddComponent<Health>();
        health.Configure(8);

        var hurtbox = new GameObject("Rat hurtbox") { layer = LayerMask.NameToLayer("EnemyHurtbox") };
        hurtbox.transform.SetParent(root.transform, false);
        var hit = hurtbox.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        hit.size = new Vector2(1.18f, .78f);
        hurtbox.AddComponent<Hurtbox2D>().Configure(health);

        var charge = AssetDatabase.LoadAssetAtPath<AttackDefinition>("Assets/Game/Data/Combat/StrayRatCharge.asset");
        root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), charge, -1);
        var rat = root.AddComponent<RatBrain2D>();
        rat.Configure(charge);
        var frames = Enumerable.Range(0, 8).Select(i => LoadSprite("Rat" + i)).ToArray();
        var visual = RenderChild(root.transform, "Rat visual", frames[0], new Vector3(0, -.35f, 0), 2);
        visual.flipX = true;
        var warning = RenderChild(root.transform, "Charge warning", LoadSprite("Warning"), Vector3.zero, 1);
        warning.enabled = false;
        var impact = RenderChild(root.transform, "Rat hit spark", LoadSprite("HitSpark"), Vector3.zero, 5);
        impact.enabled = false;
        root.AddComponent<AudioSource>().playOnAwake = false;
        var presentation = root.AddComponent<RatPresentation>();
        presentation.Configure(visual, warning, frames);
        presentation.ConfigureHit(LoadSprite("RatHurt"), impact,
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Art/RatHit.wav"));
        return rat;
    }

    internal static WarehouseExit CreateExit(Health enemy, Sprite exitMarker = null,
        float gateX = 7.4f, float exitX = 9.7f) {
        var gate = new GameObject("Locked warehouse gate") { layer = LayerMask.NameToLayer("Ground") };
        gate.transform.position = new Vector3(gateX, 2.4f, 0);
        var blocker = gate.AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(.75f, 5.8f);
        var door = RenderChild(gate.transform, "Gate panels", LoadSprite("Gate"), Vector3.zero, 1);
        door.transform.localScale = new Vector3(.75f / door.sprite.bounds.size.x, 5.8f / door.sprite.bounds.size.y, 1);

        var can = new GameObject(exitMarker != null ? "Loading exit" : "Last Can");
        can.transform.position = new Vector3(exitX, -.20f, 0);
        var trigger = can.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(.85f, .70f);
        var visual = RenderChild(can.transform, exitMarker != null ? "Exit beacon" : "Golden can",
            exitMarker != null ? exitMarker : LoadSprite("Can"), Vector3.zero, 1);
        var exit = can.AddComponent<WarehouseExit>();
        exit.Configure(enemy, blocker, door.transform, visual);
        return exit;
    }

    static SpriteRenderer RenderChild(Transform parent, string name, Sprite sprite, Vector3 position, int order) {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    static Sprite LoadSprite(string name) {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + name + ".png");
        return sprite != null ? sprite : throw new InvalidOperationException("Missing warehouse sprite " + name);
    }

    static void ImportRat() {
        var sheet = PixelPreviewBuilder.ReadSource("StrayRat");
        try {
            const int alpha = 192;
            var pixels = sheet.GetPixels32();
            var cells = new RectInt[8];
            var crops = new RectInt[8];
            for (int i = 0; i < cells.Length; i++) {
                int col = i % 4, row = 1 - i / 4;
                int x = col * sheet.width / 4, y = row * sheet.height / 2;
                cells[i] = new RectInt(x, y, (col + 1) * sheet.width / 4 - x, (row + 1) * sheet.height / 2 - y);
                crops[i] = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width, cells[i], alpha);
            }
            float scale = 27f / crops[0].height;
            for (int i = 0; i < cells.Length; i++) {
                var crop = crops[i];
                int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(crop.height * scale));
                var art = PixelPreviewBuilder.Resample(pixels, sheet.width, crop, width, height, alpha);
                float pivot = Mathf.Clamp01((cells[i].center.x - crop.xMin) / crop.width);
                PixelPreviewBuilder.SaveSprite("Warehouse/Rat" + i, art, width, height, new Vector2(pivot, 0));
            }
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }
    }

    static void ImportRatHurt() {
        var source = PixelPreviewBuilder.ReadSource("StrayRatHurt");
        try {
            const int alpha = 192, height = 30;
            var pixels = source.GetPixels32();
            var crop = PixelPreviewBuilder.OpaqueBounds(pixels, source.width,
                new RectInt(0, 0, source.width, source.height), alpha);
            int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * (float)height / crop.height));
            var art = PixelPreviewBuilder.Resample(pixels, source.width, crop, width, height, alpha);
            PixelPreviewBuilder.SaveSprite("Warehouse/RatHurt", art, width, height, new Vector2(.5f, 0));
        } finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    static void ImportHazards() {
        var sheet = PixelPreviewBuilder.ReadSource("WarehouseHazards");
        try {
            const int alpha = 192;
            var pixels = sheet.GetPixels32();
            string[] names = { "Manhole", "Spikes", "Fan", "Steam" };
            int[] widths = { 46, 52, 46, 40 };
            int[] heights = { 7, 11, 46, 29 };
            // The source has a short top row for the grate and teeth.
            int rowDivider = sheet.height * 2 / 3;
            for (int i = 0; i < names.Length; i++) {
                int col = i % 2, x = col * sheet.width / 2;
                bool top = i < 2;
                var cell = new RectInt(x, top ? rowDivider : 0, (col + 1) * sheet.width / 2 - x,
                    top ? sheet.height - rowDivider : rowDivider);
                var crop = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width, cell, alpha);
                var art = PixelPreviewBuilder.Resample(pixels, sheet.width, crop, widths[i], heights[i], alpha);
                PixelPreviewBuilder.SaveSprite("Warehouse/" + names[i], art, widths[i], heights[i], new Vector2(.5f, .5f));
            }
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }
    }

    static void BuildProps() {
        var gate = new Color32[24 * 128];
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 24; x++) {
                bool edge = x < 3 || x >= 21;
                gate[y * 24 + x] = edge
                    ? new Color32(24, 29, 36, 255)
                    : y < 18 && (x + y) % 9 < 4 ? new Color32(195, 127, 52, 255)
                    : y % 7 == 0 ? new Color32(107, 130, 145, 255)
                    : y % 7 == 1 ? new Color32(33, 43, 55, 255) : new Color32(66, 82, 96, 255);
            }
        PixelPreviewBuilder.SaveSprite("Warehouse/Gate", gate, 24, 128, new Vector2(.5f, .5f));

        var can = new Color32[20 * 16];
        for (int y = 1; y < 15; y++)
            for (int x = 2; x < 18; x++) {
                if ((y == 1 || y == 14) && (x < 4 || x > 15)) continue;
                bool edge = y < 4 || y > 11;
                can[y * 20 + x] = edge ? new Color32(255, 209, 110, 255)
                    : x < 5 || x > 15 ? new Color32(155, 77, 42, 255)
                    : new Color32(233, 149, 49, 255);
                if (y >= 6 && y <= 9 && x >= 7 && x <= 12) can[y * 20 + x] = new Color32(255, 239, 161, 255);
            }
        PixelPreviewBuilder.SaveSprite("Warehouse/Can", can, 20, 16, new Vector2(.5f, .5f));

        var strip = new Color32[32 * 32];
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                strip[y * 32 + x] = (x + y / 8) % 8 < 4
                    ? new Color32(249, 173, 63, 210) : new Color32(148, 72, 38, 130);
        PixelPreviewBuilder.SaveSprite("Warehouse/Warning", strip, 32, 32, new Vector2(.5f, .5f));

        const int size = 28;
        var spark = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) {
                int dx = Mathf.Abs(x - 14), dy = Mathf.Abs(y - 14);
                bool cross = dx + 3 * dy <= 13 || dy + 3 * dx <= 13;
                bool rays = Mathf.Abs(dx - dy) <= 1 && dx >= 5 && dx <= 9;
                if (!cross && !rays) continue;
                spark[y * size + x] = dx + dy < 7 ? new Color32(255, 251, 220, 255)
                    : rays ? new Color32(249, 157, 55, 255) : new Color32(255, 215, 106, 255);
            }
        PixelPreviewBuilder.SaveSprite("Warehouse/HitSpark", spark, size, size, new Vector2(.5f, .5f));

        const int housingSize = 54;
        var housing = new Color32[housingSize * housingSize];
        for (int y = 0; y < housingSize; y++)
            for (int x = 0; x < housingSize; x++) {
                int dx = x - 27, dy = y - 27;
                int radiusSquared = dx * dx + dy * dy;
                if (radiusSquared > 26 * 26) continue;
                bool lip = radiusSquared >= 22 * 22;
                housing[y * housingSize + x] = lip ? new Color32(96, 110, 119, 255) : new Color32(29, 41, 51, 255);
            }
        PixelPreviewBuilder.SaveSprite("Warehouse/FanHousing", housing, housingSize, housingSize, new Vector2(.5f, .5f));
    }
}
}
