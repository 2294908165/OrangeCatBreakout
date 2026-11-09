using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace OrangeCat.Editor {
[InitializeOnLoad]
public static class PhaseOneBuilder {
    const string ScenePath = "Assets/Game/Scenes/PhaseOne.unity";
    static PhaseOneBuilder() { EditorApplication.delayCall += AutoBuild; }
    static void AutoBuild() {
        if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            Build();
    }
    [MenuItem("Orange Cat/Build Phase 0-1 Scene")]
    public static void Build() {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before building.");
        // Never discard an unsaved scene or overwrite an existing deliverable.
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        ConfigureSettings();
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Game/Data/PlayerControls.inputactions");
        if (inputAsset == null) throw new InvalidOperationException("Input actions must finish importing first.");
        var sprite = CreateSprite();
        var hao = Definition("HaoJu"); Definition("JiangZhua");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("HaoJu"); player.SetActive(false);
        player.layer = LayerMask.NameToLayer("Player"); player.transform.position = new Vector3(-6, 1, 0);
        var rb = player.AddComponent<Rigidbody2D>(); rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var box = player.AddComponent<BoxCollider2D>(); box.size = new Vector2(.7f, 1.2f);
        var material = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0, bounciness = 0 };
        string materialPath = "Assets/Game/Data/PlayerNoFriction.physicsMaterial2D";
        if (!File.Exists(materialPath)) AssetDatabase.CreateAsset(material, materialPath);
        else UnityEngine.Object.DestroyImmediate(material);
        box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
        var visual = new GameObject("Visual"); visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = new Vector3(.7f,1.2f,1);
        var sr = visual.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = new Color(1,.48f,.10f);
        // Asymmetric facing marker, flips with the sprite in future art replacement.
        var reader = player.AddComponent<PlayerInputReader>(); reader.Configure(inputAsset);
        var motor = player.AddComponent<PlayerMotor2D>(); motor.Configure(hao, reader, sr, LayerMask.GetMask("Ground"));
        player.AddComponent<PlayerDebugHUD>().Configure(motor);
        SavePrefab(player, "HaoJu");
        motor.Configure(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Game/Data/JiangZhua.asset"),reader,sr,LayerMask.GetMask("Ground"));
        sr.color = new Color(.85f,.6f,.2f); SavePrefab(player,"JiangZhua");
        motor.Configure(hao,reader,sr,LayerMask.GetMask("Ground")); sr.color = new Color(1,.48f,.10f);
        player.SetActive(true);
        var manager = new GameObject("GameManager").AddComponent<GameManager>(); manager.Configure(reader);
        var cameraGO = new GameObject("Main Camera"); cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5;
        camera.backgroundColor = new Color(.12f,.17f,.22f); camera.clearFlags = CameraClearFlags.SolidColor;
        cameraGO.transform.position = new Vector3(-6,1,-10); cameraGO.AddComponent<CameraFollow2D>().Configure(player.transform);
        Ground("Floor", new Vector2(0,-1),new Vector2(24,1),sprite);
        Ground("JumpPlatform",new Vector2(3,1),new Vector2(3,.5f),sprite);
        Ground("HighPlatform",new Vector2(8,3),new Vector2(3,.5f),sprite);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Phase 0/1 scene and prefabs created. Open PhaseOne and press Play.");
    }
    static void SavePrefab(GameObject player,string name) {
        string path = "Assets/Game/Prefabs/"+name+".prefab";
        if (!File.Exists(path)) { player.SetActive(true); PrefabUtility.SaveAsPrefabAsset(player,path); player.SetActive(false); }
    }
    static CharacterDefinition Definition(string name) {
        string path = "Assets/Game/Data/"+name+".asset";
        var asset = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<CharacterDefinition>();
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("displayName").stringValue = name == "HaoJu" ? "哈橘" : "姜爪";
        serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(asset,path); return asset;
    }
    internal static Sprite CreateSprite() {
        const string path = "Assets/Game/Art/Placeholder.png";
        if (!File.Exists(path)) {
            var texture = new Texture2D(16,16);
            var colors = new Color[256]; for(int i=0;i<colors.Length;i++) colors[i] = i%16 > 11 && i/16 > 9 ? Color.black : Color.white;
            texture.SetPixels(colors); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    internal static void Ground(string name,Vector2 position,Vector2 size,Sprite sprite) {
        var go = new GameObject(name); go.layer = LayerMask.NameToLayer("Ground"); go.transform.position = position;
        go.transform.localScale = size; go.AddComponent<BoxCollider2D>();
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = new Color(.3f,.4f,.35f);
    }
    internal static void ConfigureSettings() {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (string name in new[]{"Ground","Player","Enemy","PlayerHitbox","EnemyHitbox","Hazard"}) {
            if (LayerMask.NameToLayer(name)>=0) continue;
            bool added = false;
            for(int i=8;i<32;i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) {
                layers.GetArrayElementAtIndex(i).stringValue = name; tags.ApplyModifiedProperties(); added = true; break;
            }
            if (!added) throw new InvalidOperationException("No free physics layer for "+name);
        }
        int ph = LayerMask.NameToLayer("PlayerHitbox"), eh = LayerMask.NameToLayer("EnemyHitbox");
        for(int i=0;i<32;i++) {
            Physics2D.IgnoreLayerCollision(ph,i,i!=LayerMask.NameToLayer("Enemy"));
            Physics2D.IgnoreLayerCollision(eh,i,i!=LayerMask.NameToLayer("Player"));
        }
        Physics2D.IgnoreLayerCollision(ph,eh,true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Enemy"),true);
        Time.fixedDeltaTime = .02f;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var activeInput = settings.FindProperty("activeInputHandler");
        if(activeInput != null) { activeInput.intValue = 1; settings.ApplyModifiedProperties(); }
    }
}
}
