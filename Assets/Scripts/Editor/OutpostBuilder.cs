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
    public const string BlackwoodScenePath = "Assets/Scenes/Blackwood.unity";
    public const string SkylineScenePath = "Assets/Scenes/Skyline.unity";
    private const string ArtPath = "Assets/Outpost";
    private const string AncientPath = "Assets/DL/POLYART_Ancient Village/Prefabs/";
    private const string CityPath = "Assets/DL/ithappy/Cartoon_City_Free/Prefabs/";
    private const string PolyHavenPath = "Assets/ThirdParty/PolyHaven/";
    private const string WarriorPath = "Assets/DL/SciFiWarriorPBRHPPolyart/Prefabs/PBRCharacter.prefab";
    private const string WarriorMaterialPath = "Assets/DL/SciFiWarriorPBRHPPolyart/Materials/PBR.mat";
    private static Material concrete, dark, teal, orange, sand, white;

    [MenuItem("GunQuest/Outpost/Generate playable outpost")]
    public static void Generate()
    {
        EnvironmentRebuild.Generate();
    }

    public static void GenerateLegacy()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(ArtPath);
        AssetDatabase.Refresh();
        ConfigureImportedTextures();
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/OutpostTracer.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")), "Assets/Resources/OutpostTracer.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        concrete = Material("Concrete", new Color(0.25f, 0.32f, 0.32f));
        dark = Material("Graphite", new Color(0.075f, 0.11f, 0.13f));
        sand = PbrMaterial("Outpost compacted earth", new Color(0.48f, 0.43f, 0.34f),
            PolyHavenPath + "Materials/Dirt/dirt_diff_4k.jpg", PolyHavenPath + "Materials/Dirt/dirt_nor_dx_4k.jpg", 10f, 0.12f);
        var landingSurface = PbrMaterial("Outpost concrete surface", new Color(0.72f, 0.76f, 0.73f),
            PolyHavenPath + "Materials/Concrete/concrete_pavement_diff_4k.jpg", PolyHavenPath + "Materials/Concrete/concrete_pavement_nor_dx_4k.jpg", 9f, 0.26f);
        teal = Material("Signal teal", new Color(0.15f, 0.75f, 0.61f), true);
        orange = Material("Signal amber", new Color(1f, 0.32f, 0.08f), true);
        white = Material("Markings", new Color(0.75f, 0.81f, 0.72f));
        var geometry = new GameObject("Outpost / architecture").transform;
        Block("Foundation", geometry, new Vector3(0, -0.6f, 0), new Vector3(60, 1, 60), sand);
        Block("Landing apron", geometry, new Vector3(0, -0.07f, 0), new Vector3(35, 0.1f, 39), landingSurface);
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
        Military("Vehicles/Tank_006", geometry, new Vector3(-20, 0, 13), 7.5f, 24f);
        Military("Vehicles/Helicopter_001", geometry, new Vector3(20, 0, -14), 10f, -32f);
        Military("Vehicles/Generator_004", geometry, new Vector3(18, 0, 7), 3.2f, 90f);
        Military("Decorations/Barrier_015", geometry, new Vector3(-7, 0, 21), 4.2f, 8f);
        Military("Decorations/Barrier_009", geometry, new Vector3(8, 0, 21), 4.2f, -8f);
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
        SetupSkybox("OutpostSky", PolyHavenPath + "HDRI/kloppenheim_06_puresky_4k.hdr", 0.82f, 118f);
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.32f, 0.43f, 0.46f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0022f;
        var volume = new GameObject("Atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = PrepareVolumeProfile(ArtPath + "/Atmosphere.asset");
        ConfigurePostProcessing(profile, 0.38f, 0.20f, 13f, -3f, 0.12f, 0.035f);
        volume.sharedProfile = profile;
        AccentLight("Relay bounce", new Vector3(0, 5.2f, 4), teal.color, 4.5f, 19f);
        AccentLight("West perimeter fill", new Vector3(-17, 4f, -2), new Color(0.26f, 0.62f, 1f), 2.8f, 15f);
        AccentLight("East perimeter fill", new Vector3(17, 4f, 10), new Color(1f, 0.38f, 0.14f), 2.4f, 14f);
        AtmosphericParticles("Outpost dust", new Vector3(0, 5f, 0), new Vector3(57, 10, 57),
            new Color(1f, 0.72f, 0.38f, 0.28f), 240, 0.035f, 0.12f, false);

        var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        SaveAsset(surface.navMeshData, ArtPath + "/OutpostNavMesh.asset");
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(ArtPath + "/OutpostNavMesh.asset");

        var player = CreatePlayer(new Vector3(0, 0.2f, -23));
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
        GenerateBlackwood();
        GenerateSkyline();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(BlackwoodScenePath, true),
            new EditorBuildSettingsScene(SkylineScenePath, true)
        };
        PlayerSettings.productName = "GunQuest";
        PlayerSettings.companyName = "GunQuest";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
        Debug.Log("GUNQUEST CAMPAIGN GENERATED: Outpost, Blackwood, Skyline.");
    }

    private static void GenerateBlackwood()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var ground = PbrMaterial("Blackwood earth", new Color(0.34f, 0.42f, 0.31f),
            PolyHavenPath + "Materials/Dirt/dirt_diff_4k.jpg", PolyHavenPath + "Materials/Dirt/dirt_nor_dx_4k.jpg", 12f, 0.08f);
        var moss = PbrMaterial("Blackwood moss trail", new Color(0.30f, 0.46f, 0.27f),
            PolyHavenPath + "Materials/Dirt/dirt_diff_4k.jpg", PolyHavenPath + "Materials/Dirt/dirt_nor_dx_4k.jpg", 9f, 0.10f);
        var stone = Material("Blackwood stone", new Color(0.25f, 0.31f, 0.27f));
        var wood = Material("Blackwood timber", new Color(0.20f, 0.12f, 0.07f));
        dark = Material("Blackwood shadow", new Color(0.035f, 0.06f, 0.05f));
        concrete = stone;
        sand = ground;
        teal = Material("Blackwood spirit", new Color(0.30f, 1f, 0.55f), true);
        orange = Material("Blackwood flame", new Color(1f, 0.28f, 0.05f), true);
        white = Material("Blackwood glyph", new Color(0.76f, 0.86f, 0.66f), true);

        var geometry = new GameObject("Blackwood / ancient village").transform;
        Block("Forest floor", geometry, new Vector3(0, -0.58f, 0), new Vector3(66, 1, 66), ground);
        Block("North-south trail", geometry, new Vector3(0, -0.035f, 0), new Vector3(9, 0.08f, 61), moss, false);
        Block("East-west trail", geometry, new Vector3(0, -0.03f, 2), new Vector3(61, 0.07f, 8), moss, false);

        // A ring of stone and trees keeps the combat space legible while selling a dense forest.
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            Vector3 edge = new Vector3(Mathf.Sin(angle) * 31f, 0, Mathf.Cos(angle) * 31f);
            Block("Forest ridge", geometry, edge + Vector3.up * 0.9f, new Vector3(6.2f, 2f, 2.4f), stone).transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            string tree = $"PA_TreeSet1_Pile{1 + i % 12}.prefab";
            Decor(AncientPath + tree, geometry, edge * 1.06f, 5.5f + i % 3, i * 31f, false);
        }

        Decor(AncientPath + "PA_BambooHouse_1.prefab", geometry, new Vector3(-16, 0, 10), 9f, 25f);
        Decor(AncientPath + "PA_BambooHouse_2.prefab", geometry, new Vector3(16, 0, 11), 9f, -20f);
        Decor(AncientPath + "PA_BambooHouse_2.prefab", geometry, new Vector3(-17, 0, -12), 8f, 145f);
        Decor(AncientPath + "PA_BambooBridge_1.prefab", geometry, new Vector3(13, 0, -12), 8f, 90f);
        Decor(AncientPath + "PA_RockSet2_Pile3.prefab", geometry, new Vector3(-8, 0, 1), 5.5f, 20f);
        Decor(AncientPath + "PA_RockSet1_Pile6.prefab", geometry, new Vector3(9, 0, 4), 4.8f, -35f);
        Decor(AncientPath + "PA_RockSet2_Pile1.prefab", geometry, new Vector3(4, 0, 15), 4.5f, 80f);
        Decor(AncientPath + "PA_RockSet1_Pile3.prefab", geometry, new Vector3(-5, 0, -15), 4.4f, -10f);
        for (int side = -1; side <= 1; side += 2)
        {
            Block("Fallen timber", geometry, new Vector3(side * 12, 0.65f, -1), new Vector3(5, 1.3f, 1.1f), wood).transform.rotation = Quaternion.Euler(0, side * 20, 4);
            Decor(AncientPath + "PA_Bamboolamp_Set1.prefab", geometry, new Vector3(side * 5, 0, -7), 2f, 0, false);
            Decor(AncientPath + "PA_Bamboolamp_Set2.prefab", geometry, new Vector3(side * 5, 0, 10), 2f, 180f, false);
            AccentLight("Village lantern", new Vector3(side * 5, 2.1f, -7), new Color(1f, 0.34f, 0.08f), 3.2f, 10f);
            AccentLight("Spirit lantern", new Vector3(side * 5, 2.1f, 10), teal.color, 2.8f, 9f);
        }

        // The spirit shrine is the strong central silhouette and a compact piece of hard cover.
        Cylinder("Shrine dais", geometry, new Vector3(0, 0.25f, 3), new Vector3(4.8f, 0.25f, 4.8f), stone);
        Cylinder("Shrine core", geometry, new Vector3(0, 1.45f, 3), new Vector3(1.3f, 1.3f, 1.3f), dark);
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            Block("Spirit glyph", geometry, new Vector3(Mathf.Sin(angle) * 1.35f, 2.9f, 3 + Mathf.Cos(angle) * 1.35f), new Vector3(0.13f, 1.8f, 0.45f), teal, false).transform.rotation = Quaternion.Euler(0, i * 120f, 18f);
        }
        var shrineLight = new GameObject("Shrine glow").AddComponent<Light>();
        shrineLight.type = LightType.Point;
        shrineLight.range = 18f;
        shrineLight.intensity = 7f;
        shrineLight.color = teal.color;
        shrineLight.transform.position = new Vector3(0, 3f, 3);
        WorldText("BLACKWOOD", geometry, new Vector3(0, 4.1f, 30.7f), 0.10f, white.color);
        WorldText("AWAKEN THE GROVE", geometry, new Vector3(0, 3.2f, 30.7f), 0.04f, teal.color);

        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Decor(AncientPath + $"PA_Mountainset1_hill{1 + i % 6}.prefab", null,
                new Vector3(Mathf.Sin(angle) * 100f, -5f, Mathf.Cos(angle) * 100f), 42f, i * 29f, false);
        }

        SetupSkybox("BlackwoodSky", PolyHavenPath + "HDRI/nature_reserve_forest_4k.hdr", 0.68f, 214f);
        SetupAtmosphere("Blackwood", new Color(0.36f, 0.48f, 0.37f), new Color(0.08f, 0.15f, 0.11f),
            new Color(0.55f, 0.70f, 0.58f), 2.05f, new Vector3(32, -48, 0), 26f, 108f, 0.46f, 0.27f, 17f, 0f);
        AtmosphericParticles("Blackwood fireflies", new Vector3(0, 4.5f, 1), new Vector3(55, 8, 55),
            new Color(0.30f, 1f, 0.48f, 0.72f), 135, 0.055f, 0.18f, false);
        BakeNavigation(geometry, ArtPath + "/BlackwoodNavMesh.asset");
        CreateMissionActors("02", "BLACKWOOD",
            "A forgotten village beneath a poisoned canopy.\nBreak the signal feeding the awakened grove.",
            "The corrupted signal is silent.\nBlackwood can breathe again.", teal.color,
            new Vector3(0, 0.2f, -25),
            new[] { new Vector3(-25, 0, -23), new Vector3(25, 0, -23), new Vector3(-23, 0, 24), new Vector3(23, 0, 24) },
            new[] { new Vector3(-18, 0.9f, -20), new Vector3(18, 0.9f, -19), new Vector3(-20, 0.9f, 18), new Vector3(20, 0.9f, 18) });
        EditorSceneManager.SaveScene(scene, BlackwoodScenePath);
    }

    private static void GenerateSkyline()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var asphalt = PbrMaterial("Skyline asphalt", new Color(0.22f, 0.25f, 0.33f),
            PolyHavenPath + "Materials/Asphalt/asphalt_01_diff_4k.jpg", PolyHavenPath + "Materials/Asphalt/asphalt_01_nor_dx_4k.jpg", 14f, 0.18f);
        var sidewalk = PbrMaterial("Skyline sidewalk", new Color(0.58f, 0.61f, 0.67f),
            PolyHavenPath + "Materials/Concrete/concrete_pavement_diff_4k.jpg", PolyHavenPath + "Materials/Concrete/concrete_pavement_nor_dx_4k.jpg", 10f, 0.24f);
        dark = Material("Skyline midnight", new Color(0.018f, 0.025f, 0.055f));
        concrete = sidewalk;
        sand = asphalt;
        teal = Material("Skyline cyan", new Color(0.05f, 0.85f, 1f), true);
        orange = Material("Skyline magenta", new Color(1f, 0.08f, 0.48f), true);
        white = Material("Skyline lane paint", new Color(0.78f, 0.84f, 0.92f));

        var geometry = new GameObject("Skyline / midnight district").transform;
        Block("City foundation", geometry, new Vector3(0, -0.6f, 0), new Vector3(72, 1, 72), asphalt);
        Block("Central avenue", geometry, new Vector3(0, -0.035f, 0), new Vector3(14, 0.08f, 68), dark, false);
        Block("Cross street", geometry, new Vector3(0, -0.03f, 2), new Vector3(68, 0.07f, 14), dark, false);
        for (int i = -30; i <= 30; i += 6)
        {
            Block("Avenue marker", geometry, new Vector3(0, 0.015f, i), new Vector3(0.18f, 0.025f, 2.4f), white, false);
            Block("Cross-street marker", geometry, new Vector3(i, 0.018f, 2), new Vector3(2.4f, 0.025f, 0.18f), white, false);
        }

        string[] buildings =
        {
            "Buildings/Eco_Building_Grid_NightLight.prefab",
            "Buildings/Eco_Building_Terrace_NightLight.prefab",
            "Buildings/Regular_Building_TwistedTower_Large_NightLight.prefab"
        };
        for (int side = -1; side <= 1; side += 2)
        {
            for (int row = -2; row <= 2; row++)
            {
                float z = row * 13f;
                Decor(CityPath + buildings[Mathf.Abs(row + side) % buildings.Length], geometry, new Vector3(side * 26f, 0, z), 12f, side < 0 ? 90 : -90);
            }
            Block("Raised sidewalk", geometry, new Vector3(side * 11f, 0.15f, 0), new Vector3(7.5f, 0.3f, 69), sidewalk);
            for (int z = -26; z <= 26; z += 13)
            {
                Color stripColor = (z / 13) % 2 == 0 ? teal.color : orange.color;
                Block("Sidewalk light", geometry, new Vector3(side * 8f, 0.34f, z), new Vector3(0.18f, 0.08f, 3.8f), (z / 13) % 2 == 0 ? teal : orange, false);
                AccentLight("Avenue neon pool", new Vector3(side * 8f, 2.3f, z), stripColor, 3.4f, 11f);
            }
        }
        Block("North perimeter", geometry, new Vector3(0, 1.4f, 34), new Vector3(70, 2.8f, 1), sidewalk);
        Block("South perimeter", geometry, new Vector3(0, 1.4f, -34), new Vector3(70, 2.8f, 1), sidewalk);

        Decor(CityPath + "Cars/Van.prefab", geometry, new Vector3(-4.2f, 0, -11), 5.5f, 10f);
        Decor(CityPath + "Cars/Futuristic_Car_1.prefab", geometry, new Vector3(4.3f, 0, 15), 5f, 188f);
        Decor(CityPath + "Cars/Car_19.prefab", geometry, new Vector3(17, 0, 2), 4.8f, 88f);
        Decor(CityPath + "Cars/Car_06.prefab", geometry, new Vector3(-17, 0, 3), 4.8f, -92f);
        Decor(CityPath + "Props/Bus_Stop_02.prefab", geometry, new Vector3(-8, 0, 22), 5f, 180f);
        Decor(CityPath + "Props/Fountain_03.prefab", geometry, new Vector3(15, 0, -16), 5f, 0);
        for (int side = -1; side <= 1; side += 2)
        {
            Decor(CityPath + "Props/traffic_light_003.prefab", geometry, new Vector3(side * 8, 0, -5), 2.2f, side * 90f, false);
            Decor(CityPath + "Billboards/Billboard_4x1_04_Line.prefab", geometry, new Vector3(side * 20, 0, -25), 7f, side * -90f, false);
        }

        // A hacked broadcast node turns the central junction into the visual and tactical objective.
        Cylinder("Broadcast node", geometry, new Vector3(0, 1.25f, 2), new Vector3(1.6f, 1.25f, 1.6f), dark);
        for (int i = 0; i < 4; i++)
            Block("Broadcast blade", geometry, new Vector3(Mathf.Sin(i * Mathf.PI * 0.5f) * 1.7f, 2.6f, 2 + Mathf.Cos(i * Mathf.PI * 0.5f) * 1.7f), new Vector3(0.16f, 2.5f, 0.55f), i % 2 == 0 ? teal : orange, false).transform.rotation = Quaternion.Euler(0, i * 90, 25);
        var nodeLight = new GameObject("Broadcast glow").AddComponent<Light>();
        nodeLight.type = LightType.Point;
        nodeLight.range = 21;
        nodeLight.intensity = 9;
        nodeLight.color = teal.color;
        nodeLight.transform.position = new Vector3(0, 4, 2);
        WorldText("SKYLINE / ZERO", geometry, new Vector3(0, 5f, 33.3f), 0.095f, white.color);
        WorldText("CUT THE BROADCAST", geometry, new Vector3(0, 4.1f, 33.3f), 0.04f, orange.color);

        SetupSkybox("SkylineSky", PolyHavenPath + "HDRI/satara_night_4k.hdr", 0.83f, 36f);
        SetupAtmosphere("Skyline", new Color(0.08f, 0.10f, 0.24f), new Color(0.018f, 0.025f, 0.075f),
            new Color(0.30f, 0.40f, 0.76f), 1.15f, new Vector3(55, 28, 0), 34f, 125f, 0.72f, 0.31f, 22f, -4f);
        AtmosphericParticles("Skyline rain", new Vector3(0, 10f, 0), new Vector3(68, 18, 68),
            new Color(0.30f, 0.62f, 1f, 0.25f), 850, 0.022f, 8.5f, true);
        BakeNavigation(geometry, ArtPath + "/SkylineNavMesh.asset");
        CreateMissionActors("03", "SKYLINE",
            "A blackout district under hostile control.\nFight block by block and cut the broadcast.",
            "The enemy network has gone dark.\nThe city belongs to its people again.", teal.color,
            new Vector3(0, 0.2f, -28),
            new[] { new Vector3(-7, 0, -30), new Vector3(7, 0, -30), new Vector3(-7, 0, 30), new Vector3(7, 0, 30), new Vector3(-21, 0.4f, 25), new Vector3(21, 0.4f, -24) },
            new[] { new Vector3(-7, 0.9f, -19), new Vector3(7, 0.9f, -18), new Vector3(-7, 0.9f, 20), new Vector3(7, 0.9f, 21) });
        EditorSceneManager.SaveScene(scene, SkylineScenePath);
    }

    internal static void CreateMissionActors(string code, string name, string description, string victory, Color accent,
        Vector3 playerStart, Vector3[] spawns, Vector3[] supplies)
    {
        var player = CreatePlayer(playerStart);
        var session = new GameObject("Operation director").AddComponent<GameSession>();
        session.missionCode = code;
        session.missionName = name;
        session.missionDescription = description;
        session.victoryDescription = victory;
        session.missionAccent = accent;
        session.player = player.GetComponent<PlayerHealth>();
        session.weapon = player.GetComponent<PlayerWeapon>();
        session.enemyPrefab = CreateEnemy();
        session.spawnPoints = new Transform[spawns.Length];
        for (int i = 0; i < spawns.Length; i++)
        {
            var spawn = new GameObject("Hostile entry " + (i + 1)).transform;
            spawn.position = spawns[i];
            session.spawnPoints[i] = spawn;
            Block("Entry warning", null, spawns[i] + Vector3.up * 0.025f, new Vector3(3, 0.03f, 3), orange, false);
        }
        session.gameObject.AddComponent<GameHud>().session = session;
        for (int i = 0; i < supplies.Length; i++) Pickup(supplies[i], i % 2 == 0);
    }

    private static void SetupAtmosphere(string name, Color ambient, Color fog, Color sunColor, float sunIntensity,
        Vector3 sunRotation, float fogStart, float fogEnd, float bloom, float vignette, float contrast, float saturation)
    {
        var light = new GameObject(name + " key light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = sunColor;
        light.intensity = sunIntensity;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(sunRotation);
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = Mathf.Max(0.35f, ambient.maxColorComponent * 1.45f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = name == "Blackwood" ? 0.0065f : 0.0032f;
        var volume = new GameObject(name + " atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = PrepareVolumeProfile(ArtPath + "/" + name + "Atmosphere.asset");
        ConfigurePostProcessing(profile, bloom, vignette, contrast, saturation,
            name == "Skyline" ? 0.20f : 0.34f, name == "Skyline" ? 0.06f : 0.045f);
        volume.sharedProfile = profile;
    }

    internal static void BakeNavigation(Transform geometry, string assetPath)
    {
        var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        SaveAsset(surface.navMeshData, assetPath);
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
    }

    private static GameObject CreatePlayer(Vector3 start)
    {
        var player = new GameObject("Operator") { tag = "Player" };
        player.transform.position = start;
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2;
        controller.center = Vector3.up;
        controller.radius = 0.35f;
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0, 1.7f, 0);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        cam.fieldOfView = 75f;
        cam.clearFlags = CameraClearFlags.Skybox;
        var cameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
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
        gun.localPosition = new Vector3(0.30f, -0.31f, 0.58f);
        var rifleMesh = CreateBakedRifleMesh();
        if (rifleMesh != null)
        {
            var rifleModel = new GameObject("PBR rifle geometry", typeof(MeshFilter), typeof(MeshRenderer));
            rifleModel.transform.SetParent(gun, false);
            rifleModel.GetComponent<MeshFilter>().sharedMesh = rifleMesh;
            rifleModel.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WarriorMaterialPath);
            Vector3 meshSize = rifleMesh.bounds.size;
            float longest = Mathf.Max(meshSize.x, meshSize.y, meshSize.z);
            Quaternion orientation = meshSize.x >= meshSize.y && meshSize.x >= meshSize.z
                ? Quaternion.Euler(0, -90f, 0)
                : meshSize.y >= meshSize.z ? Quaternion.Euler(90f, 0, 0) : Quaternion.identity;
            float modelScale = 0.88f / Mathf.Max(0.01f, longest);
            rifleModel.transform.localRotation = orientation;
            rifleModel.transform.localScale = Vector3.one * modelScale;
            rifleModel.transform.localPosition = -(orientation * rifleMesh.bounds.center) * modelScale + new Vector3(0, 0, 0.06f);
            Block("Holographic sight", gun, new Vector3(0, 0.08f, 0.08f), new Vector3(0.062f, 0.008f, 0.05f), teal, false);
            Block("Sight shroud left", gun, new Vector3(-0.039f, 0.05f, 0.08f), new Vector3(0.009f, 0.06f, 0.055f), dark, false);
            Block("Sight shroud right", gun, new Vector3(0.039f, 0.05f, 0.08f), new Vector3(0.009f, 0.06f, 0.055f), dark, false);
        }
        else
        {
            Block("Receiver", gun, Vector3.zero, new Vector3(0.14f, 0.16f, 0.48f), dark, false);
            Block("Foregrip", gun, new Vector3(0, -0.025f, 0.27f), new Vector3(0.12f, 0.12f, 0.3f), concrete, false);
            Block("Barrel", gun, new Vector3(0, 0.02f, 0.5f), new Vector3(0.055f, 0.055f, 0.25f), dark, false);
            Block("Magazine", gun, new Vector3(0, -0.16f, -0.05f), new Vector3(0.09f, 0.23f, 0.14f), concrete, false);
            Block("Stock", gun, new Vector3(0, -0.025f, -0.34f), new Vector3(0.105f, 0.14f, 0.24f), concrete, false);
        }
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun, false);
        muzzle.localPosition = new Vector3(0, 0.02f, 0.58f);
        weapon.muzzle = muzzle;
        var presentation = player.AddComponent<WeaponPresentation>();
        presentation.weapon = weapon;
        presentation.viewModel = gun;
        return player;
    }

    internal static void PrepareActorMaterials()
    {
        dark = Material("Graphite", new Color(0.12f, 0.15f, 0.18f));
        concrete = Material("Concrete", new Color(0.45f, 0.48f, 0.5f));
        teal = Material("Signal teal", new Color(0.15f, 0.75f, 0.61f), true);
        orange = Material("Signal amber", new Color(1f, 0.32f, 0.08f), true);
    }

    private static Enemy CreateEnemy()
    {
        const string enemyPath = ArtPath + "/SentinelPBR.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
        if (existing != null) return existing.GetComponent<Enemy>();
        var root = new GameObject("Sentinel");
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 1, 0);
        collider.height = 2;
        collider.radius = 0.45f;
        var warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPath);
        if (warriorPrefab != null)
        {
            var warrior = (GameObject)PrefabUtility.InstantiatePrefab(warriorPrefab);
            warrior.name = "Sentinel PBR armor";
            warrior.transform.SetParent(root.transform, false);
            warrior.transform.localPosition = Vector3.zero;
            warrior.transform.localRotation = Quaternion.identity;
            foreach (var childCollider in warrior.GetComponentsInChildren<Collider>()) childCollider.enabled = false;
            var renderers = warrior.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                warrior.transform.localScale *= 2.08f / Mathf.Max(0.01f, bounds.size.y);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                warrior.transform.position += Vector3.up * -bounds.min.y;
            }
            var animator = warrior.GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }
        else
        {
            Block("Armored torso", root.transform, new Vector3(0, 1.15f, 0), new Vector3(0.85f, 0.7f, 0.5f), dark, false);
            Block("Helmet", root.transform, new Vector3(0, 1.8f, 0), new Vector3(0.48f, 0.42f, 0.45f), concrete, false);
            Block("Visor", root.transform, new Vector3(0, 1.84f, 0.235f), new Vector3(0.39f, 0.075f, 0.035f), orange, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Block("Leg", root.transform, new Vector3(side * 0.23f, 0.4f, 0), new Vector3(0.25f, 0.8f, 0.32f), concrete, false);
                Block("Shoulder", root.transform, new Vector3(side * 0.53f, 1.4f, 0), new Vector3(0.25f, 0.3f, 0.38f), concrete, false);
            }
        }
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform, false);
        muzzle.localPosition = new Vector3(0.34f, 1.22f, 0.78f);
        var agent = root.AddComponent<NavMeshAgent>();
        agent.height = 2;
        agent.radius = 0.45f;
        agent.stoppingDistance = 7f;
        agent.angularSpeed = 240f;
        root.AddComponent<EnemyHealth>();
        var enemy = root.AddComponent<Enemy>();
        root.AddComponent<EnemyVisualController>();
        enemy.gunBarrel = muzzle;
        enemy.sightDistance = 45f;
        enemy.fieldOfView = 150f;
        enemy.huntPlayer = true;
        enemy.bulletPrefab = TutorialMapBuilder.CreateBulletPrefab();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, enemyPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Enemy>();
    }

    private static Mesh CreateBakedRifleMesh()
    {
        const string meshPath = ArtPath + "/GQ30Rifle.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPath);
        if (source == null) return null;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        var animator = instance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Update(0f);
        }
        SkinnedMeshRenderer rifleRenderer = null;
        foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (renderer.name == "AssaultRifle") { rifleRenderer = renderer; break; }
        if (rifleRenderer == null)
        {
            Object.DestroyImmediate(instance);
            return null;
        }
        var mesh = new Mesh { name = "GQ-30 Viewmodel Rifle" };
        rifleRenderer.BakeMesh(mesh, true);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, meshPath);
        Object.DestroyImmediate(instance);
        return AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    }

    private static void ConfigureImportedTextures()
    {
        ConfigureTexture(PolyHavenPath + "Materials/Asphalt/asphalt_01_diff_4k.jpg", false);
        ConfigureTexture(PolyHavenPath + "Materials/Asphalt/asphalt_01_nor_dx_4k.jpg", true);
        ConfigureTexture(PolyHavenPath + "Materials/Concrete/concrete_pavement_diff_4k.jpg", false);
        ConfigureTexture(PolyHavenPath + "Materials/Concrete/concrete_pavement_nor_dx_4k.jpg", true);
        ConfigureTexture(PolyHavenPath + "Materials/Dirt/dirt_diff_4k.jpg", false);
        ConfigureTexture(PolyHavenPath + "Materials/Dirt/dirt_nor_dx_4k.jpg", true);
        ConfigureTexture(PolyHavenPath + "HDRI/kloppenheim_06_puresky_4k.hdr", false, true);
        ConfigureTexture(PolyHavenPath + "HDRI/nature_reserve_forest_4k.hdr", false, true);
        ConfigureTexture(PolyHavenPath + "HDRI/satara_night_4k.hdr", false, true);
        ConfigureTexture("Assets/Resources/Brand/GunQuestEmblem.png", false, false, true);
    }

    private static void ConfigureTexture(string path, bool normal, bool hdr = false, bool ui = false)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal && !hdr;
        importer.wrapMode = ui ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = ui ? 1 : 16;
        importer.mipmapEnabled = !ui;
        importer.maxTextureSize = ui ? 1024 : 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var standalone = importer.GetPlatformTextureSettings("Standalone");
        standalone.overridden = !ui;
        standalone.maxTextureSize = ui ? 1024 : 4096;
        standalone.textureCompression = TextureImporterCompression.CompressedHQ;
        standalone.compressionQuality = 100;
        importer.SetPlatformTextureSettings(standalone);
        if (ui)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
        }
        importer.SaveAndReimport();
    }

    private static void SetupSkybox(string name, string texturePath, float exposure, float rotation)
    {
        string path = ArtPath + "/" + name + ".mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(sky, path);
        }
        sky.shader = Shader.Find("Skybox/Panoramic");
        sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        sky.SetFloat("_Exposure", exposure);
        sky.SetFloat("_Rotation", rotation);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 1f;
        DynamicGI.UpdateEnvironment();
    }

    private static void ConfigurePostProcessing(VolumeProfile profile, float bloom, float vignette,
        float contrast, float saturation, float exposure, float grain)
    {
        var bloomEffect = AddVolumeOverride<Bloom>(profile);
        bloomEffect.intensity.Override(bloom);
        bloomEffect.threshold.Override(1.05f);
        bloomEffect.scatter.Override(0.67f);
        var vignetteEffect = AddVolumeOverride<Vignette>(profile);
        vignetteEffect.intensity.Override(vignette);
        vignetteEffect.smoothness.Override(0.38f);
        var grading = AddVolumeOverride<ColorAdjustments>(profile);
        grading.contrast.Override(contrast);
        grading.saturation.Override(saturation);
        grading.postExposure.Override(exposure);
        AddVolumeOverride<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var filmGrain = AddVolumeOverride<FilmGrain>(profile);
        filmGrain.intensity.Override(grain);
        filmGrain.response.Override(0.82f);
        AddVolumeOverride<ChromaticAberration>(profile).intensity.Override(0.018f);
        AddVolumeOverride<LensDistortion>(profile).intensity.Override(-0.025f);
        EditorUtility.SetDirty(profile);
    }

    private static VolumeProfile PrepareVolumeProfile(string path)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        foreach (var component in profile.components.ToArray())
            if (component != null) Object.DestroyImmediate(component, true);
        profile.components.Clear();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T AddVolumeOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = profile.Add<T>();
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void AccentLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var accent = new GameObject(name).AddComponent<Light>();
        accent.type = LightType.Point;
        accent.color = color;
        accent.intensity = intensity;
        accent.range = range;
        accent.shadows = LightShadows.None;
        accent.transform.position = position;
    }

    private static void AtmosphericParticles(string name, Vector3 position, Vector3 boxSize, Color color,
        int maximum, float size, float speed, bool rain)
    {
        var particles = new GameObject(name, typeof(ParticleSystem)).GetComponent<ParticleSystem>();
        particles.transform.position = position;
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximum;
        main.startLifetime = rain ? 2.4f : 8f;
        main.startSize = size;
        main.startSpeed = rain ? 0f : speed;
        main.startColor = color;
        var emission = particles.emission;
        emission.rateOverTime = maximum / (rain ? 2.4f : 8f);
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = rain ? -speed : 0.035f;
        var noise = particles.noise;
        noise.enabled = !rain;
        noise.strength = rain ? 0f : 0.34f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.22f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = rain ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = rain ? 5.5f : 1f;
        renderer.velocityScale = rain ? 0.08f : 0f;
        renderer.sharedMaterial = ParticleMaterial(rain ? "Rain particles" : name + " particles");
    }

    private static Material ParticleMaterial(string name)
    {
        string path = ArtPath + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 1f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
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

    private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collides = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
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
        Decor("Assets/DL/ithappy/Military_Free/Prefabs/" + asset + ".prefab", parent, position, width, yaw);
    }

    private static void Decor(string assetPath, Transform parent, Vector3 position, float width, float yaw, bool collides = true)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
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
        foreach (var existing in go.GetComponentsInChildren<Collider>()) existing.enabled = false;
        if (collides)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(bounds.size.x / go.transform.lossyScale.x, bounds.size.y / go.transform.lossyScale.y, bounds.size.z / go.transform.lossyScale.z);
        }
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

    private static Material PbrMaterial(string name, Color tint, string albedoPath, string normalPath, float tiling, float smoothness)
    {
        var mat = Material(name, tint);
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        mat.SetColor("_BaseColor", tint);
        mat.SetTexture("_BaseMap", albedo);
        mat.SetTextureScale("_BaseMap", Vector2.one * tiling);
        mat.SetTexture("_BumpMap", normal);
        mat.SetTextureScale("_BumpMap", Vector2.one * tiling);
        mat.SetFloat("_BumpScale", 1f);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        mat.EnableKeyword("_NORMALMAP");
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
        if (!File.Exists(ScenePath) || !File.Exists(BlackwoodScenePath) || !File.Exists(SkylineScenePath)) Generate();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath, BlackwoodScenePath, SkylineScenePath },
            locationPathName = "Builds/Windows/GunQuest.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
        Debug.Log("GUNQUEST WINDOWS BUILD PASSED: " + report.summary.totalSize + " bytes");
    }
}
