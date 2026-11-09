using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class PixelPreviewBuilder {
    const string Art = "Assets/Game/Art/PixelPreview/";
    const string Source = "ArtSource/PixelPreview/";
    const string Prefabs = "Assets/Game/Prefabs/";
    const string CombatScene = "Assets/Game/Scenes/CombatTest.unity";
    const string ScenePath = "Assets/Game/Scenes/PixelPreview.unity";
    const int PixelsPerUnit = 32;

    [MenuItem("Orange Cat/Build Pixel Preview")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the pixel preview.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(CombatScene)) CombatTestBuilder.Build();
        if (!AssetDatabase.IsValidFolder(Art.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Game/Art", "PixelPreview");

        var cats = ReadSource("Cats");
        try {
            int half = cats.width / 2;
            var pixels = cats.GetPixels32();
            PixelPrefab("HaoJu", 42, pixels, cats.width, new RectInt(0, 0, half, cats.height));
            PixelPrefab("JiangZhua", 46, pixels, cats.width, new RectInt(half, 0, cats.width - half, cats.height));
        } finally { UnityEngine.Object.DestroyImmediate(cats); }
        MakeBackdrop();
        MakeTile("Wet pavement", 32, 32, false);
        MakeTile("Crate panels", 48, 16, true);
        MakeTrainingProps();

        // Copy the authored scene to keep all gameplay references and the original test scene.
        // Keeping the existing scene asset also preserves its GUID when rebuilding.
        File.Copy(CombatScene, ScenePath, true);
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roots = scene.GetRootGameObjects();
        var session = roots.Select(root => root.GetComponent<CombatTestSession>()).Single(item => item != null);
        // Opening a scene can unload assets referenced only by local variables.
        var hao = LoadPlayer("HaoJu");
        var jiang = LoadPlayer("JiangZhua");
        var background = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Rain Warehouse.png");
        var floor = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Wet pavement.png");
        var crate = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Crate panels.png");
        var sessionData = new SerializedObject(session);
        sessionData.FindProperty("haoJu").objectReferenceValue = hao;
        sessionData.FindProperty("jiangZhua").objectReferenceValue = jiang;
        sessionData.FindProperty("expanded").boolValue = false;
        sessionData.ApplyModifiedPropertiesWithoutUndo();
        SkinTarget(session.Dummy, "Target dummy");
        SkinTarget(session.Source, "Training launcher");

        var backdrop = new GameObject("Rain warehouse backdrop").AddComponent<SpriteRenderer>();
        backdrop.sprite = background;
        backdrop.sortingOrder = -20;
        backdrop.transform.position = new Vector3(0, 2.75f, 0);
        backdrop.transform.localScale = new Vector3(42 / background.bounds.size.x, 14 / background.bounds.size.y, 1);

        var groundLayer = LayerMask.NameToLayer("Ground");
        foreach (var root in roots.Where(root => root.layer == groundLayer)) {
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider == null || !root.TryGetComponent<SpriteRenderer>(out var oldVisual)) continue;
            var size = collider.bounds.size;
            bool useCrate = size.y < .8f;
            oldVisual.enabled = false;
            var skin = new GameObject(root.name + " pixel skin").AddComponent<SpriteRenderer>();
            skin.transform.position = collider.bounds.center;
            skin.sprite = useCrate ? crate : floor;
            skin.drawMode = SpriteDrawMode.Tiled;
            skin.size = size;
            skin.sortingOrder = -5;
            if (size.y > 2) skin.color = new Color(.58f, .67f, .75f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the pixel preview.");
        var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("PixelPreview is ready. Open the scene and press Play.");
    }

    static PlayerMotor2D LoadPlayer(string name) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + "Pixel.prefab");
        if (prefab == null || !prefab.TryGetComponent<PlayerMotor2D>(out var motor))
            throw new InvalidOperationException("Could not load " + name + "Pixel prefab.");
        return motor;
    }

    static void SkinTarget(Health target, string asset) {
        var renderer = target.transform.Find("Visual").GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset + ".png");
        renderer.color = Color.white;
        renderer.transform.localScale = Vector3.one;
    }

    static void PixelPrefab(string name, int height, Color32[] image, int sourceWidth, RectInt cell) {
        var crop = OpaqueBounds(image, sourceWidth, cell);
        int width = Mathf.RoundToInt(crop.width * (float)height / crop.height);
        var pixels = Resample(image, sourceWidth, crop, width, height);
        // Anchor under the planted paws rather than the center of a tail-heavy image.
        float feet = FeetCenter(pixels, width, height);
        var sprite = SaveSprite(name, pixels, width, height, new Vector2(feet, 0));

        var root = PrefabUtility.LoadPrefabContents(Prefabs + name + "Combat.prefab");
        try {
            var body = root.GetComponent<BoxCollider2D>();
            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 3;
            renderer.transform.localScale = Vector3.one;
            renderer.transform.localPosition = new Vector3(0, body.offset.y - body.size.y * .5f, 0);
            PixelMotionBuilder.ApplyIfAvailable(root, name, height);
            if (PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + "Pixel.prefab") == null)
                throw new InvalidOperationException("Could not save " + name + "Pixel prefab.");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    internal static Texture2D ReadSource(string name) {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(File.ReadAllBytes(Source + name + ".png"))) return texture;
        UnityEngine.Object.DestroyImmediate(texture);
        throw new InvalidOperationException("Could not decode " + name + ".png");
    }

    internal static RectInt OpaqueBounds(Color32[] pixels, int sourceWidth, RectInt cell, int alpha = 128) {
        int left = cell.xMax, bottom = cell.yMax, right = -1, top = -1;
        for (int y = cell.yMin; y < cell.yMax; y++)
            for (int x = cell.xMin; x < cell.xMax; x++) {
                if (pixels[y * sourceWidth + x].a < alpha) continue;
                left = Mathf.Min(left, x);
                bottom = Mathf.Min(bottom, y);
                right = Mathf.Max(right, x);
                top = Mathf.Max(top, y);
            }
        if (right < left || (left == cell.xMin && bottom == cell.yMin && right == cell.xMax - 1 && top == cell.yMax - 1))
            throw new InvalidOperationException("Character source needs an isolated sprite on a transparent background.");
        return new RectInt(left, bottom, right - left + 1, top - bottom + 1);
    }

    internal static Color32[] Resample(Color32[] image, int sourceWidth, RectInt region, int width, int height, int alpha = 128) {
        var result = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) {
                int sx = region.x + (2 * x + 1) * region.width / (2 * width);
                int sy = region.y + (2 * y + 1) * region.height / (2 * height);
                var color = image[sy * sourceWidth + sx];
                color.a = color.a < alpha ? (byte)0 : (byte)255;
                result[y * width + x] = color;
            }
        return result;
    }

    internal static float FeetCenter(Color32[] pixels, int width, int height) {
        int left = width, right = -1;
        for (int y = 0; y < Mathf.Max(1, height / 10); y++)
            for (int x = 0; x < width; x++) {
                if (pixels[y * width + x].a == 0) continue;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }
        return right < left ? .5f : (left + right + 1f) / (2 * width);
    }

    static Sprite MakeBackdrop() {
        var texture = ReadSource("RainWarehouse");
        try {
            var pixels = Resample(texture.GetPixels32(), texture.width,
                new RectInt(0, 0, texture.width, texture.height), 1344, 448);
            return SaveSprite("Rain Warehouse", pixels, 1344, 448, new Vector2(.5f, .5f));
        } finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    static Sprite MakeTile(string name, int width, int height, bool timber) {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) {
                int grain = (x * 13 + y * 5 + x * y * 7) % 9;
                if (timber) {
                    bool brace = x < 2 || x >= width - 2 || y < 2 || Mathf.Abs(x - y * 3) < 3;
                    pixels[y * width + x] = brace ? new Color32(119, 81, 60, 255)
                        : new Color32((byte)(65 + grain), (byte)(46 + grain), (byte)(39 + grain), 255);
                } else {
                    bool seam = (y % 8 == 0) || ((x + y / 8 * 11) % 16 == 0);
                    pixels[y * width + x] = seam ? new Color32(24, 36, 54, 255)
                        : new Color32((byte)(37 + grain), (byte)(52 + grain), (byte)(67 + grain), 255);
                }
                if (y == height - 1) pixels[y * width + x] = new Color32(122, 163, 181, 255);
            }
        return SaveSprite(name, pixels, width, height, new Vector2(.5f, .5f));
    }

    // Small hand-authored, pixel-aligned shapes replace the colored training blocks.
    static void MakeTrainingProps() {
        const int width = 32, height = 43;
        var outline = new Color32(28, 31, 40, 255);
        var darkWood = new Color32(84, 56, 42, 255);
        var lightWood = new Color32(178, 129, 83, 255);
        var steel = new Color32(81, 103, 120, 255);
        var amber = new Color32(245, 166, 69, 255);
        var dummy = new Color32[width * height];
        Fill(dummy, width, new RectInt(3, 0, 26, 5), outline);
        Fill(dummy, width, new RectInt(13, 4, 6, 35), darkWood);
        Fill(dummy, width, new RectInt(15, 5, 2, 12), lightWood);
        Fill(dummy, width, new RectInt(5, 16, 22, 24), outline);
        Fill(dummy, width, new RectInt(7, 18, 18, 20), lightWood);
        Fill(dummy, width, new RectInt(7, 18, 18, 3), darkWood);
        Fill(dummy, width, new RectInt(8, 34, 16, 2), new Color32(211, 165, 112, 255));
        Fill(dummy, width, new RectInt(11, 23, 10, 9), new Color32(135, 59, 49, 255));
        Fill(dummy, width, new RectInt(14, 26, 4, 3), new Color32(224, 175, 119, 255));
        SaveSprite("Target dummy", dummy, width, height, new Vector2(.5f, .5f));

        var launcher = new Color32[width * height];
        Fill(launcher, width, new RectInt(2, 0, 29, 6), outline);
        Fill(launcher, width, new RectInt(13, 5, 15, 32), outline);
        Fill(launcher, width, new RectInt(15, 7, 11, 29), steel);
        Fill(launcher, width, new RectInt(19, 32, 5, 7), outline);
        Fill(launcher, width, new RectInt(20, 36, 3, 4), amber);
        Fill(launcher, width, new RectInt(2, 22, 17, 10), outline);
        Fill(launcher, width, new RectInt(3, 25, 16, 4), steel);
        Fill(launcher, width, new RectInt(2, 24, 3, 6), amber);
        Fill(launcher, width, new RectInt(15, 10, 11, 5), darkWood);
        for (int x = 16; x < 26; x += 4)
            Fill(launcher, width, new RectInt(x, 10, 2, 5), amber);
        SaveSprite("Training launcher", launcher, width, height, new Vector2(.5f, .5f));
    }

    static void Fill(Color32[] pixels, int width, RectInt area, Color32 color) {
        for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                pixels[y * width + x] = color;
    }

    internal static Sprite SaveSprite(string name, Color32[] pixels, int width, int height, Vector2 pivot) {
        string path = Art + name + ".png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePixelsPerUnit = PixelsPerUnit;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
            throw new InvalidOperationException("Unity did not import a Sprite at " + path);
    }
}
}
