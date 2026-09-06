using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OutpostBuilder
{
    public const string ScenePath = "Assets/Scenes/Outpost.unity";
    private const string ArtPath = "Assets/Outpost";
    private static Material concrete, dark, teal, orange, sand, white;

    [MenuItem("GunQuest/Outpost/Generate playable outpost")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(ArtPath);
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/OutpostTracer.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")), "Assets/Resources/OutpostTracer.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        concrete = Material("Concrete", new Color(0.25f, 0.32f, 0.32f));
        dark = Material("Graphite", new Color(0.075f, 0.11f, 0.13f));
        sand = Material("Sand", new Color(0.43f, 0.40f, 0.31f));
        teal = Material("Signal teal", new Color(0.15f, 0.75f, 0.61f), true);
        orange = Material("Signal amber", new Color(1f, 0.32f, 0.08f), true);
        white = Material("Markings", new Color(0.75f, 0.81f, 0.72f));
        var geometry = new GameObject("Outpost / architecture").transform;
        Block("Foundation", geometry, new Vector3(0, -0.6f, 0), new Vector3(60, 1, 60), sand);
        Block("Landing apron", geometry, new Vector3(0, -0.07f, 0), new Vector3(35, 0.1f, 39), concrete);
        for (int side = -1; side <= 1; side += 2)
        {
            Block("Perimeter east-west", geometry, new Vector3(side * 29, 1.5f, 0), new Vector3(1, 3, 59), concrete);
            Block("Perimeter north-south", geometry, new Vector3(0, 1.5f, side * 29), new Vector3(59, 3, 1), concrete);
            for (int j = -24; j <= 24; j += 8)
            {
                Block("Wall buttress", geometry, new Vector3(side * 28.4f, 2, j), new Vector3(1.2f, 4, 1.2f), dark);
                Block("Perimeter beacon", geometry, new Vector3(side * 28.4f, 4.1f, j), new Vector3(0.8f, 0.16f, 0.8f), teal, false);
            }
        }
        // Offset cover leaves three connected lanes and several flanking routes.
        for (int side = -1; side <= 1; side += 2)
        {
            Block("Cargo module", geometry, new Vector3(side * 11, 1.7f, 5), new Vector3(5, 3.4f, 9), dark);
            Block("Cargo stripe", geometry, new Vector3(side * 11, 2.6f, 0.46f), new Vector3(4.8f, 0.3f, 0.06f), teal, false);
            for (int j = 0; j < 6; j++) Block("Cargo rib", geometry, new Vector3(side * 11, 1.7f, 1 + j * 1.5f), new Vector3(5.15f, 3.55f, 0.08f), concrete);
            Block("Low barricade", geometry, new Vector3(side * 5.5f, 0.65f, -8), new Vector3(5.5f, 1.3f, 1), concrete);
            Block("Barricade cap", geometry, new Vector3(side * 5.5f, 1.33f, -8), new Vector3(5.7f, 0.08f, 1.1f), white, false);
            Block("Forward barricade", geometry, new Vector3(side * 4, 0.7f, 15), new Vector3(4, 1.4f, 1.2f), concrete);
            Military("Decorations/Box_003", geometry, new Vector3(side * 18, 0, -11), 2.4f, side * 14f);
            Military("Decorations/Barrel_005", geometry, new Vector3(side * 21, 0, 10), 1.8f, 0);
            Military("Buildings/Tower_003", geometry, new Vector3(side * 25, 0, 24), 5f, 0);
        }
        Military("Vehicles/Hummer_003", geometry, new Vector3(-20, 0, 1), 5.5f, -15);
        Military("Buildings/Radiostation_001", geometry, new Vector3(19, 0, 18), 6f, 180);
        Military("Buildings/Tent_002", geometry, new Vector3(-19, 0, 17), 6f, 90);
        // Central signal tower is the arena's landmark, readable from every lane.
        Block("Relay base", geometry, new Vector3(0, 0.6f, 4), new Vector3(3, 1.2f, 3), dark);
        Block("Relay mast", geometry, new Vector3(0, 5, 4), new Vector3(0.5f, 9, 0.5f), concrete);
        for (int i = 0; i < 3; i++) Block("Relay signal", geometry, new Vector3(0, 7 + i, 4), new Vector3(3 - i * 0.6f, 0.12f, 0.3f), teal, false);
        WorldText("OUTPOST / 07", geometry, new Vector3(0, 4.3f, 28.35f), 0.12f, white.color);
        WorldText("HOLD THE PERIMETER", geometry, new Vector3(0, 3.3f, 28.35f), 0.045f, teal.color);
        for (int i = -12; i <= 12; i += 4)
        {
            Block("Road dash", geometry, new Vector3(0, 0.005f, i), new Vector3(0.15f, 0.012f, 1.6f), white, false);
            Block("Apron edge", geometry, new Vector3(i, 0.005f, -19), new Vector3(2, 0.012f, 0.12f), white, false);
        }
        for (int i = 0; i < 14; i++)
        {
            float angle = i * Mathf.PI * 2f / 14f;
            var hill = Block("Distant rock formation", null, new Vector3(Mathf.Sin(angle) * 85f, -4f, Mathf.Cos(angle) * 85f), new Vector3(27, 22 + i % 4 * 8, 25), concrete, false);
            hill.transform.rotation = Quaternion.Euler(10 + i * 2, i * 31, 18);
        }

        var light = new GameObject("Late afternoon sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.85f, 0.65f);
        light.intensity = 2.2f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(38, -35, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.58f, 0.67f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.32f, 0.43f, 0.46f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 145f;
        var volume = new GameObject("Atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<Bloom>().intensity.Override(0.3f);
        profile.Add<Vignette>().intensity.Override(0.22f);
        var grading = profile.Add<ColorAdjustments>();
        grading.contrast.Override(12f);
        grading.saturation.Override(-8f);
        SaveAsset(profile, ArtPath + "/Atmosphere.asset");
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArtPath + "/Atmosphere.asset");

        var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        SaveAsset(surface.navMeshData, ArtPath + "/OutpostNavMesh.asset");
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(ArtPath + "/OutpostNavMesh.asset");

        var player = CreatePlayer();
        var session = new GameObject("Operation director").AddComponent<GameSession>();
        session.player = player.GetComponent<PlayerHealth>();
        session.weapon = player.GetComponent<PlayerWeapon>();
        session.enemyPrefab = CreateEnemy();
        session.spawnPoints = new Transform[4];
        Vector3[] spawns = { new Vector3(-24, 0, -23), new Vector3(24, 0, -23), new Vector3(-8, 0, 24), new Vector3(8, 0, 24) };
        for (int i = 0; i < spawns.Length; i++)
        {
            var spawn = new GameObject("Hostile entry " + (i + 1)).transform;
            spawn.position = spawns[i];
            session.spawnPoints[i] = spawn;
            Block("Entry warning", null, spawns[i] + Vector3.up * 0.025f, new Vector3(3, 0.03f, 3), orange, false);
        }
        session.gameObject.AddComponent<GameHud>().session = session;
        Pickup(new Vector3(-17, 0.9f, -18), true);
        Pickup(new Vector3(17, 0.9f, -18), false);
        Pickup(new Vector3(-17, 0.9f, 11), false);
        Pickup(new Vector3(17, 0.9f, 11), true);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };
        PlayerSettings.productName = "GunQuest - Outpost";
        PlayerSettings.companyName = "GunQuest";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
        Debug.Log("GUNQUEST OUTPOST GENERATED: " + ScenePath);
    }

    private static GameObject CreatePlayer()
    {
        var player = new GameObject("Operator") { tag = "Player" };
        player.transform.position = new Vector3(0, 0.2f, -23);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2;
        controller.center = Vector3.up;
        controller.radius = 0.35f;
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0, 1.7f, 0);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 220f;
        cam.fieldOfView = 75f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        var motor = player.AddComponent<PlayerMotor>();
        motor.jumpHeight = 1.25f;
        motor.gravity = -22f;
        motor.crouchDuration = 0.16f;
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerLook>().cam = cam;
        var weapon = player.AddComponent<PlayerWeapon>();
        weapon.aimCamera = cam;
        player.AddComponent<InputManager>();
        var gun = new GameObject("GQ-30 rifle").transform;
        gun.SetParent(cam.transform, false);
        gun.localPosition = new Vector3(0.3f, -0.28f, 0.55f);
        Block("Receiver", gun, Vector3.zero, new Vector3(0.14f, 0.16f, 0.48f), dark, false);
        Block("Foregrip", gun, new Vector3(0, -0.025f, 0.27f), new Vector3(0.12f, 0.12f, 0.3f), concrete, false);
        Block("Barrel", gun, new Vector3(0, 0.02f, 0.5f), new Vector3(0.055f, 0.055f, 0.25f), dark, false);
        Block("Magazine", gun, new Vector3(0, -0.16f, -0.05f), new Vector3(0.09f, 0.23f, 0.14f), concrete, false);
        Block("Stock", gun, new Vector3(0, -0.025f, -0.34f), new Vector3(0.105f, 0.14f, 0.24f), concrete, false);
        Block("Sight left", gun, new Vector3(-0.045f, 0.135f, 0), new Vector3(0.014f, 0.1f, 0.04f), dark, false);
        Block("Sight right", gun, new Vector3(0.045f, 0.135f, 0), new Vector3(0.014f, 0.1f, 0.04f), dark, false);
        Block("Sight top", gun, new Vector3(0, 0.18f, 0), new Vector3(0.1f, 0.014f, 0.04f), dark, false);
        Block("Charge indicator", gun, new Vector3(0.071f, 0.02f, -0.08f), new Vector3(0.006f, 0.035f, 0.2f), teal, false);
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun, false);
        muzzle.localPosition = new Vector3(0, 0.02f, 0.64f);
        weapon.muzzle = muzzle;
        var presentation = player.AddComponent<WeaponPresentation>();
        presentation.weapon = weapon;
        presentation.viewModel = gun;
        return player;
    }

    private static Enemy CreateEnemy()
    {
        var root = new GameObject("Sentinel");
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 1, 0);
        collider.height = 2;
        collider.radius = 0.45f;
        Block("Armored torso", root.transform, new Vector3(0, 1.15f, 0), new Vector3(0.85f, 0.7f, 0.5f), dark, false);
        Block("Helmet", root.transform, new Vector3(0, 1.8f, 0), new Vector3(0.48f, 0.42f, 0.45f), concrete, false);
        Block("Visor", root.transform, new Vector3(0, 1.84f, 0.235f), new Vector3(0.39f, 0.075f, 0.035f), orange, false);
        Block("Reactor", root.transform, new Vector3(0, 1.3f, 0.27f), new Vector3(0.2f, 0.2f, 0.035f), orange, false);
        for (int side = -1; side <= 1; side += 2)
        {
            Block("Leg", root.transform, new Vector3(side * 0.23f, 0.4f, 0), new Vector3(0.25f, 0.8f, 0.32f), concrete, false);
            Block("Shoulder", root.transform, new Vector3(side * 0.53f, 1.4f, 0), new Vector3(0.25f, 0.3f, 0.38f), concrete, false);
        }
        var barrel = Block("Pulse weapon", root.transform, new Vector3(0.5f, 1.1f, 0.45f), new Vector3(0.18f, 0.18f, 0.7f), dark, false).transform;
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform, false);
        muzzle.localPosition = new Vector3(0.5f, 1.1f, 0.85f);
        var agent = root.AddComponent<NavMeshAgent>();
        agent.height = 2;
        agent.radius = 0.45f;
        agent.stoppingDistance = 7f;
        agent.angularSpeed = 240f;
        root.AddComponent<EnemyHealth>();
        var enemy = root.AddComponent<Enemy>();
        enemy.gunBarrel = muzzle;
        enemy.sightDistance = 45f;
        enemy.fieldOfView = 150f;
        enemy.huntPlayer = true;
        enemy.bulletPrefab = TutorialMapBuilder.CreateBulletPrefab();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ArtPath + "/Sentinel.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Enemy>();
    }

    private static void Pickup(Vector3 position, bool heals)
    {
        var root = new GameObject(heals ? "Field medkit" : "Ammo cache");
        root.transform.position = position;
        var trigger = root.AddComponent<SphereCollider>();
        trigger.radius = 1.4f;
        trigger.isTrigger = true;
        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        root.AddComponent<SupplyPickup>().heals = heals;
        Block("Supply crate", root.transform, Vector3.zero, new Vector3(0.65f, 0.45f, 0.45f), dark, false);
        Block("Supply symbol", root.transform, new Vector3(0, 0, -0.24f), new Vector3(0.4f, 0.08f, 0.02f), heals ? teal : orange, false);
        if (heals) Block("Cross", root.transform, new Vector3(0, 0, -0.25f), new Vector3(0.08f, 0.3f, 0.02f), teal, false);
        WorldText(heals ? "+ HEALTH" : "+ AMMO", root.transform, new Vector3(0, 0.7f, 0), 0.022f, heals ? teal.color : orange.color);
    }

    private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collides = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collides) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static void Military(string asset, Transform parent, Vector3 position, float width, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DL/ithappy/Military_Free/Prefabs/" + asset + ".prefab");
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        float scale = width / Mathf.Max(bounds.size.x, bounds.size.z, 0.01f);
        go.transform.localScale *= scale;
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(bounds.size.x / go.transform.lossyScale.x, bounds.size.y / go.transform.lossyScale.y, bounds.size.z / go.transform.lossyScale.z);
        go.transform.position = position + Vector3.up * -bounds.min.y;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    private static void WorldText(string text, Transform parent, Vector3 position, float size, Color color)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 64;
        label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = color;
    }

    private static Material Material(string name, Color color, bool emissive = false)
    {
        string path = ArtPath + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.3f);
        if (emissive) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.5f); }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SaveAsset(Object asset, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (existing == null) AssetDatabase.CreateAsset(asset, path);
        else { EditorUtility.CopySerialized(asset, existing); EditorUtility.SetDirty(existing); }
    }

    [MenuItem("GunQuest/Outpost/Build Windows player")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath)) Generate();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/GunQuest.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
        Debug.Log("GUNQUEST WINDOWS BUILD PASSED: " + report.summary.totalSize + " bytes");
    }
}
