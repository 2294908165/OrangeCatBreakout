using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeCat.Editor {
public static class CombatTestBuilder {
    const string ScenePath = "Assets/Game/Scenes/CombatTest.unity";
    const string Data = "Assets/Game/Data/Combat/";
    const string Prefabs = "Assets/Game/Prefabs/";

    [MenuItem("Orange Cat/Build or Update CombatTest")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building CombatTest.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Prefabs + "HaoJu.prefab") || !File.Exists(Prefabs + "JiangZhua.prefab"))
            PhaseOneBuilder.Build();

        PhaseOneBuilder.ConfigureSettings();
        CreateHurtboxLayers();
        if (!AssetDatabase.IsValidFolder(Data.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Game/Data", "Combat");

        var first = Attack("HaoJuClaw1", "Claw I", 2, .07f, .10f, .17f, 3.4f, 1.35f, .87f, DamageType.Claw, true);
        var second = Attack("HaoJuClaw2", "Claw II", 2, .09f, .11f, .19f, 4.8f, 1.45f, .91f, DamageType.Claw);
        var guardClaw = Attack("JiangZhuaClaw", "Guard Claw", 1, .10f, .12f, .22f, 3f, 1.25f, .82f, DamageType.Claw);
        var counter = Attack("JiangZhuaCounter", "Counter", 3, .08f, .12f, .20f, 5.6f, 1.95f, 1.08f, DamageType.Counter);
        var training = Attack("TrainingStrike", "Training Strike", 2, .50f, .12f, .25f, 3.8f, 2.4f, 1.45f, DamageType.Training);
        var hao = Character("HaoJu", 4, 7f, first, second, null);
        HissSkillBuilder.ConfigureAttack(hao);
        ConfigureHaoFinisher(hao);
        var jiang = Character("JiangZhua", 6, 6.3f, guardClaw, null, counter);
        var haoPrefab = CombatPrefab("HaoJu", hao);
        var jiangPrefab = CombatPrefab("JiangZhua", jiang);
        var sprite = PhaseOneBuilder.CreateSprite();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PhaseOneBuilder.Ground("Floor", new Vector2(0,-1), new Vector2(24,1), sprite);
        PhaseOneBuilder.Ground("Left wall", new Vector2(-12,2), new Vector2(1,6), sprite);
        PhaseOneBuilder.Ground("Right wall", new Vector2(12,2), new Vector2(1,6), sprite);
        PhaseOneBuilder.Ground("Jump platform", new Vector2(6,1), new Vector2(3,.5f), sprite);
        var spawn = new GameObject("Player spawn").transform;
        spawn.position = new Vector3(-5, .11f, 0);

        var dummy = Target("Training Dummy", new Vector2(-2.5f,.18f), sprite, new Color(.55f,.74f,.43f), 20);
        AddHurtbox(dummy, "EnemyHurtbox", new Vector2(.85f,1.35f), Vector2.zero);
        AddHurtbox(dummy, "EnemyHurtbox", new Vector2(.60f,.55f), new Vector2(0,.36f));
        var source = Target("Training Device", new Vector2(2.5f,.18f), sprite, new Color(.81f,.3f,.27f), 20);
        AddHurtbox(source, "EnemyHurtbox", new Vector2(.9f,1.35f), Vector2.zero);
        var warning = RenderChild(source.transform, sprite, new Color(1,.64f,.1f,.26f),
            training.HitboxSize, new Vector2(-training.HitboxOffset.x, training.HitboxOffset.y));
        warning.name = "Strike telegraph";
        warning.sortingOrder = -1;
        warning.enabled = false;
        source.gameObject.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("PlayerHurtbox"), training, -1);
        source.gameObject.AddComponent<TrainingAttackSource>().Configure(training, warning);

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-4,1,-10);
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 4.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.12f,.17f,.22f);
        var follow = cameraObject.AddComponent<CameraFollow2D>();
        new GameObject("Combat test controls").AddComponent<CombatTestSession>()
            .Configure(haoPrefab, jiangPrefab, spawn, manager, follow, dummy, source);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("CombatTest could not be saved.");
        var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("CombatTest scene, data and both combat prefabs are ready.");
    }

    static PlayerMotor2D CombatPrefab(string name, CharacterDefinition data) {
        var root = PrefabUtility.LoadPrefabContents(Prefabs + name + ".prefab");
        try {
            var motor = root.GetComponent<PlayerMotor2D>();
            var sprite = root.GetComponentInChildren<SpriteRenderer>();
            motor.Configure(data, root.GetComponent<PlayerInputReader>(), sprite, LayerMask.GetMask("Ground"));
            UnityEngine.Object.DestroyImmediate(root.GetComponent<PlayerDebugHUD>());
            var health = root.AddComponent<Health>();
            health.Configure(data.MaxHealth);
            root.AddComponent<AttackHitbox2D>().Configure(LayerMask.GetMask("EnemyHurtbox"), data.PrimaryAttack);
            root.AddComponent<PlayerCombat>();
            root.AddComponent<CombatFeedback>().Configure(sprite);
            AddHurtbox(health, "PlayerHurtbox", new Vector2(.68f, 1.15f), Vector2.zero);
            if (data.HissAttack != null) HissSkillBuilder.ConfigurePlayer(root, data.HissAttack);
            string path = Prefabs + name + "Combat.prefab";
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<PlayerMotor2D>();
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Health Target(string name, Vector2 position, Sprite sprite, Color tint, float hp) {
        var root = new GameObject(name);
        root.layer = LayerMask.NameToLayer("Enemy");
        root.transform.position = position;
        var visual = RenderChild(root.transform, sprite, tint, new Vector2(.9f,1.35f), Vector2.zero);
        var health = root.AddComponent<Health>();
        health.Configure(hp);
        root.AddComponent<CombatFeedback>().Configure(visual, true);
        return health;
    }

    static SpriteRenderer RenderChild(Transform parent, Sprite sprite, Color tint, Vector2 size, Vector2 center) {
        var child = new GameObject("Visual");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = center;
        child.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        return renderer;
    }

    static void AddHurtbox(Health owner, string layer, Vector2 size, Vector2 offset) {
        var child = new GameObject("Hurtbox") { layer = LayerMask.NameToLayer(layer) };
        child.transform.SetParent(owner.transform, false);
        var box = child.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        box.offset = offset;
        child.AddComponent<Hurtbox2D>().Configure(owner);
    }

    static CharacterDefinition Character(string name, float hp, float speed, AttackDefinition primary,
        AttackDefinition combo, AttackDefinition counter) {
        string path = Data + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
        if (existing != null) return existing;
        var data = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Game/Data/" + name + ".asset"));
        var serialized = new SerializedObject(data);
        serialized.FindProperty("maxHealth").floatValue = hp;
        serialized.FindProperty("moveSpeed").floatValue = speed;
        serialized.FindProperty("primaryAttack").objectReferenceValue = primary;
        serialized.FindProperty("comboAttack").objectReferenceValue = combo;
        serialized.FindProperty("counterAttack").objectReferenceValue = counter;
        serialized.FindProperty("supportsGuard").boolValue = counter != null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    internal static void ConfigureHaoFinisher(CharacterDefinition hao) {
        if (hao.FinisherAttack != null) return;
        var finisher = Attack("HaoJuClaw3", "Twin Claw Drop", 3, .12f, .12f, .24f, 6.5f,
            1.84f, .98f, DamageType.Claw, height: 1.15f, verticalOffset: .12f);
        var fields = new SerializedObject(hao);
        fields.FindProperty("finisherAttack").objectReferenceValue = finisher;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static AttackDefinition Attack(string id, string name, int damage, float startup, float active, float recovery,
        float knockback, float width, float offset, DamageType type, bool rollCancel = false,
        float height = 1f, float verticalOffset = .04f) {
        string path = Data + id + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<AttackDefinition>(path);
        if (existing != null) return existing;
        var data = ScriptableObject.CreateInstance<AttackDefinition>();
        var serialized = new SerializedObject(data);
        serialized.FindProperty("attackName").stringValue = name;
        serialized.FindProperty("damage").intValue = damage;
        serialized.FindProperty("startup").floatValue = startup;
        serialized.FindProperty("active").floatValue = active;
        serialized.FindProperty("recovery").floatValue = recovery;
        serialized.FindProperty("knockback").floatValue = knockback;
        serialized.FindProperty("hitboxSize").vector2Value = new Vector2(width, height);
        serialized.FindProperty("hitboxOffset").vector2Value = new Vector2(offset, verticalOffset);
        serialized.FindProperty("damageType").enumValueIndex = (int)type;
        serialized.FindProperty("allowRollCancel").boolValue = rollCancel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    static void CreateHurtboxLayers() {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (string name in new[] { "PlayerHurtbox", "EnemyHurtbox" }) {
            int layer = LayerMask.NameToLayer(name);
            if (layer < 0) {
                layer = Enumerable.Range(8, 24).FirstOrDefault(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
                if (layer == 0) throw new InvalidOperationException("No free layer for " + name);
                layers.GetArrayElementAtIndex(layer).stringValue = name;
                tags.ApplyModifiedProperties();
            }
            // Hurtboxes are query-only triggers. The explicit overlap filter supplies the target mask.
            for (int i = 0; i < 32; i++) Physics2D.IgnoreLayerCollision(layer, i);
        }
    }
}
}
