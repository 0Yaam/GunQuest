using UnityEditor;
using UnityEngine;

public sealed class ForestAssetTools : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/ForestScans/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 4096;
        importer.anisoLevel = 8;
        if (assetPath.Contains("nor_gl")) importer.textureType = TextureImporterType.NormalMap;
        else if (assetPath.Contains("rough") || assetPath.Contains("alpha")) importer.sRGBTexture = false;
    }

    public static void Inspect()
    {
        AssetDatabase.Refresh();
        foreach (string id in new[] { "rock_moss_set_01", "fern_02", "dead_tree_trunk", "pine_sapling_small" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ThirdParty/ForestScans/{id}/{id}.fbx");
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = mf.GetComponent<MeshRenderer>();
                string names = string.Join(",", System.Array.ConvertAll(renderer.sharedMaterials, m => m == null ? "null" : m.name));
                Debug.Log($"FOREST ASSET {id}/{mf.name}: {mf.sharedMesh.vertexCount} vertices; bounds {mf.sharedMesh.bounds}; scale {mf.transform.lossyScale}; materials {names}");
            }
        }
    }
}
