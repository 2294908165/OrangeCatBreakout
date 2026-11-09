using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class LoadingBayBuilder {
    const string Template = "Assets/Game/Scenes/PixelPreview.unity";
    const string ScenePath = "Assets/Game/Scenes/LoadingBay03.unity";
    const string Art = "Assets/Game/Art/PixelPreview/LoadingBay";

    [MenuItem("Orange Cat/Build Loading Bay 03")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before rebuilding LoadingBay03.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Template) || !File.Exists("Assets/Game/Scenes/ColdStorage02.unity"))
            throw new FileNotFoundException("Build PixelPreview, Warehouse01, and ColdStorage02 first.");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Game/Art/PixelPreview", "LoadingBay");
        ImportBackdrop();
        BuildProps();
        var bellyLaughPrefab = BellyLaughBuilder.BuildPrefab();
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
        spawn.position = new Vector3(-18.7f, .11f, 0);
        follow.transform.position = new Vector3(-17.5f, 1, -10);
        follow.SetHorizontalBounds(-26, 16);
        SetRoute(roots);
        AddLift("Practice lift", -15.9f, -.05f, 1.35f, 2.40f, 1.65f, true, false);
        CreateSpikeBed();
        AddLift("Spike crossing lift", -9.20f, .02f, 1.88f, 2.55f, 1.55f, true, false);

        var bellyLaugh = BellyLaughBuilder.Place(bellyLaughPrefab, scene);
        var cargo = CreateCargoLift();
        new GameObject("Loading bay run").AddComponent<WarehouseRun>()
            .ConfigureLoadingBay(hao, jiang, spawn, bellyLaugh, cargo, follow, manager);
        StageMusicBuilder.Configure(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save LoadingBay03.");
        var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("LoadingBay03 ready: cross the lifts, call the cargo lift and ride it to escape.");
    }

    [MenuItem("Orange Cat/Update Loading Bay Enemy")]
    public static void UpdateEnemy() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before updating LoadingBay03.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var run = scene.GetRootGameObjects().Select(root => root.GetComponent<WarehouseRun>()).Single(item => item != null);
        var prefab = BellyLaughBuilder.BuildPrefab();
        var monster = run.BellyLaugh != null ? run.BellyLaugh : BellyLaughBuilder.Place(prefab, scene);
        if (run.Rat != null) UnityEngine.Object.DestroyImmediate(run.Rat.gameObject);
        if (run.Fly != null) UnityEngine.Object.DestroyImmediate(run.Fly.gameObject);
        run.ConfigureLoadingBayEnemy(monster);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save LoadingBay03.");
        AssetDatabase.SaveAssets();
        Debug.Log("LoadingBay03 now uses the Belly Laugh enemy.");
    }

    static void SetRoute(GameObject[] roots) {
        foreach (var root in roots) {
            if (root == null) continue;
            switch (root.name) {
                case "Floor":
                    root.transform.position = new Vector3(-5, -1, 0);
                    root.transform.localScale = new Vector3(34, 1, 1);
                    break;
                case "Floor pixel skin":
                    root.transform.position = new Vector3(-5, -1, 0);
                    root.GetComponent<SpriteRenderer>().size = new Vector2(34, 1);
                    break;
                case "Left wall":
                case "Left wall pixel skin":
                    root.transform.position = new Vector3(-22, 2, 0);
                    break;
                case "Jump platform":
                    root.transform.position = new Vector3(-12.0f, -.25f, 0);
                    root.transform.localScale = new Vector3(1.40f, .5f, 1);
                    break;
                case "Jump platform pixel skin":
                    root.transform.position = new Vector3(-12.0f, -.25f, 0);
                    root.GetComponent<SpriteRenderer>().size = new Vector2(1.40f, .5f);
                    break;
                case "Rain warehouse backdrop":
                    root.name = "Loading dock backdrop";
                    root.transform.position = new Vector3(-5, 2.75f, -.57f);
                    root.transform.localScale = Vector3.one;
                    root.GetComponent<SpriteRenderer>().sprite = LoadArt("Loading backdrop");
                    break;
            }
        }
    }

    static LiftPlatform2D AddLift(string name, float x, float low, float high, float width,
        float speed, bool automatic, bool startsHigh) {
        var root = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        root.transform.position = new Vector3(x, startsHigh ? high : low, 0);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, .26f);
        box.usedByEffector = true;
        var effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useSideFriction = false;
        effector.surfaceArc = 160;
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadArt("Lift deck");
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(width, .25f);
        renderer.sortingOrder = 1;
        var lift = root.AddComponent<LiftPlatform2D>();
        lift.Configure(low, high, speed, automatic, startsHigh);
        return lift;
    }

    static void CreateSpikeBed() {
        var root = new GameObject("Lift spike bed") { layer = LayerMask.NameToLayer("Hazard") };
        root.transform.position = new Vector3(-9.05f, -.5f, 0);
        var box = root.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.30f, .32f);
        box.offset = new Vector2(0, .16f);
        var spikes = AddSprite("Spike bed visual",
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/PixelPreview/Warehouse/Spikes.png"),
            root.transform.position + Vector3.up * .16f, 2);
        spikes.drawMode = SpriteDrawMode.Tiled;
        spikes.size = new Vector2(4.35f, .34f);
        root.AddComponent<WarehouseHazard>().Configure(WarehouseHazardKind.Spikes, box);
    }

    static CargoLiftEscape CreateCargoLift() {
        var lift = AddLift("Cargo lift", 9.50f, .01f, 2.92f, 2.52f, 2.0f, false, true);
        AddSprite("Cargo call panel", LoadArt("Call panel"), new Vector3(7.83f, .03f, 0), 1);
        var lamp = AddSprite("Cargo indicator", LoadArt("Call lamp"), new Vector3(7.83f, .49f, 0), 2);
        var exit = new GameObject("Cargo lift controls").AddComponent<CargoLiftEscape>();
        exit.transform.position = new Vector3(7.83f, .25f, 0);
        exit.Configure(lift, lamp);
        return exit;
    }

    static SpriteRenderer AddSprite(string name, Sprite sprite, Vector3 position, int order) {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.position = position;
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    static Sprite LoadArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + name + ".png");

    static void ImportBackdrop() {
        var source = PixelPreviewBuilder.ReadSource("LoadingBayBackdrop");
        try {
            const int width = 1344, height = 448;
            var pixels = PixelPreviewBuilder.Resample(source.GetPixels32(), source.width,
                new RectInt(0, 0, source.width, source.height), width, height);
            PixelPreviewBuilder.SaveSprite("LoadingBay/Loading backdrop", pixels, width, height, new Vector2(.5f, .5f));
        } finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    static void BuildProps() {
        const int width = 32, height = 8;
        var deck = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                deck[y * width + x] = y >= 6 ? new Color32(195, 192, 172, 255)
                    : y == 0 ? new Color32(37, 42, 48, 255)
                    : (x + y) % 8 < 4 ? new Color32(226, 157, 65, 255) : new Color32(61, 69, 77, 255);
        PixelPreviewBuilder.SaveSprite("LoadingBay/Lift deck", deck, width, height, new Vector2(.5f, .5f));

        var panel = new Color32[22 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 22; x++) {
                bool upright = x >= 8 && x <= 13;
                bool face = y >= 17 && x >= 2 && x <= 19;
                bool basePlate = y <= 3;
                if (!upright && !face && !basePlate) continue;
                panel[y * 22 + x] = face && x >= 4 && x <= 17 && y >= 20 && y <= 29
                    ? new Color32(106, 127, 135, 255) : new Color32(43, 53, 64, 255);
            }
        PixelPreviewBuilder.SaveSprite("LoadingBay/Call panel", panel, 22, 32, new Vector2(.5f, .5f));

        var lamp = new Color32[9 * 9];
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                if ((x - 4) * (x - 4) + (y - 4) * (y - 4) <= 16)
                    lamp[y * 9 + x] = new Color32(255, 239, 168, 255);
        PixelPreviewBuilder.SaveSprite("LoadingBay/Call lamp", lamp, 9, 9, new Vector2(.5f, .5f));
    }
}
}
