using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public sealed class ForestAssetTools : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/ForestScans/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 2048;
        importer.anisoLevel = 4;
        importer.streamingMipmaps = System.IO.Path.GetExtension(assetPath).ToLowerInvariant() != ".hdr";
        if (assetPath.Contains("nor_gl")) importer.textureType = TextureImporterType.NormalMap;
        else if (assetPath.Contains("rough") || assetPath.Contains("alpha")) importer.sRGBTexture = false;
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/ForestScans/")) return;
        ConfigureModel((ModelImporter)assetImporter);
    }

    [MenuItem("GunQuest/Performance/Apply optimized project defaults")]
    public static void OptimizeProjectAssets()
    {
        string[] textureRoots = { "Assets/ThirdParty/ForestScans", "Assets/ThirdParty/PolyHaven" };
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", textureRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool hdr = System.IO.Path.GetExtension(path).ToLowerInvariant() == ".hdr";
            importer.maxTextureSize = 2048;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = !hdr;
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = 2048;
            standalone.textureCompression = TextureImporterCompression.CompressedHQ;
            standalone.compressionQuality = 75;
            importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/ForestScans" }))
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
            if (importer == null) continue;
            ConfigureModel(importer);
            importer.SaveAndReimport();
        }

        string[] materialRoots = { "Assets/ForestChapter", "Assets/Outpost" };
        foreach (string guid in AssetDatabase.FindAssets("t:Material", materialRoots))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.shader == null || material.shader.name.StartsWith("Skybox/")) continue;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        OptimizeCampaignScenes();

        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        AssetDatabase.SaveAssets();
        Debug.Log("GUNQUEST PERFORMANCE DEFAULTS APPLIED: 2K streaming textures, instancing, cheaper foliage shadows and fullscreen.");
    }

    [MenuItem("GunQuest/Performance/Optimize campaign scene batching")]
    public static void OptimizeCampaignScenes()
    {
        foreach (string scenePath in new[] { OutpostBuilder.BlackwoodScenePath, OutpostBuilder.ScenePath, OutpostBuilder.SkylineScenePath })
        {
            if (!System.IO.File.Exists(scenePath)) continue;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                string rootName = root.name;
                if (rootName.StartsWith("BLACKWOOD /") || rootName.StartsWith("World /") || rootName.StartsWith("Outpost /") || rootName.StartsWith("Skyline /"))
                    OutpostBuilder.OptimizeEnvironment(root.transform);
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("GUNQUEST CAMPAIGN BATCHING APPLIED: static architecture and non-shadowing decorative details.");
    }

    private static void ConfigureModel(ModelImporter importer)
    {
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
    }

    public static void Inspect()
    {
        AssetDatabase.Refresh();
        foreach (string id in new[] { "rock_moss_set_01", "fern_02", "dead_tree_trunk", "pine_sapling_small" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ThirdParty/ForestScans/{id}/{id}.fbx");
            InspectPrefab(id, prefab);
        }
        for (int variant = 0; variant < 2; variant++)
            for (int lod = 0; lod < 3; lod++)
                InspectPrefab($"fir_tree_01/Fir_{variant}_LOD{lod}", AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ThirdParty/ForestScans/fir_tree_01/Fir_{variant}_LOD{lod}.fbx"));
    }

    private static void InspectPrefab(string label, GameObject prefab)
    {
        if (prefab == null) { Debug.LogWarning("Missing forest asset " + label); return; }
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = mf.GetComponent<MeshRenderer>();
            string names = renderer == null ? "none" : string.Join(",", System.Array.ConvertAll(renderer.sharedMaterials, m => m == null ? "null" : m.name));
            long indices = 0;
            if (mf.sharedMesh != null)
                for (int subMesh = 0; subMesh < mf.sharedMesh.subMeshCount; subMesh++) indices += (long)mf.sharedMesh.GetIndexCount(subMesh);
            int triangles = (int)(indices / 3);
            Debug.Log($"FOREST ASSET {label}/{mf.name}: {mf.sharedMesh.vertexCount} vertices; {triangles} triangles; bounds {mf.sharedMesh.bounds}; scale {mf.transform.lossyScale}; materials {names}");
        }
    }

    [MenuItem("GunQuest/Performance/Audit campaign scenes")]
    public static void AuditScenes()
    {
        foreach (string scenePath in new[] { OutpostBuilder.BlackwoodScenePath, OutpostBuilder.ScenePath, OutpostBuilder.SkylineScenePath })
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var counted = new HashSet<Renderer>();
            long vertices = 0, triangles = 0, materialSlots = 0;
            int shadowCasters = 0, highPoly = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var group = renderer.GetComponentInParent<LODGroup>();
                if (group != null)
                {
                    var lods = group.GetLODs();
                    if (lods.Length == 0 || System.Array.IndexOf(lods[0].renderers, renderer) < 0) continue;
                }
                if (!counted.Add(renderer)) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                if (mesh != null)
                {
                    vertices += mesh.vertexCount;
                    long indices = 0;
                    for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++) indices += (long)mesh.GetIndexCount(subMesh);
                    triangles += indices / 3;
                    if (mesh.vertexCount >= 100000) highPoly++;
                }
                materialSlots += renderer.sharedMaterials.Length;
                if (renderer.shadowCastingMode != ShadowCastingMode.Off) shadowCasters++;
            }
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            int shadowLights = 0;
            foreach (var light in lights) if (light.shadows != LightShadows.None) shadowLights++;
            var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include);
            int realtimeProbes = 0;
            foreach (var probe in probes) if (probe.mode == ReflectionProbeMode.Realtime) realtimeProbes++;
            Debug.Log($"PERFORMANCE AUDIT {scenePath}: LOD0/base renderers {counted.Count}, {vertices} vertices, {triangles} triangles, {materialSlots} material slots, {shadowCasters} shadow casters, {highPoly} meshes >=100k vertices, {lights.Length} lights ({shadowLights} shadowed), {probes.Length} probes ({realtimeProbes} realtime).");
        }
    }
}
