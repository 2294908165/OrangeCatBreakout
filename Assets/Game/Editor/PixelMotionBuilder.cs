using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OrangeCat.Editor {
public static class PixelMotionBuilder {
    const string Art = "Assets/Game/Art/PixelPreview/";
    const string Sources = "ArtSource/PixelPreview/";
    const string Prefabs = "Assets/Game/Prefabs/";
    const int AlphaThreshold = 192;
    const int Columns = 4;
    const int Rows = 3;

    [MenuItem("Orange Cat/Build Pixel Animations")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building animations.");
        foreach (var name in new[] { "HaoJu", "JiangZhua" }) {
            string prefabPath = Prefabs + name + "Pixel.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try {
                var renderer = root.GetComponentInChildren<SpriteRenderer>();
                if (!ApplyIfAvailable(root, name, Mathf.RoundToInt(renderer.sprite.rect.height)))
                    throw new FileNotFoundException("Missing source atlas: " + name + "Motion.png");
                if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                    throw new InvalidOperationException("Could not save " + prefabPath);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Pixel animations are ready on both PixelPreview prefabs.");
    }

    internal static bool ApplyIfAvailable(GameObject root, string name, int standingHeight) {
        if (!File.Exists(Sources + name + "Motion.png")) return false;
        bool hao = name == "HaoJu";
        var frames = ImportAtlas(name, standingHeight, hao
            ? new[] { "Claw1Ready", "Claw1Active", "Claw2Ready", "Claw2Active" }
            : new[] { "Guard", "ClawActive", "CounterReady", "CounterActive" });
        var renderer = root.GetComponentInChildren<SpriteRenderer>();
        if (!root.TryGetComponent<PlayerPixelAnimator>(out var animator))
            animator = root.AddComponent<PlayerPixelAnimator>();
        var run = frames.Take(4).ToArray();
        var roll = frames.Skip(4).Take(4).ToArray();
        if (hao) {
            animator.Configure(renderer, renderer.sprite, run, roll, null,
                frames[8], frames[9], frames[10], frames[11], null, null);
            CombatTestBuilder.ConfigureHaoFinisher(root.GetComponent<PlayerMotor2D>().Definition);
            ConfigureClawAudio(root);
            if (File.Exists(Sources + "HaoJuClawFlow.png")) ConfigureClaws(root, animator, standingHeight);
        } else {
            animator.Configure(renderer, renderer.sprite, run, roll, frames[8],
                null, frames[9], null, null, frames[10], frames[11]);
        }
        if (File.Exists(Sources + "PlayerHurt.png"))
            ConfigureHurt(root, animator, name, standingHeight, hao ? 0 : 1);
        if (File.Exists(Sources + "PlayerDeath.png"))
            ConfigureDeath(animator, name, standingHeight, hao ? 0 : 1);
        if (File.Exists(Sources + "PlayerIdle.png"))
            ConfigureIdle(animator, name, standingHeight, hao);
        if (File.Exists(Sources + "PlayerJump.png"))
            ConfigureJump(root, animator, name, standingHeight, hao);
        return true;
    }

    internal static void ConfigureClawAudio(GameObject root) {
        var child = root.transform.Find("Claw swing audio");
        if (child == null) {
            child = new GameObject("Claw swing audio").transform;
            child.SetParent(root.transform, false);
            child.gameObject.AddComponent<ClawSwingAudio>();
        }
        var source = child.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = .65f;
        child.GetComponent<ClawSwingAudio>().Configure(root.GetComponent<PlayerMotor2D>(),
            LoadClawSound("ClawSweep"), LoadClawSound("ClawRise"), LoadClawSound("ClawDrop"));
    }

    static AudioClip LoadClawSound(string name) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Game/Audio/{name}.wav")
        ?? throw new FileNotFoundException($"Missing claw sound {name}.");

    static void ConfigureClaws(GameObject root, PlayerPixelAnimator animator, int standingHeight) {
        var sheet = PixelPreviewBuilder.ReadSource("HaoJuClawFlow");
        var windups = new Sprite[3];
        var strikes = new Sprite[3];
        var recoveries = new Sprite[3];
        try {
            var pixels = sheet.GetPixels32();
            var crops = new RectInt[3, 3];
            for (int row = 0; row < 3; row++) {
                int y = (2 - row) * sheet.height / 3;
                int height = (3 - row) * sheet.height / 3 - y;
                for (int i = 0; i < 3; i++) {
                    int x = i * sheet.width / 3, width = (i + 1) * sheet.width / 3 - x;
                    crops[row, i] = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width,
                        new RectInt(x, y, width, height), AlphaThreshold);
                    if (crops[row, i].height < height / 3)
                        throw new InvalidOperationException($"Missing HaoJu claw pose at row {row + 1}, column {i + 1}.");
                }
            }
            // Match the standing height to the upright windup and keep one scale throughout.
            float scale = standingHeight / (float)crops[0, 1].height;
            // Each column has an authored stance center. A fixed pivot keeps reaching paws from shifting it.
            float[] stanceCenters = { .145f, .468f, .785f };
            for (int i = 0; i < 3; i++) {
                float center = stanceCenters[i] * sheet.width;
                windups[i] = SaveClawPose($"Claw{i + 1}Ready", crops[0, i], center);
                strikes[i] = SaveClawPose($"Claw{i + 1}Active", crops[1, i], center);
                recoveries[i] = SaveClawPose($"Claw{i + 1}Recover", crops[2, i], center);
            }
            Sprite SaveClawPose(string name, RectInt crop, float center) =>
                SavePose("HaoJu", name, pixels, sheet.width, crop, scale, (center - crop.xMin) / crop.width);
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }
        var child = root.transform.Find("Claw trails");
        if (child == null) child = root.transform.Find("Third claw trails");
        var trails = child != null ? child.GetComponent<SpriteRenderer>()
            : new GameObject("Claw trails").AddComponent<SpriteRenderer>();
        trails.gameObject.name = "Claw trails";
        trails.transform.SetParent(root.transform, false);
        var effects = new[] {
            BuildClawTrail("ClawSweepTrail", u => .68f + .18f * Mathf.Sin(Mathf.PI * u) - .52f * u,
                new Color32(255, 151, 86, 232)),
            BuildClawTrail("ClawRiseTrail", u => .10f + .85f * u + .16f * Mathf.Sin(Mathf.PI * u),
                new Color32(132, 229, 228, 232)),
            BuildClawTrail("ClawDropTrail", u => .95f - .86f * u + .13f * Mathf.Sin(Mathf.PI * u),
                new Color32(255, 190, 66, 250), true)
        };
        trails.sprite = effects[0];
        trails.sortingOrder = 4;
        trails.enabled = false;
        animator.ConfigureClawChain(windups, strikes, recoveries, trails, effects);
    }

    static Sprite BuildClawTrail(string name, Func<float, float> path, Color32 edgeColor, bool paired = false) {
        const int width = 42, height = 38;
        var pixels = new Color32[width * height];
        if (paired) DrawArc(.02f, .78f, -.18f, .80f);
        DrawArc(paired ? .14f : .02f, .98f, 0, 1);
        return PixelPreviewBuilder.SaveSprite(name, pixels, width, height, new Vector2(.5f, .5f));

        void DrawArc(float start, float end, float offset, float thickness) {
            for (int x = 0; x < width; x++) {
                float u = ((x + .5f) / width - start) / (end - start);
                if (u <= 0 || u >= 1) continue;
                float center = path(u) + offset;
                float edge = (.023f + .040f * Mathf.Sin(Mathf.PI * u)) * thickness;
                for (int y = 0; y < height; y++) {
                    float distance = Mathf.Abs((y + .5f) / height - center);
                    if (distance <= edge)
                        pixels[y * width + x] = distance < edge * .46f ? new Color32(255, 248, 218, 255) : edgeColor;
                }
            }
        }
    }

    static void ConfigureHurt(GameObject root, PlayerPixelAnimator animator, string name, int height, int column) {
        var sheet = PixelPreviewBuilder.ReadSource("PlayerHurt");
        try {
            var pixels = sheet.GetPixels32();
            var cell = new RectInt(column * sheet.width / 2, 0,
                (column + 1) * sheet.width / 2 - column * sheet.width / 2, sheet.height);
            var crop = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width, cell, AlphaThreshold);
            int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * (float)height / crop.height));
            var pose = PixelPreviewBuilder.Resample(pixels, sheet.width, crop, width, height, AlphaThreshold);
            animator.ConfigureHurtPose(PixelPreviewBuilder.SaveSprite("Motion/" + name + "/Hurt",
                pose, width, height, new Vector2(.55f, 0)));
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }

        var sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Warehouse/HitSpark.png");
        if (sparkSprite == null) return;
        var existing = root.transform.Find("Player hit spark");
        var spark = existing != null ? existing.GetComponent<SpriteRenderer>()
            : new GameObject("Player hit spark").AddComponent<SpriteRenderer>();
        spark.transform.SetParent(root.transform, false);
        spark.sprite = sparkSprite;
        spark.sortingOrder = 5;
        spark.enabled = false;
        if (!root.TryGetComponent<AudioSource>(out var audio)) audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        root.GetComponent<CombatFeedback>().ConfigurePlayerImpact(spark,
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Art/RatHit.wav"));
    }

    static void ConfigureDeath(PlayerPixelAnimator animator, string name, int standingHeight, int column) {
        var sheet = PixelPreviewBuilder.ReadSource("PlayerDeath");
        try {
            var pixels = sheet.GetPixels32();
            int x = column * sheet.width / 2;
            // The taller collapse row extends past the midpoint of the source atlas.
            int rowDivider = sheet.height * 2 / 5;
            int columnWidth = (column + 1) * sheet.width / 2 - x;
            var fall = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width,
                new RectInt(x, rowDivider, columnWidth, sheet.height - rowDivider), AlphaThreshold);
            var down = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width,
                new RectInt(x, 0, columnWidth, rowDivider), AlphaThreshold);
            // Both frames share one scale so the fallen silhouette stays low.
            float scale = .85f * standingHeight / fall.height;
            animator.ConfigureDeathPoses(
                SavePose(name, "DeathFall", pixels, sheet.width, fall, scale, .54f),
                SavePose(name, "DeathDown", pixels, sheet.width, down, scale, .5f));
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }
    }

    static void ConfigureIdle(PlayerPixelAnimator animator, string name, int standingHeight, bool topRow) {
        var sheet = PixelPreviewBuilder.ReadSource("PlayerIdle");
        try {
            var pixels = sheet.GetPixels32();
            int half = sheet.height / 2;
            var crops = new RectInt[4];
            for (int i = 0; i < crops.Length; i++) {
                int x = i * sheet.width / 4;
                var cell = new RectInt(x, topRow ? half : 0, (i + 1) * sheet.width / 4 - x,
                    topRow ? sheet.height - half : half);
                crops[i] = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width, cell, AlphaThreshold);
            }
            // A shared scale keeps the cat the same size throughout the breathing cycle.
            float scale = standingHeight / (float)crops[0].height;
            var frames = new Sprite[crops.Length];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = SavePose(name, name + "Idle" + (i + 1), pixels, sheet.width, crops[i], scale);
            animator.ConfigureIdle(frames);
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }
    }

    static Sprite SavePose(string cat, string name, Color32[] pixels, int sourceWidth,
        RectInt crop, float scale, float? anchor = null) {
        int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(crop.height * scale));
        var art = PixelPreviewBuilder.Resample(pixels, sourceWidth, crop, width, height, AlphaThreshold);
        return PixelPreviewBuilder.SaveSprite("Motion/" + cat + "/" + name, art, width, height,
            new Vector2(anchor ?? PixelPreviewBuilder.FeetCenter(art, width, height), 0));
    }

    static void ConfigureJump(GameObject root, PlayerPixelAnimator animator, string name, int standingHeight, bool topRow) {
        var sheet = PixelPreviewBuilder.ReadSource("PlayerJump");
        var frames = new Sprite[3];
        try {
            var pixels = sheet.GetPixels32();
            int half = sheet.height / 2;
            var crops = new RectInt[3];
            for (int i = 0; i < crops.Length; i++) {
                int x = i * sheet.width / 3;
                var cell = new RectInt(x, topRow ? half : 0, (i + 1) * sheet.width / 3 - x,
                    topRow ? sheet.height - half : half);
                crops[i] = PixelPreviewBuilder.OpaqueBounds(pixels, sheet.width, cell, AlphaThreshold);
            }
            string[] names = { "JumpRise", "JumpFall", "JumpLand" };
            float scale = standingHeight / (float)crops[0].height;
            for (int i = 0; i < frames.Length; i++)
                frames[i] = SavePose(name, names[i], pixels, sheet.width, crops[i], scale, i == 2 ? null : .55f);
        } finally { UnityEngine.Object.DestroyImmediate(sheet); }

        var existing = root.transform.Find("Landing splash");
        var splash = existing != null ? existing.GetComponent<SpriteRenderer>()
            : new GameObject("Landing splash").AddComponent<SpriteRenderer>();
        splash.transform.SetParent(root.transform, false);
        splash.sprite = LandingSplashSprite();
        splash.sortingOrder = 2;
        splash.enabled = false;
        animator.ConfigureJump(frames[0], frames[1], frames[2], splash);
    }

    static Sprite LandingSplashSprite() {
        const string path = Art + "LandingSplash.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        const int width = 32, height = 12;
        var pixels = new Color32[width * height];
        for (int x = 1; x < width - 1; x++) {
            int distance = Mathf.Abs(x - 16);
            int y = Mathf.Max(1, 4 - distance / 3);
            if (distance > 3) pixels[y * width + x] = new Color32(112, 184, 218, 255);
        }
        foreach (var point in new[] { new Vector2Int(3, 6), new Vector2Int(7, 8), new Vector2Int(11, 6),
                     new Vector2Int(21, 7), new Vector2Int(25, 9), new Vector2Int(28, 6) }) {
            pixels[point.y * width + point.x] = new Color32(199, 240, 255, 255);
            pixels[(point.y - 1) * width + point.x] = new Color32(112, 184, 218, 255);
        }
        return PixelPreviewBuilder.SaveSprite("LandingSplash", pixels, width, height, new Vector2(.5f, .15f));
    }

    static Sprite[] ImportAtlas(string name, int standingHeight, string[] attacks) {
        if (!AssetDatabase.IsValidFolder(Art + "Motion"))
            AssetDatabase.CreateFolder(Art.TrimEnd('/'), "Motion");
        if (!AssetDatabase.IsValidFolder(Art + "Motion/" + name))
            AssetDatabase.CreateFolder(Art + "Motion", name);
        var atlas = PixelPreviewBuilder.ReadSource(name + "Motion");
        try {
            var pixels = atlas.GetPixels32();
            var cells = new RectInt[Columns * Rows];
            var crops = new RectInt[cells.Length];
            for (int i = 0; i < cells.Length; i++) {
                cells[i] = Cell(i, atlas.width, atlas.height);
                crops[i] = PixelPreviewBuilder.OpaqueBounds(pixels, atlas.width, cells[i], AlphaThreshold);
                if (crops[i].height < cells[i].height / 4)
                    throw new InvalidOperationException("Missing pose in " + name + ", cell " + (i + 1));
            }
            // One scale for the entire sheet keeps crouch and roll silhouettes shorter.
            float scale = standingHeight / crops.Take(4).Average(crop => (float)crop.height);
            var names = new[] { "Run1", "Run2", "Run3", "Run4", "Roll1", "Roll2", "Roll3", "Roll4" }
                .Concat(attacks).ToArray();
            var frames = new Sprite[cells.Length];
            for (int i = 0; i < frames.Length; i++) {
                var crop = crops[i];
                int width = Mathf.Max(1, Mathf.RoundToInt(crop.width * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(crop.height * scale));
                var prepared = PixelPreviewBuilder.Resample(pixels, atlas.width, crop, width, height, AlphaThreshold);
                // Keep the horizontal animation origin fixed in each source cell.
                float anchor = Mathf.Clamp01((cells[i].center.x - crop.xMin) / crop.width);
                frames[i] = PixelPreviewBuilder.SaveSprite("Motion/" + name + "/" + names[i],
                    prepared, width, height, new Vector2(anchor, 0));
            }
            return frames;
        } finally { UnityEngine.Object.DestroyImmediate(atlas); }
    }

    static RectInt Cell(int index, int width, int height) {
        int column = index % Columns;
        int row = Rows - 1 - index / Columns;
        int x = column * width / Columns;
        int y = row * height / Rows;
        return new RectInt(x, y, (column + 1) * width / Columns - x, (row + 1) * height / Rows - y);
    }
}
}
