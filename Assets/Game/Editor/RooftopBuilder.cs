using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrangeCat.Editor {
public static class RooftopBuilder {
    const string Template = "Assets/Game/Scenes/PixelPreview.unity";
    const string FirstStage = "Assets/Game/Scenes/Warehouse01.unity";
    const string ScenePath = "Assets/Game/Scenes/Rooftop04.unity";
    const string Art = "Assets/Game/Art/PixelPreview";
    const float EntranceTop = -.5f;
    const float ServiceTop = .36f;
    const float ExitTop = 1.22f;

    [MenuItem("Orange Cat/Build Rooftop 04")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building Rooftop04.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Template)) throw new FileNotFoundException("Build PixelPreview first.", Template);
        if (!AssetDatabase.IsValidFolder(Art + "/Rooftop")) AssetDatabase.CreateFolder(Art, "Rooftop");
        var backdrop = ImportBackdrop();

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
        Object.DestroyImmediate(controls.Dummy.gameObject);
        Object.DestroyImmediate(controls.Source.gameObject);
        Object.DestroyImmediate(controls.gameObject);
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

        spawn.position = new Vector3(-19.4f, EntranceTop + .62f, 0);
        follow.transform.position = new Vector3(-17, 1, -10);
        follow.SetHorizontalBounds(-24, 16);
        if (!follow.TryGetComponent<AudioListener>(out _)) follow.gameObject.AddComponent<AudioListener>();
        Draw("Blue hour skyline", backdrop, new Vector3(-4, 3.6f, -.57f), new Vector2(42, 14), -20);
        BuildRoute();
        var escape = CreateExit();
        new GameObject("Rooftop run").AddComponent<WarehouseRun>()
            .ConfigureRooftop(hao, jiang, spawn, escape, follow, manager);

        StageMusicBuilder.Configure(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Rooftop04.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath)
            .Append(new EditorBuildSettingsScene(ScenePath, true)).OrderBy(item => item.path != FirstStage).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Rooftop04 ready: cross the steam vents, power both breakers, then use the exit.");
    }

    static void BuildRoute() {
        var masonry = Load("Wet pavement");
        Solid("Lift landing", new Vector2(-15.2f, EntranceTop - .5f), new Vector2(13.6f, 1), masonry);
        Solid("Vent catwalk", new Vector2(-3, ServiceTop - .93f), new Vector2(10.8f, 1.86f), masonry);
        Solid("Exit roof", new Vector2(7.95f, ExitTop - 1.36f), new Vector2(11.1f, 2.72f), masonry);
        Solid("Left parapet", new Vector2(-22, 2.15f), new Vector2(.6f, 5.3f), masonry);
        Solid("Right parapet", new Vector2(13.5f, 3.57f), new Vector2(.6f, 4.7f), masonry);
        AddVent("Landing vent", -14.5f, EntranceTop, 0);
        AddVent("Step vent", -10.9f, EntranceTop, .70f);
        AddVent("Catwalk vent A", -3.3f, ServiceTop, 0);
        AddVent("Catwalk vent B", -.05f, ServiceTop, 1.24f);
    }

    static void AddVent(string name, float x, float floor, float cycleOffset) {
        var root = new GameObject(name) { layer = LayerMask.NameToLayer("Hazard") };
        root.transform.position = new Vector3(x, floor, 0);
        var hit = root.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        hit.offset = new Vector2(0, .57f);
        hit.size = new Vector2(1.40f, 1.14f);
        Draw("Grate", Load("Warehouse/Manhole"), root.transform.position + Vector3.up * .08f,
            new Vector2(1.6f, .20f), 1).transform.SetParent(root.transform);
        var steam = Draw("Steam", Load("Warehouse/Steam"), root.transform.position + Vector3.up * .57f, hit.size, 3);
        var warning = Draw("Warning", Load("Warehouse/Warning"), root.transform.position + Vector3.up * .21f,
            new Vector2(1.4f, .14f), 2);
        steam.transform.SetParent(root.transform);
        warning.transform.SetParent(root.transform);
        steam.enabled = false;
        warning.enabled = false;
        root.AddComponent<WarehouseHazard>().Configure(WarehouseHazardKind.SteamVent, hit, steam, warning, cycleOffset);
    }

    static RooftopEscape CreateExit() {
        var first = Breaker("Breaker A", -6.55f, ServiceTop);
        var second = Breaker("Breaker B", 5.05f, ExitTop);
        var gate = Solid("Roof gate", new Vector2(8.25f, ExitTop + 2.6f), new Vector2(.64f, 5.2f));
        var door = Draw("Gate shutter", Load("Warehouse/Gate"), gate.transform.position, gate.size, 1);
        var exit = Draw("Roof exit", Load("ColdStorage/Exit beacon"), new Vector3(10.8f, ExitTop + .58f, 0),
            new Vector2(.86f, .93f), 2);
        var controller = new GameObject("Roof power controls").AddComponent<RooftopEscape>();
        controller.Configure(new[] { first, second }, gate, door.transform, exit);
        return controller;
    }

    static SpriteRenderer Breaker(string name, float x, float floor) {
        var panel = Draw(name, Load("LoadingBay/Call panel"), new Vector3(x, floor + .51f, 0),
            new Vector2(.70f, 1), 1);
        var lamp = Draw(name + " lamp", Load("LoadingBay/Call lamp"), new Vector3(x, floor + .83f, 0),
            new Vector2(.25f, .25f), 2);
        lamp.transform.SetParent(panel.transform);
        return lamp;
    }

    static BoxCollider2D Solid(string name, Vector2 position, Vector2 size, Sprite sprite = null) {
        var root = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        root.transform.position = position;
        var box = root.AddComponent<BoxCollider2D>();
        box.size = size;
        if (sprite != null) {
            var skin = root.AddComponent<SpriteRenderer>();
            skin.sprite = sprite;
            skin.color = new Color(.87f, .92f, 1);
            skin.drawMode = SpriteDrawMode.Tiled;
            skin.size = size;
        }
        return box;
    }

    static SpriteRenderer Draw(string name, Sprite sprite, Vector3 position, Vector2 size, int order) {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.transform.position = position;
        renderer.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return renderer;
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{name}.png")
        ?? throw new FileNotFoundException($"Build the source art first: {name}.");

    static Sprite ImportBackdrop() {
        var source = PixelPreviewBuilder.ReadSource("RooftopBackdrop");
        try {
            const int width = 1344, height = 448;
            var pixels = PixelPreviewBuilder.Resample(source.GetPixels32(), source.width,
                new RectInt(0, 0, source.width, source.height), width, height);
            return PixelPreviewBuilder.SaveSprite("Rooftop/Blue hour", pixels, width, height, new Vector2(.5f, .5f));
        } finally { Object.DestroyImmediate(source); }
    }
}
}
