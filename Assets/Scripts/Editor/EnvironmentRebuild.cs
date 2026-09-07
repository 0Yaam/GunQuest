using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Authored industrial environments with world-scaled surfaces and rounded architecture.</summary>
public static class EnvironmentRebuild
{
    private const string Art = "Assets/Rebuilt";
    private const string Textures = "Assets/ThirdParty/PolyHaven/";
    private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
    private static Transform architecture;
    private static Material concrete, steel, pale, rust, rubber, amber, cyan, glass, earth, road;
    private static int map;

    [MenuItem("GunQuest/World/Rebuild industrial campaign")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art + "/Meshes");
        AssetDatabase.Refresh();
        Meshes.Clear();
        OutpostBuilder.PrepareActorMaterials();
        concrete = Surface("Weathered concrete", new Color(0.64f, 0.65f, 0.64f), 0f, 0.28f, "Concrete/concrete_pavement");
        steel = Surface("Brushed gunmetal", new Color(0.22f, 0.28f, 0.32f), 0.72f, 0.52f);
        pale = Surface("Ceramic cladding", new Color(0.70f, 0.72f, 0.69f), 0.3f, 0.4f);
        rust = Surface("Oxide paint", new Color(0.46f, 0.14f, 0.045f), 0.42f, 0.32f);
        rubber = Surface("Dark seals", new Color(0.038f, 0.049f, 0.06f), 0.05f, 0.27f);
        earth = Surface("Earth and gravel", new Color(0.65f, 0.58f, 0.44f), 0f, 0.15f, "Dirt/dirt");
        road = Surface("Worn asphalt", new Color(0.52f, 0.55f, 0.59f), 0.06f, 0.4f, "Asphalt/asphalt_01");
        amber = Surface("Warm fixtures", new Color(1f, 0.60f, 0.23f), 0f, 0.3f, null, 3.2f);
        cyan = Surface("Cool fixtures", new Color(0.20f, 0.75f, 1f), 0f, 0.3f, null, 2.8f);
        glass = Surface("Smoked glazing", new Color(0.055f, 0.13f, 0.19f), 0.8f, 0.92f);
        string[] scenes = { OutpostBuilder.ScenePath, OutpostBuilder.BlackwoodScenePath, OutpostBuilder.SkylineScenePath };
        for (map = 0; map < 3; map++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            architecture = new GameObject("World / collision and architecture").transform;
            Lighting();
            Ground();
            if (map == 0) Outpost();
            else if (map == 1) Blackwood();
            else Skyline();
            Perimeter();
            OutpostBuilder.BakeNavigation(architecture, Art + "/Navigation" + map + ".asset");
            string[] descriptions = {
                "A refinery at the edge of the desert.\nDefend the uplink through five hostile waves.",
                "An abandoned research station in the highlands.\nSecure the relay beneath the forest canopy.",
                "A rain-soaked industrial district after dark.\nBreak the blockade and cut the transmission."
            };
            OutpostBuilder.CreateMissionActors("0" + (map == 0 ? 2 : map == 1 ? 1 : 3), new[] { "OUTPOST", "BLACKWOOD", "SKYLINE" }[map], descriptions[map],
                "The transmission is secure.\nAll hostile contacts eliminated.", map == 0 ? new Color(1f, 0.65f, 0.28f) : new Color(0.30f, 0.81f, 0.91f),
                new Vector3(0, 0.2f, -23),
                new[] { new Vector3(-24, 0, -23), new Vector3(24, 0, -23), new Vector3(-8, 0, 24), new Vector3(8, 0, 24) },
                new[] { new Vector3(-15, 0.9f, -17), new Vector3(15, 0.9f, -17), new Vector3(-15, 0.9f, 16), new Vector3(15, 0.9f, 16) });
            RefineViewModel();
            EditorSceneManager.SaveScene(scene, scenes[map]);
        }
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(scenes[0], true), new EditorBuildSettingsScene(scenes[1], true), new EditorBuildSettingsScene(scenes[2], true)
        };
        AssetDatabase.SaveAssets();
        ForestChapterBuilder.Generate();
        Debug.Log("GUNQUEST WORLD REBUILD PASSED: three complete industrial environments.");
    }

    private static Material Surface(string name, Color color, float metallic, float smoothness, string texture = null, float emission = 0f)
    {
        string path = Art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (texture != null)
        {
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "Materials/" + texture + "_diff_4k.jpg"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "Materials/" + texture + "_nor_dx_4k.jpg"));
            material.SetFloat("_BumpScale", 0.65f);
            material.EnableKeyword("_NORMALMAP");
        }
        if (emission > 0f) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Ground()
    {
        Box("Foundation slab", new Vector3(0, -0.55f, 0), new Vector3(64, 1, 64), map == 1 ? earth : concrete);
        Box("Service avenue", new Vector3(0, -0.035f, 0), new Vector3(13, 0.08f, 62), map == 1 ? earth : road, 0.015f);
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Drainage channel", new Vector3(s * 7.1f, 0.005f, 0), new Vector3(0.38f, 0.04f, 60), rubber, 0.008f, false);
            for (int z = -29; z < 30; z += 2)
                Box("Drain grate", new Vector3(s * 7.1f, 0.035f, z), new Vector3(0.4f, 0.04f, 0.1f), steel, 0.009f, false);
            if (map != 1)
                for (int z = -22; z <= 22; z += 5)
                    Box("Lane paint", new Vector3(s * 5.8f, 0.012f, z), new Vector3(0.13f, 0.013f, 2.2f), pale, 0.004f, false);
        }
        // A continuous sculpted landscape replaces isolated geometric mountains.
        const int cells = 100;
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
        for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++)
        {
            float wx = (x - cells / 2f) * 4f, wz = (z - cells / 2f) * 4f;
            float distance = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));
            float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(34, 100, distance));
            float height = -0.7f + fade * (9 + Mathf.PerlinNoise(wx * 0.018f + 17, wz * 0.018f + 21) * 38
                + Mathf.PerlinNoise(wx * 0.065f, wz * 0.065f) * 6);
            vertices.Add(new Vector3(wx, height, wz)); uv.Add(new Vector2(wx, wz) / 5);
            if (x < cells && z < cells) { int i = z * (cells + 1) + x; triangles.AddRange(new[] { i, i + cells + 1, i + 1, i + 1, i + cells + 1, i + cells + 2 }); }
        }
        var mesh = new Mesh { name = "Sculpted landscape" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
        MeshObject("Highland terrain", StoreMesh(mesh, "Landscape"), null, earth);
    }

    private static void Outpost()
    {
        Building("PROCESSING / 07", new Vector3(-20, 0, 7), new Vector3(12, 8, 22), pale, false);
        Building("CONTROL", new Vector3(20, 0, 17), new Vector3(12, 10, 16), concrete, false);
        Tank(new Vector3(20, 0, -4), 2.4f, 9);
        Tank(new Vector3(26, 0, -5), 2.1f, 7.5f);
        Pipe(new Vector3(18, 6, -4), new Vector3(13, 6, -4), 0.32f, rust);
        Pipe(new Vector3(13, 6, -4), new Vector3(13, 6, 22), 0.32f, rust);
        for (int z = -8; z < 24; z += 9) Gantry(z, z == 1 ? 7.3f : 6.7f);
        Crate(new Vector3(-10, 0, -6), 3.1f); Crate(new Vector3(10, 0, 10), 2.7f);
        Barricade(new Vector3(-3.8f, 0, -9), 3.9f); Barricade(new Vector3(4.5f, 0, 14), 3.5f);
        Box("Recessed uplink footing", new Vector3(0, 0.25f, 6), new Vector3(3.5f, 0.5f, 3.5f), concrete);
        Tank(new Vector3(0, 0.5f, 6), 0.85f, 3.6f);
        Lamp(new Vector3(-12, 0, -17), false); Lamp(new Vector3(12, 0, 17), false);
        ServiceDeck(new Vector3(-20, 0, -12));
    }

    private static void Blackwood()
    {
        Building("BIOLOGY / 02", new Vector3(-21, 0, 8), new Vector3(13, 6.2f, 20), concrete, false);
        Building("FIELD LAB", new Vector3(21, 0, 17), new Vector3(12, 7.2f, 16), pale, false);
        for (int i = 0; i < 5; i++)
        {
            float z = -7 + i * 6;
            Box("Abandoned colonnade", new Vector3(12, 2.5f, z), new Vector3(1.1f, 5, 1.1f), concrete);
            Box("Lintel", new Vector3(12, 5.2f, z), new Vector3(2.2f, 0.5f, 5.6f), concrete);
        }
        Crate(new Vector3(-9, 0, -7), 2.9f); Crate(new Vector3(9, 0, 11), 2.7f);
        Barricade(new Vector3(3.5f, 0, -6), 4.5f); Barricade(new Vector3(-4, 0, 17), 4);
        Tank(new Vector3(1, 0, 7), 1.35f, 4.8f);
        Pipe(new Vector3(1, 3.5f, 7), new Vector3(-13, 3.5f, 7), 0.16f, steel);
        Lamp(new Vector3(-11, 0, -13), false); Lamp(new Vector3(11, 0, 22), false);
        ServiceDeck(new Vector3(22, 0, -10));
        Forest();
    }

    private static void Skyline()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Building("DISTRICT / " + (side == -1 ? "WEST" : "EAST"), new Vector3(side * 22, 0, 7), new Vector3(13, 17, 29), concrete, true);
            for (int row = 0; row < 5; row++)
                Building("", new Vector3(side * (39 + row % 2 * 8), 0, -29 + row * 16), new Vector3(12, 22 + row * 5, 12), steel, true);
            for (int z = -20; z <= 22; z += 14) Lamp(new Vector3(side * 10, 0, z), true);
        }
        Gantry(7, 8.5f);
        Crate(new Vector3(-9, 0, -9), 3.3f); Crate(new Vector3(9, 0, 16), 3.1f);
        Barricade(new Vector3(3.8f, 0, -5), 4.1f); Barricade(new Vector3(-4, 0, 17), 3.5f);
        Box("Switching station", new Vector3(0, 1.3f, 6), new Vector3(2.5f, 2.6f, 2), steel);
        for (int x = -1; x <= 1; x++) Box("Signal status", new Vector3(x * 0.6f, 1.8f, 4.98f), new Vector3(0.1f, 1, 0.04f), cyan, 0.01f, false);
        LightAt("Switch glow", new Vector3(0, 2.5f, 4), new Color(0.2f, 0.7f, 1), 6, 9);
    }

    private static void Building(string name, Vector3 position, Vector3 size, Material facade, bool windows)
    {
        Box(name + " / structure", position + Vector3.up * size.y / 2, size, facade, 0.16f);
        Box("Plinth", position + Vector3.up * 0.35f, new Vector3(size.x + 0.5f, 0.7f, size.z + 0.5f), steel);
        Box("Overhanging roof", position + Vector3.up * (size.y + 0.1f), new Vector3(size.x + 0.9f, 0.35f, size.z + 0.9f), steel);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = position.x + side * (size.x / 2 + 0.12f);
            for (float z = -size.z / 2 + 1.5f; z < size.z / 2; z += 3)
            {
                Box("Facade vertical seam", new Vector3(x, size.y / 2, position.z + z), new Vector3(0.22f, size.y, 0.15f), steel, 0.025f);
                if (windows)
                    for (float y = 3; y < size.y - 1; y += 3.2f)
                    {
                        Box("Window reveal", new Vector3(x + side * 0.05f, y, position.z + z + 1.25f), new Vector3(0.2f, 1.65f, 1.6f), rubber, 0.035f);
                        Box("Window glazing", new Vector3(x + side * 0.17f, y + 0.02f, position.z + z + 1.25f), new Vector3(0.04f, 1.35f, 1.28f), ((int)(y + z) % 3 == 0) ? amber : glass, 0.008f, false);
                        Box("Window sill", new Vector3(x + side * 0.22f, y - 0.85f, position.z + z + 1.25f), new Vector3(0.44f, 0.13f, 1.8f), steel, 0.02f);
                    }
            }
        }
        float front = position.z - size.z / 2;
        Box("Door recess", new Vector3(position.x, 2.25f, front - 0.05f), new Vector3(4.6f, 4.5f, 0.2f), rubber);
        for (int i = 0; i < 11; i++) Box("Roller door slat", new Vector3(position.x, 0.4f + i * 0.34f, front - 0.18f), new Vector3(4, 0.29f, 0.1f), steel, 0.025f);
        Box("Door canopy", new Vector3(position.x, 4.6f, front - 0.8f), new Vector3(5, 0.23f, 1.8f), rust);
        Box("Door luminaire", new Vector3(position.x, 4.43f, front - 0.6f), new Vector3(2.4f, 0.07f, 0.12f), amber, 0.02f, false);
        if (name.Length > 0) Sign(name, new Vector3(position.x, Mathf.Min(size.y - 0.8f, 5.9f), front - 0.24f), 0.09f);
        for (int i = 0; i < 2; i++)
        {
            var unit = position + new Vector3(-size.x * 0.24f + i * size.x * 0.48f, size.y + 0.8f, 0);
            Box("Rooftop HVAC", unit, new Vector3(2.5f, 1.3f, 3.2f), pale);
            for (int j = 0; j < 6; j++) Box("HVAC louvre", unit + new Vector3(0, -0.42f + j * 0.17f, -1.63f), new Vector3(2, 0.065f, 0.08f), rubber, 0.01f, false);
        }
        // Facade layers cast short contact shadows and break up large untextured planes.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = position.x + side * (size.x / 2 + 0.22f);
            for (float z = -size.z / 2 + 1.8f; z < size.z / 2 - 1; z += 5.5f)
            {
                if (!windows)
                {
                    Box("Recessed equipment panel", new Vector3(x, 2.5f, position.z + z), new Vector3(0.08f, 2.9f, 3.2f), steel);
                    Box("Equipment cover", new Vector3(x + side * 0.09f, 2.5f, position.z + z), new Vector3(0.12f, 2.7f, 3), pale);
                    for (int j = 0; j < 7; j++)
                        Box("Vent grille", new Vector3(x + side * 0.17f, 2.2f + j * 0.16f, position.z + z), new Vector3(0.04f, 0.07f, 2.3f), rubber, 0.01f, false);
                }
                Pipe(new Vector3(x, 0.4f, position.z + z + 2), new Vector3(x, size.y + 0.3f, position.z + z + 2), 0.065f, rust);
            }
        }
    }

    private static void ServiceDeck(Vector3 position)
    {
        Box("Raised service platform", position + Vector3.up * 0.6f, new Vector3(8, 1.2f, 8), concrete, 0.1f);
        Box("Platform surface", position + Vector3.up * 1.23f, new Vector3(8.1f, 0.08f, 8.1f), steel, 0.02f);
        for (int i = 0; i < 8; i++)
        {
            float height = (i + 1) * 0.15f;
            Box("Walkable stair", position + new Vector3(0, height / 2, -7.8f + i * 0.5f), new Vector3(3, height, 0.5f), concrete, 0.018f);
            Box("Stair nosing", position + new Vector3(0, height + 0.006f, -8.0f + i * 0.5f), new Vector3(3.02f, 0.022f, 0.055f), pale, 0.006f, false);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -3; z <= 3; z += 2)
                Pipe(position + new Vector3(side * 3.8f, 1.2f, z), position + new Vector3(side * 3.8f, 2.35f, z), 0.038f, rust);
            for (int level = 0; level < 2; level++)
                Pipe(position + new Vector3(side * 3.8f, 1.7f + level * 0.65f, -3.8f), position + new Vector3(side * 3.8f, 1.7f + level * 0.65f, 3.8f), 0.035f, rust);
        }
        Crate(position + new Vector3(-2, 1.3f, 1.6f), 1.7f);
    }

    private static void Tank(Vector3 position, float radius, float height)
    {
        Cylinder("Tank foundation", position + Vector3.up * 0.2f, radius + 0.4f, 0.4f, concrete);
        Cylinder("Pressure vessel", position + Vector3.up * (height / 2 + 0.4f), radius, height, pale);
        for (int i = 0; i < 4; i++) Cylinder("Steel reinforcement hoop", position + Vector3.up * (0.55f + i * height / 3), radius + 0.055f, 0.13f, steel);
        Cylinder("Vessel cap", position + Vector3.up * (height + 0.5f), radius * 0.95f, 0.3f, steel);
        Pipe(position + new Vector3(radius + 0.22f, 0.4f, 0), position + new Vector3(radius + 0.22f, height - 0.3f, 0), 0.12f, rust);
        for (int i = 0; i < Mathf.FloorToInt(height / 0.4f); i++)
            Pipe(position + new Vector3(-0.32f, 0.65f + i * 0.4f, -radius - 0.09f), position + new Vector3(0.32f, 0.65f + i * 0.4f, -radius - 0.09f), 0.025f, steel);
        Sign("07", position + new Vector3(0, height * 0.64f, -radius - 0.025f), 0.16f);
    }

    private static void Gantry(float z, float height)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Gantry concrete footing", new Vector3(s * 12.3f, 0.4f, z), new Vector3(1.4f, 0.8f, 1.4f), concrete);
            Box("Gantry column", new Vector3(s * 12.3f, height / 2, z), new Vector3(0.32f, height, 0.38f), steel, 0.035f);
            Beam(new Vector3(s * 12.3f, height - 2, z), new Vector3(s * 10.3f, height, z), 0.2f, steel);
        }
        Box("Overhead service rack", new Vector3(0, height, z), new Vector3(25.2f, 0.45f, 1.2f), steel);
        Pipe(new Vector3(-23, height + 0.46f, z - 0.3f), new Vector3(23, height + 0.46f, z - 0.3f), 0.23f, rust);
        Pipe(new Vector3(-23, height + 0.43f, z + 0.34f), new Vector3(23, height + 0.43f, z + 0.34f), 0.16f, pale);
        Box("Rack downlight", new Vector3(0, height - 0.25f, z), new Vector3(2.4f, 0.07f, 0.3f), amber, 0.02f, false);
        LightAt("Gantry work light", new Vector3(0, height - 0.5f, z), new Color(1f, 0.75f, 0.47f), 7, 15);
    }

    private static void Crate(Vector3 p, float width)
    {
        Box("Cargo skid", p + Vector3.up * 0.15f, new Vector3(width + 0.2f, 0.3f, 2.1f), steel);
        Box("Reinforced cargo", p + Vector3.up * 1.15f, new Vector3(width, 2, 1.9f), rust, 0.12f);
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Cargo corner", p + new Vector3(s * (width / 2 - 0.16f), 1.15f, -0.99f), new Vector3(0.22f, 2.1f, 0.16f), steel, 0.025f);
            Box("Cargo band", p + new Vector3(0, 1.15f + s * 0.64f, -1), new Vector3(width + 0.1f, 0.11f, 0.09f), steel, 0.02f);
        }
        Box("Cargo label", p + new Vector3(-width / 4, 1.3f, -1.005f), new Vector3(0.52f, 0.32f, 0.02f), pale, 0.005f, false);
    }

    private static void Barricade(Vector3 p, float width)
    {
        Box("Jersey barrier footing", p + Vector3.up * 0.18f, new Vector3(width, 0.36f, 1.05f), concrete, 0.12f);
        Box("Barrier wall", p + Vector3.up * 0.8f, new Vector3(width - 0.15f, 1.35f, 0.64f), concrete, 0.13f);
        Box("Barrier cap", p + Vector3.up * 1.48f, new Vector3(width, 0.12f, 0.76f), steel, 0.035f);
        for (float x = -width / 2 + 0.3f; x < width / 2; x += 0.58f)
        {
            var stripe = Box("Hazard inset", p + new Vector3(x, 1.1f, -0.33f), new Vector3(0.25f, 0.31f, 0.018f), rust, 0.008f, false);
            stripe.transform.localRotation = Quaternion.Euler(0, 0, -25);
        }
    }

    private static void Lamp(Vector3 p, bool cool)
    {
        Cylinder("Lamp base", p + Vector3.up * 0.2f, 0.34f, 0.4f, concrete);
        Pipe(p, p + Vector3.up * 5.8f, 0.095f, steel);
        Pipe(p + Vector3.up * 5.8f, p + new Vector3(-Mathf.Sign(p.x) * 1.6f, 5.8f, 0), 0.085f, steel);
        Vector3 head = p + new Vector3(-Mathf.Sign(p.x) * 1.6f, 5.8f, 0);
        Box("Lamp housing", head, new Vector3(0.85f, 0.2f, 0.6f), steel);
        Box("Lamp diffuser", head + Vector3.down * 0.12f, new Vector3(0.66f, 0.04f, 0.4f), cool ? cyan : amber, 0.02f, false);
        LightAt("Street light", head + Vector3.down * 0.35f, cool ? new Color(0.45f, 0.72f, 1f) : new Color(1f, 0.72f, 0.4f), 10, 15);
    }

    private static void Perimeter()
    {
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Boundary retaining wall", new Vector3(s * 31, 1.3f, 0), new Vector3(0.7f, 2.6f, 63), concrete);
            Box("Boundary end wall", new Vector3(0, 1.3f, s * 31), new Vector3(63, 2.6f, 0.7f), concrete);
            for (int x = -28; x <= 28; x += 4)
            {
                Box("Perimeter pier", new Vector3(x, 1.8f, s * 31), new Vector3(0.46f, 3.6f, 0.95f), steel);
                Pipe(new Vector3(x, 3.3f, s * 31), new Vector3(x + 3.8f, 3.3f, s * 31), 0.04f, steel);
            }
        }
    }

    private static void Forest()
    {
        const string root = "Assets/DL/Fantasy Forest Environment Free Sample/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Meshes/Prefabs/tree_1.prefab");
        if (prefab == null) return;
        var bark = Surface("Forest bark", new Color(0.58f, 0.52f, 0.43f), 0f, 0.15f);
        bark.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Textures/bark01_bottom.tga"));
        var leaves = Surface("Forest foliage", new Color(0.70f, 0.78f, 0.55f), 0f, 0.13f);
        leaves.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Textures/tree_branches.png"));
        leaves.SetFloat("_AlphaClip", 1); leaves.SetFloat("_Cutoff", 0.4f); leaves.SetFloat("_Cull", 0);
        leaves.EnableKeyword("_ALPHATEST_ON"); leaves.renderQueue = 2450;
        EditorUtility.SetDirty(bark); EditorUtility.SetDirty(leaves);
        for (int i = 0; i < 44; i++)
        {
            float angle = i * 2.39996f, radius = 34 + i % 5 * 4;
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tree.name = "Highland pine";
            tree.transform.position = Vector3.zero;
            var renderers = tree.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            tree.transform.localScale *= (14 + i % 4 * 3) / Mathf.Max(0.01f, bounds.size.y);
            bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            tree.transform.position = new Vector3(Mathf.Sin(angle) * radius, -bounds.min.y, Mathf.Cos(angle) * radius);
            tree.transform.rotation = Quaternion.Euler(0, i * 71, 0);
            foreach (var r in renderers)
            {
                var materials = r.sharedMaterials;
                for (int j = 0; j < materials.Length; j++) materials[j] = materials[j].name.ToLowerInvariant().Contains("branch") ? leaves : bark;
                r.sharedMaterials = materials;
            }
        }
    }

    private static void Lighting()
    {
        bool night = map == 2;
        var sky = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Sky" + map + ".mat");
        if (sky == null) { sky = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(sky, Art + "/Sky" + map + ".mat"); }
        sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "HDRI/kloppenheim_06_puresky_4k.hdr"));
        sky.SetFloat("_Exposure", night ? 0.14f : map == 1 ? 0.48f : 0.65f);
        sky.SetFloat("_Rotation", 215);
        sky.SetColor("_Tint", night ? new Color(0.18f, 0.29f, 0.5f) : new Color(0.50f, 0.52f, 0.54f));
        EditorUtility.SetDirty(sky); RenderSettings.skybox = sky;
        // Explicit hemisphere fill remains reliable when scenes are generated without a GPU light bake.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = night ? new Color(0.14f, 0.22f, 0.35f) : new Color(0.43f, 0.53f, 0.66f);
        RenderSettings.ambientEquatorColor = night ? new Color(0.10f, 0.14f, 0.20f) : new Color(0.29f, 0.32f, 0.34f);
        RenderSettings.ambientGroundColor = night ? new Color(0.06f, 0.075f, 0.1f) : new Color(0.17f, 0.15f, 0.12f);
        var diffuse = new GameObject("Diffuse sky fill").AddComponent<EnvironmentLighting>();
        diffuse.fill = night ? new Color(0.15f, 0.19f, 0.27f) : new Color(0.29f, 0.32f, 0.35f);
        diffuse.sky = night ? new Color(0.12f, 0.18f, 0.3f) : new Color(0.25f, 0.32f, 0.42f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = night ? new Color(0.04f, 0.075f, 0.12f) : map == 1 ? new Color(0.25f, 0.34f, 0.32f) : new Color(0.52f, 0.54f, 0.54f);
        RenderSettings.fogDensity = night ? 0.009f : map == 1 ? 0.008f : 0.004f;
        var sun = new GameObject("Raking key light").AddComponent<Light>(); sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(night ? 48 : 28, -48, 0); sun.shadows = LightShadows.Soft;
        sun.color = night ? new Color(0.44f, 0.64f, 1f) : new Color(1f, 0.83f, 0.62f); sun.intensity = night ? 0.9f : 2.1f;
        sun.shadowBias = 0.025f; sun.shadowNormalBias = 0.18f; RenderSettings.sun = sun;
        // Broad, shadowless bounce represents light returned by the open sky and courtyard walls.
        var bounce = new GameObject("Sky bounce / broad fill").AddComponent<Light>();
        bounce.type = LightType.Directional; bounce.shadows = LightShadows.None;
        bounce.transform.rotation = Quaternion.Euler(54, 135, 0);
        bounce.color = night ? new Color(0.62f, 0.72f, 0.9f) : new Color(0.65f, 0.77f, 0.92f);
        bounce.intensity = night ? 0.65f : 0.72f;
        if (night)
            for (int side = -1; side <= 1; side += 2)
                for (int z = -13; z <= 23; z += 12)
                    LightAt("Facade warm spill", new Vector3(side * 14, 4, z), new Color(1f, 0.47f, 0.22f), 18, 13);
        var volume = new GameObject("Cinematic exposure").AddComponent<Volume>(); volume.isGlobal = true;
        string path = Art + "/Grade" + map + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        foreach (var old in profile.components.ToArray()) if (old != null) Object.DestroyImmediate(old, true);
        profile.components.Clear();
        Override<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var colors = Override<ColorAdjustments>(profile); colors.contrast.Override(8); colors.saturation.Override(-8); colors.postExposure.Override(night ? 0.6f : 0.2f);
        var bloom = Override<Bloom>(profile); bloom.intensity.Override(0.2f); bloom.threshold.Override(1.3f);
        Override<Vignette>(profile).intensity.Override(0.16f); volume.sharedProfile = profile; EditorUtility.SetDirty(profile);
        var probe = new GameObject("Local environment reflections").AddComponent<ReflectionProbe>();
        probe.transform.position = new Vector3(0, 4, 0); probe.size = new Vector3(65, 25, 65);
        probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce; probe.resolution = 128; probe.boxProjection = true;
    }

    private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
    { var component = profile.Add<T>(); AssetDatabase.AddObjectToAsset(component, profile); return component; }

    private static void LightAt(string name, Vector3 position, Color color, float intensity, float range)
    {
        var light = new GameObject(name).AddComponent<Light>(); light.transform.position = position;
        light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
    }

    internal static void RefineViewModel()
    {
        if (rubber == null) rubber = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Dark seals.mat");
        if (steel == null) steel = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Brushed gunmetal.mat");
        var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
        var presentation = weapon.GetComponent<WeaponPresentation>();
        var gun = presentation.viewModel;
        gun.localPosition = new Vector3(0.24f, -0.22f, 0.45f);
        gun.localScale = Vector3.one * 0.8f;
        // Remove oversized accessory blocks; the source rifle already has modeled sights.
        foreach (string name in new[] { "Holographic sight", "Sight shroud left", "Sight shroud right" })
        { var child = gun.Find(name); if (child != null) Object.DestroyImmediate(child.gameObject); }
        var rifle = gun.Find("PBR rifle geometry");
        if (rifle != null)
        {
            var source = rifle.GetComponent<MeshFilter>().sharedMesh;
            // Bake the large imported vertex offset into a centered mesh, then expose the receiver side.
            string path = Art + "/Meshes/CenteredRifle.asset";
            var centered = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (centered == null)
            {
                centered = Object.Instantiate(source); centered.name = "Centered GQ30";
                var vertices = centered.vertices; var rotation = rifle.localRotation;
                float scale = rifle.localScale.x;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = rotation * (vertices[i] - source.bounds.center) * scale;
                centered.vertices = vertices;
                var normals = centered.normals; for (int i = 0; i < normals.Length; i++) normals[i] = rotation * normals[i]; centered.normals = normals;
                centered.RecalculateBounds(); centered.RecalculateTangents(); AssetDatabase.CreateAsset(centered, path);
            }
            rifle.GetComponent<MeshFilter>().sharedMesh = centered;
            rifle.localScale = Vector3.one;
            rifle.localRotation = Quaternion.Euler(0, -7, -12);
            rifle.localPosition = new Vector3(0, -0.015f, 0.06f);
        }
        ArmSegment(gun, "Support sleeve", new Vector3(-0.30f, -0.32f, -0.1f), new Vector3(-0.06f, -0.1f, 0.20f), 0.095f, rubber);
        ArmSegment(gun, "Support wrist armor", new Vector3(-0.10f, -0.14f, 0.14f), new Vector3(-0.065f, -0.095f, 0.20f), 0.104f, steel);
        ArmSegment(gun, "Support glove", new Vector3(-0.055f, -0.08f, 0.21f), new Vector3(0.025f, -0.08f, 0.22f), 0.066f, rubber);
        ArmSegment(gun, "Trigger sleeve", new Vector3(0.23f, -0.31f, -0.30f), new Vector3(0.04f, -0.16f, -0.08f), 0.09f, rubber);
        ArmSegment(gun, "Trigger glove", new Vector3(0.04f, -0.16f, -0.08f), new Vector3(0.04f, -0.065f, -0.04f), 0.065f, rubber);
        weapon.aimCamera.farClipPlane = 450;
    }

    private static void ArmSegment(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material)
    {
        var mesh = RoundedBox(new Vector3(width, Vector3.Distance(start, end), width), width * 0.4f);
        var go = MeshObject(name, StoreMesh(mesh, name.Replace(" ", "")), parent, material);
        go.transform.localPosition = (start + end) / 2;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
    }

    private static void Sign(string text, Vector3 position, float size)
    {
        var go = new GameObject("Painted designation"); go.transform.SetParent(architecture); go.transform.position = position;
        var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 72; label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter; label.color = new Color(0.84f, 0.85f, 0.81f);
    }

    private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, float bevel = 0.06f, bool collides = true)
    {
        string key = size.ToString("F3") + "b" + bevel.ToString("F3");
        if (!Meshes.TryGetValue(key, out var mesh)) { mesh = RoundedBox(size, bevel); mesh = StoreMesh(mesh, "Box" + Meshes.Count); Meshes.Add(key, mesh); }
        var go = MeshObject(name, mesh, architecture, material); go.transform.position = position;
        if (collides) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    private static GameObject MeshObject(string name, Mesh mesh, Transform parent, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static Mesh StoreMesh(Mesh mesh, string name)
    {
        mesh.name = name;
        string path = Art + "/Meshes/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
    }

    private static void Cylinder(string name, Vector3 center, float radius, float height, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(architecture);
        go.transform.position = center; go.transform.localScale = new Vector3(radius * 2, height / 2, radius * 2); go.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void Pipe(Vector3 start, Vector3 end, float radius, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = "Service pipe"; go.transform.SetParent(architecture);
        go.transform.position = (start + end) / 2; go.transform.up = (end - start).normalized;
        go.transform.localScale = new Vector3(radius * 2, Vector3.Distance(start, end) / 2, radius * 2);
        go.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    private static void Beam(Vector3 start, Vector3 end, float width, Material material)
    { var go = Box("Diagonal brace", (start + end) / 2, new Vector3(width, Vector3.Distance(start, end), width), material, 0.02f, false); go.transform.up = (end - start).normalized; }

    private static Mesh RoundedBox(Vector3 size, float radius)
    {
        radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.45f);
        Vector3 half = size / 2, inner = half - Vector3.one * radius;
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var normal in directions)
        {
            Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
            Vector3 v = Vector3.Cross(normal, u);
            float hu = Vector3.Dot(half, Abs(u)), hv = Vector3.Dot(half, Abs(v)), hn = Vector3.Dot(half, Abs(normal));
            float[] xs = { -hu, -hu + radius, hu - radius, hu }, ys = { -hv, -hv + radius, hv - radius, hv };
            int begin = vertices.Count;
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
            {
                Vector3 point = normal * hn + u * xs[x] + v * ys[y];
                Vector3 clamp = new Vector3(Mathf.Clamp(point.x, -inner.x, inner.x), Mathf.Clamp(point.y, -inner.y, inner.y), Mathf.Clamp(point.z, -inner.z, inner.z));
                Vector3 n = (point - clamp).normalized;
                vertices.Add(clamp + n * radius); normals.Add(n); uvs.Add(new Vector2(xs[x], ys[y]) / 3);
                if (x < 3 && y < 3) { int i = begin + y * 4 + x; triangles.AddRange(new[] { i, i + 1, i + 4, i + 1, i + 5, i + 4 }); }
            }
        }
        var mesh = new Mesh { name = "Beveled architectural module" }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
    }
    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
