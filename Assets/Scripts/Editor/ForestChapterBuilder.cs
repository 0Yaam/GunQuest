using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>A deliberately routed forest valley, not a reskin of the industrial arena.</summary>
public static class ForestChapterBuilder
{
    private const string Art = "Assets/ForestChapter";
    private const string Scans = "Assets/ThirdParty/ForestScans/";
    private static Transform world, flora;
    private static Material wood, metal, roof, glass, lamp, rock, deadwood, fern, saplingBark, saplingLeaf;
    private static MeshFilter[] rocks, ferns, logs, saplings;
    private static readonly MeshFilter[,] firLods = new MeshFilter[2,3];
    private static readonly Dictionary<string,Material> firMaterials = new();
    private static readonly Vector2[] Trail = { new(-9,-51), new(-11,-35), new(-9,-25), new(5,-15), new(8,1), new(2,12), new(-12,24), new(-7,34), new(6,43) };
    private static readonly Vector2[] Flank = { new(-10,-34), new(-22,-20), new(-23,-3), new(-27,10), new(-16,24) };

    [MenuItem("GunQuest/World/Build Blackwood - First Signal")]
    public static void Generate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art);
        Directory.CreateDirectory("Assets/Resources/Fonts");
        if (!File.Exists("Assets/Resources/Fonts/Interface.ttf")) AssetDatabase.CopyAsset("Assets/TextMesh Pro/Fonts/LiberationSans.ttf", "Assets/Resources/Fonts/Interface.ttf");
        if (!File.Exists("Assets/Resources/Fonts/Display.ttf")) AssetDatabase.CopyAsset("Assets/TextMesh Pro/Examples & Extras/Fonts/Oswald-Bold.ttf", "Assets/Resources/Fonts/Display.ttf");
        AssetDatabase.Refresh();
        OutpostBuilder.PrepareActorMaterials();
        Materials();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        world = new GameObject("BLACKWOOD / authored valley and collision").transform;
        flora = new GameObject("Forest ecology / layered vegetation").transform;
        flora.SetParent(world);
        Terrain();
        Atmosphere();
        Landmarks();
        Ecology();
        OutpostBuilder.BakeNavigation(world, Art + "/BlackwoodNavigation.asset");
        Vector3 start = Ground(-9,-49) + Vector3.up * 0.2f;
        OutpostBuilder.CreateMissionActors("01", "BLACKWOOD",
            "FIRST SIGNAL\nThe ranger network went silent during a sealed cargo transfer. Restore the relays, recover the patrol's last transmission, and get out with the evidence.",
            "The patrol log points to the Outpost refinery.\nFollow the cargo trail before the signal disappears.",
            new Color(0.68f,0.75f,0.59f), start,
            new[] { Ground(-27,-27), Ground(20,-14), Ground(-30,18), Ground(17,38), Ground(-14,46) },
            new[] { Ground(-14,-26) + Vector3.up * 0.9f, Ground(-7,-27) + Vector3.up * 0.9f, Ground(-16,19) + Vector3.up * 0.9f, Ground(3,39) + Vector3.up * 0.9f });
        var session = Object.FindAnyObjectByType<GameSession>();
        session.relayAnchors = new Transform[3];
        Vector2[] anchors = { new(-10,-27), new(-13,23), new(6,41) };
        for (int i = 0; i < 3; i++)
        {
            var anchor = new GameObject("Authored relay site " + (i + 1)).transform;
            anchor.position = Ground(anchors[i].x, anchors[i].y);
            session.relayAnchors[i] = anchor;
        }
        session.relayLabels = new[] { "RANGER CHECKPOINT", "STATION ARCHIVE", "SIGNAL RIDGE" };
        session.relayReports = new[] {
            "PATROL LOG / Cargo 07 passed the checkpoint. The escort never reported back.",
            "ARCHIVE RECOVERED / The quarantine order came from the Outpost refinery.",
            "SIGNAL TRACED / Refinery coordinates recovered. Return to the trailhead."
        };
        session.player.transform.rotation = Quaternion.Euler(0,-2,0);
        EnvironmentRebuild.RefineViewModel();
        var gun = session.weapon.GetComponent<WeaponPresentation>().viewModel;
        foreach (var renderer in gun.GetComponentsInChildren<Renderer>())
        {
            if (renderer.name != "PBR rifle geometry") continue;
            string path = Art + "/Field rifle.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(renderer.sharedMaterial); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor", new Color(0.37f,0.39f,0.36f));
            material.SetColor("_EmissionColor",Color.black); material.DisableKeyword("_EMISSION");
            material.SetFloat("_Smoothness",0.26f); renderer.sharedMaterial = material;
        }
        foreach (var go in scene.GetRootGameObjects()) if (go.name == "Entry warning") Object.DestroyImmediate(go);
        ReplaceHud(session);
        EditorSceneManager.SaveScene(scene,OutpostBuilder.BlackwoodScenePath);
        UpdateOtherChapter(OutpostBuilder.ScenePath,"02", "CHAIN OF CUSTODY\nThe recovered patrol log leads to Cargo 07. Secure the refinery's processing records and identify where the shipment went next.",
            "The transfer records identify Skyline's command transmitter.\nCut the source of the blockade.");
        UpdateOtherChapter(OutpostBuilder.SkylineScenePath,"03", "DEAD FREQUENCY\nThe blockade is coordinated from this district. Recover the final transmission and extract the evidence linking the forest patrol to Cargo 07.",
            "The command network is down.\nThe evidence is out of the quarantine zone.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(OutpostBuilder.BlackwoodScenePath,true), new EditorBuildSettingsScene(OutpostBuilder.ScenePath,true), new EditorBuildSettingsScene(OutpostBuilder.SkylineScenePath,true) };
        EditorSceneManager.OpenScene(OutpostBuilder.BlackwoodScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("GUNQUEST FOREST BUILD PASSED: terrain, routed trail/ford/bridge, checkpoint, station, ridge, first-scene campaign order.");
    }

    private static void UpdateOtherChapter(string path, string code, string description, string ending)
    {
        var scene = EditorSceneManager.OpenScene(path);
        var session = Object.FindAnyObjectByType<GameSession>();
        session.missionCode = code; session.missionDescription = description; session.victoryDescription = ending;
        ReplaceHud(session);
        EditorSceneManager.SaveScene(scene,path);
    }

    private static void ReplaceHud(GameSession session)
    {
        var old = session.GetComponent<GameHud>(); if (old != null) Object.DestroyImmediate(old);
        if (session.GetComponent<ExpeditionHud>() == null) session.gameObject.AddComponent<ExpeditionHud>().session = session;
    }

    private static void Materials()
    {
        wood = Surface("Weathered station timber",new Color(0.86f,0.87f,0.79f),"old_planks_02",0.13f);
        metal = Surface("Oxidised iron",new Color(0.13f,0.16f,0.16f),null,0.28f); metal.SetFloat("_Metallic",0.72f);
        roof = Surface("Dull corrugated roof",new Color(0.17f,0.21f,0.19f),null,0.32f); roof.SetFloat("_Metallic",0.48f);
        glass = Surface("Unlit station glazing",new Color(0.09f,0.13f,0.13f),null,0.85f); glass.SetFloat("_Metallic",0.85f);
        lamp = Surface("Warm practical lamps",new Color(0.82f,0.60f,0.33f),null,0.2f); lamp.EnableKeyword("_EMISSION"); lamp.SetColor("_EmissionColor",new Color(1,0.68f,0.34f)*1.5f);
        rock = Surface("Scanned mossy granite",Color.white,"rock_moss_set_01",0.14f);
        deadwood = Surface("Scanned deadwood",new Color(0.78f,0.79f,0.73f),"dead_tree_trunk",0.14f);
        fern = Foliage("Scanned ferns", "fern_02/diff.jpg","fern_02/nor_gl.jpg","fern_02/alpha.png",new Color(0.73f,0.77f,0.63f));
        saplingBark = Surface("Young pine bark",new Color(0.70f,0.68f,0.60f),null,0.12f);
        saplingBark.SetTexture("_BaseMap",Texture("pine_sapling_small/bark_diff.jpg")); saplingBark.SetTexture("_BumpMap",Texture("pine_sapling_small/bark_nor_gl.jpg")); saplingBark.EnableKeyword("_NORMALMAP");
        saplingLeaf = Foliage("Young pine needles","pine_sapling_small/twig_diff.jpg","pine_sapling_small/twig_nor_gl.jpg","pine_sapling_small/twig_alpha.png",new Color(0.71f,0.77f,0.65f));
        rocks = Sources("rock_moss_set_01"); ferns = Sources("fern_02"); logs = Sources("dead_tree_trunk"); saplings = Sources("pine_sapling_small");
        foreach (string part in new[] { "bark", "trunk_a", "trunk_b", "trunk_c" })
        {
            var mat = Surface("Fir " + part,new Color(.94f,.95f,.91f),null,.18f);
            mat.SetTexture("_BaseMap",Texture("fir_tree_01/"+part+"_diff.jpg"));
            mat.SetTexture("_BumpMap",Texture("fir_tree_01/"+part+"_nor_gl.jpg"));
            mat.EnableKeyword("_NORMALMAP"); firMaterials[part] = mat;
        }
        firMaterials["twig"] = Foliage("Fir living needles","fir_tree_01/twig_diff.jpg","fir_tree_01/twig_nor_gl.jpg","fir_tree_01/twig_alpha.png",new Color(.89f,.95f,.83f));
        for(int variant=0;variant<2;variant++) for(int lod=0;lod<3;lod++)
            firLods[variant,lod] = AssetDatabase.LoadAssetAtPath<GameObject>(Scans+"fir_tree_01/Fir_"+variant+"_LOD"+lod+".fbx").GetComponentInChildren<MeshFilter>();
    }

    private static Material Surface(string name,Color colour,string folder,float smoothness)
    {
        string path = Art + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetColor("_BaseColor",colour); mat.SetFloat("_Smoothness",smoothness); mat.enableInstancing = true;
        if (folder != null) { mat.SetTexture("_BaseMap",Texture(folder+"/diff.jpg")); mat.SetTexture("_BumpMap",Texture(folder+"/nor_gl.jpg")); mat.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(mat); return mat;
    }
    private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(Scans+path);
    private static MeshFilter[] Sources(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Scans+name+"/"+name+".fbx").GetComponentsInChildren<MeshFilter>();
    private static Material Foliage(string name,string diffuse,string normal,string alpha,Color tint)
    {
        string path = Art+"/"+name+".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("GunQuest/Forest Foliage")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetTexture("_BaseMap",Texture(diffuse)); mat.SetTexture("_BumpMap",Texture(normal)); mat.SetTexture("_AlphaMap",Texture(alpha));
        mat.SetColor("_BaseColor",tint); mat.SetFloat("_Wind",0.025f); mat.enableInstancing = true; EditorUtility.SetDirty(mat); return mat;
    }

    private static float RouteDistance(Vector2 p,Vector2[] route)
    {
        float best = float.MaxValue;
        for (int i=1;i<route.Length;i++)
        {
            Vector2 segment = route[i]-route[i-1];
            float t = Mathf.Clamp01(Vector2.Dot(p-route[i-1],segment)/segment.sqrMagnitude);
            best = Mathf.Min(best,Vector2.Distance(p,route[i-1]+segment*t));
        }
        return best;
    }
    private static float PathDistance(float x,float z) => Mathf.Min(RouteDistance(new Vector2(x,z),Trail),RouteDistance(new Vector2(x,z),Flank));
    private static float Creek(float x) => -2f + Mathf.Sin(x*0.055f)*3f;
    public static float Height(float x,float z)
    {
        float route = PathDistance(x,z);
        float baseHeight = 0.55f + (z+50)*0.022f;
        float rolling = (Mathf.PerlinNoise(x*0.026f+4,z*0.033f+7)-0.48f)*6f + Mathf.PerlinNoise(x*0.14f+1,z*0.14f+8)*0.42f;
        float shoulders = Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,14,route));
        float valleyEdge = Mathf.Max(Mathf.Abs(x)-36,Mathf.Abs(z+3)-58);
        float hills = Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,38,valleyEdge))*(16+Mathf.PerlinNoise(x*0.04f,z*0.04f)*18);
        float stream = Mathf.Exp(-Mathf.Pow((z-Creek(x))/2.6f,2))*1.35f;
        if (Mathf.Abs(x+23)<3.8f) stream *= 0.3f;
        return baseHeight+rolling*shoulders+hills-stream;
    }
    private static Vector3 Ground(float x,float z) => new Vector3(x,Height(x,z),z);

    private static void Terrain()
    {
        const int resolution=513;
        // Paint the persistent asset directly: CopySerialized loses native alpha-map subassets.
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(Art+"/Valley terrain.asset");
        if(data==null) { data=new TerrainData();AssetDatabase.CreateAsset(data,Art+"/Valley terrain.asset"); }
        data.heightmapResolution=resolution;data.alphamapResolution=512;data.size=new Vector3(192,64,192);
        var heights = new float[resolution,resolution];
        for (int z=0;z<resolution;z++) for (int x=0;x<resolution;x++) heights[z,x]=(Height(x/512f*192-96,z/512f*192-80)+8)/64;
        data.SetHeights(0,0,heights);
        var litter=Layer("Leaf litter",Texture("forest_leaves_02/diff.jpg"),Texture("forest_leaves_02/nor_gl.jpg"),4);
        var dirt=Layer("Trodden path",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHaven/Materials/Dirt/dirt_diff_4k.jpg"),AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHaven/Materials/Dirt/dirt_nor_dx_4k.jpg"),5);
        var stone=Layer("Granite banks",Texture("rock_moss_set_01/diff.jpg"),Texture("rock_moss_set_01/nor_gl.jpg"),7);
        data.terrainLayers=new[] { litter,dirt,stone };
        var alpha=new float[512,512,3];
        for(int z=0;z<512;z++) for(int x=0;x<512;x++)
        {
            float wx=x/511f*192-96,wz=z/511f*192-80;
            float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,3.6f,PathDistance(wx,wz)+Mathf.PerlinNoise(wx*.5f,wz*.5f)*.65f));
            float bank=(1-Mathf.Clamp01(Mathf.Abs(wz-Creek(wx))/4))*0.75f;
            float slope=Mathf.Clamp01((Mathf.Abs(Height(wx+1,wz)-Height(wx-1,wz))+Mathf.Abs(Height(wx,wz+1)-Height(wx,wz-1))-1)*0.55f);
            float rockWeight=Mathf.Max(bank,slope)*(1-path*0.7f);
            alpha[z,x,1]=path*(1-rockWeight); alpha[z,x,2]=rockWeight; alpha[z,x,0]=1-alpha[z,x,1]-alpha[z,x,2];
        }
        data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();EditorUtility.SetDirty(data);
        var go=UnityEngine.Terrain.CreateTerrainGameObject(AssetDatabase.LoadAssetAtPath<TerrainData>(Art+"/Valley terrain.asset"));
        go.name="Sculpted forest floor / path, litter and creek banks"; go.transform.SetParent(world); go.transform.position=new Vector3(-96,-8,-80);
        var terrain=go.GetComponent<UnityEngine.Terrain>(); terrain.heightmapPixelError=4; terrain.basemapDistance=180;
        string materialPath=Art+"/Forest terrain.mat";
        var terrainMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(terrainMaterial==null) { terrainMaterial=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(terrainMaterial,materialPath); }
        terrain.materialTemplate=terrainMaterial;
        terrain.drawInstanced=true;
        // Steep wooded ridges hide these safety bounds; no visible arena walls.
        foreach(float x in new[]{-49f,49f}) InvisibleWall(new Vector3(x,12,0),new Vector3(2,45,126));
        InvisibleWall(new Vector3(0,12,-61),new Vector3(100,45,2)); InvisibleWall(new Vector3(0,12,61),new Vector3(100,45,2));
    }
    private static TerrainLayer Layer(string name,Texture2D colour,Texture2D normal,float size)
    {
        var layer=new TerrainLayer { diffuseTexture=colour,normalMapTexture=normal,tileSize=Vector2.one*size,normalScale=0.8f,metallic=0,smoothness=0.1f,smoothnessSource=TerrainLayerSmoothnessSource.Constant };
        Save(layer,Art+"/"+name+".terrainlayer"); return AssetDatabase.LoadAssetAtPath<TerrainLayer>(Art+"/"+name+".terrainlayer");
    }
    private static void InvisibleWall(Vector3 position,Vector3 size)
    { var go=new GameObject("Hidden ridge boundary",typeof(BoxCollider)); go.transform.SetParent(world); go.transform.position=position; go.GetComponent<BoxCollider>().size=size; }

    private static void Atmosphere()
    {
        var sky=Surface("Dawn sky",Color.white,null,0);
        sky.shader=Shader.Find("Skybox/Panoramic"); sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHaven/HDRI/kloppenheim_06_puresky_4k.hdr"));
        sky.SetFloat("_Exposure",0.48f); sky.SetFloat("_Rotation",142); sky.SetColor("_Tint",new Color(0.48f,0.53f,0.55f)); RenderSettings.skybox=sky;
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogColor=new Color(0.24f,0.30f,0.31f); RenderSettings.fogDensity=0.012f;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(0.33f,0.40f,0.43f); RenderSettings.ambientEquatorColor=new Color(0.20f,0.25f,0.25f); RenderSettings.ambientGroundColor=new Color(0.10f,0.13f,0.11f);
        var fill=new GameObject("Overcast sky diffusion").AddComponent<EnvironmentLighting>(); fill.fill=new Color(0.24f,0.28f,0.27f); fill.sky=new Color(0.31f,0.38f,0.42f);
        var sun=new GameObject("First light through canopy").AddComponent<Light>(); sun.type=LightType.Directional; sun.transform.rotation=Quaternion.Euler(24,-36,0);
        sun.color=new Color(1f,0.89f,0.73f); sun.intensity=1.65f; sun.shadows=LightShadows.Soft; sun.shadowBias=0.02f; sun.shadowNormalBias=0.15f; RenderSettings.sun=sun;
        var bounce=new GameObject("Cloud fill").AddComponent<Light>(); bounce.type=LightType.Directional; bounce.transform.rotation=Quaternion.Euler(65,140,0); bounce.intensity=0.85f; bounce.color=new Color(0.74f,0.83f,0.89f); bounce.shadows=LightShadows.None;
        var volume=new GameObject("Blackwood restrained grading").AddComponent<Volume>(); volume.isGlobal=true;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art+"/Forest grade.asset");
        if(profile==null) { profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Art+"/Forest grade.asset"); }
        foreach(var c in profile.components.ToArray()) if(c!=null) Object.DestroyImmediate(c,true); profile.components.Clear();
        Effect<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var colour=Effect<ColorAdjustments>(profile); colour.saturation.Override(-17); colour.contrast.Override(9); colour.postExposure.Override(0.35f);
        var bloom=Effect<Bloom>(profile); bloom.intensity.Override(0.10f); bloom.threshold.Override(1.5f);
        Effect<Vignette>(profile).intensity.Override(0.19f); volume.sharedProfile=profile;
        EditorUtility.SetDirty(profile);
        var probe=new GameObject("Forest reflection capture").AddComponent<ReflectionProbe>(); probe.transform.position=Ground(0,0)+Vector3.up*5;
        probe.size=new Vector3(120,40,130); probe.mode=ReflectionProbeMode.Realtime; probe.refreshMode=ReflectionProbeRefreshMode.OnAwake; probe.resolution=128;
    }
    private static T Effect<T>(VolumeProfile profile) where T:VolumeComponent { var c=profile.Add<T>(); AssetDatabase.AddObjectToAsset(c,profile); return c; }

    private static void Landmarks()
    {
        // Trailhead: a modest quarantine gate, not an arena entrance.
        foreach(float x in new[]{-13f,-5f}) Box("Trailhead post",Ground(x,-43)+Vector3.up*1.05f,new Vector3(.22f,2.1f,.22f),wood);
        Box("Raised quarantine barrier",Ground(-13,-43)+new Vector3(1.15f,2.1f,0),new Vector3(3.1f,.13f,.16f),wood,Quaternion.Euler(0,0,50));
        Sign("BLACKWOOD\nRANGER DISTRICT",Ground(-14,-42)+Vector3.up*1.5f,1.5f);
        Shelter(new Vector2(-14,-29),false);
        Sign("CHECKPOINT 01\nKEEP TO THE TRAIL",Ground(-7,-30)+Vector3.up*1.2f,1.2f);
        Bridge(8,Creek(8),3.8f,8.2f);
        Water();
        Cabin(new Vector2(-21,25));
        Shelter(new Vector2(-17,19),true);
        Sign("RANGER STATION\nSIGNAL RIDGE  >",Ground(-9,19)+Vector3.up*1.6f,1.4f);
        Mast(new Vector2(8,45));
        // Logs are placed as cover on the outside of bends, leaving the intended route open.
        Scan(logs[0],Ground(-5,-20),5.4f,35,new[]{deadwood},true,true);
        Scan(logs[0],Ground(14,8),6.4f,-22,new[]{deadwood},true,true);
        Scan(logs[0],Ground(-30,13),5f,60,new[]{deadwood},true,true);
        foreach(var point in new[]{new Vector2(-10,-35),new Vector2(6,-14),new Vector2(-2,12),new Vector2(-4,34)})
            TrailPost(point);
    }

    private static void Shelter(Vector2 point,bool supply)
    {
        Vector3 p=Ground(point.x,point.y);
        foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1}) Box("Shelter post",p+new Vector3(x*1.6f,1.25f,z*1.05f),new Vector3(.14f,2.5f,.14f),wood);
        Box("Shelter roof",p+Vector3.up*2.65f,new Vector3(3.9f,.15f,2.9f),roof,Quaternion.Euler(5,0,0));
        Box("Field desk",p+new Vector3(0,.9f,.45f),new Vector3(2.3f,.12f,.7f),wood);
        Box("Sealed equipment case",p+new Vector3(.6f,1.15f,.4f),new Vector3(.65f,.4f,.44f),metal);
        Lamp(p+new Vector3(-1.4f,2.25f,-.8f));
        Sign(supply?"FIELD SUPPLIES":"PATROL LOG / 07",p+new Vector3(0,1.85f,1.1f),1.1f);
    }
    private static void Cabin(Vector2 point)
    {
        Vector3 p=Ground(point.x,point.y);
        Box("Ranger cabin foundation",p+Vector3.up*.2f,new Vector3(8.8f,.4f,7.6f),rock);
        for(int i=0;i<12;i++)
        {
            float y=.56f+i*.24f;
            Box("Horizontal timber siding",p+new Vector3(-4,y,0),new Vector3(.17f,.215f,7),wood);
            Box("Horizontal timber siding",p+new Vector3(4,y,0),new Vector3(.17f,.215f,7),wood);
            Box("Rear timber siding",p+new Vector3(0,y,3.5f),new Vector3(8,.215f,.17f),wood);
            Box("Front wall left",p+new Vector3(-2.45f,y,-3.5f),new Vector3(3.1f,.215f,.17f),wood);
            Box("Front wall right",p+new Vector3(2.45f,y,-3.5f),new Vector3(3.1f,.215f,.17f),wood);
        }
        Box("Open door lintel",p+new Vector3(0,3,-3.5f),new Vector3(1.8f,.7f,.17f),wood);
        // Recessed glazing and external shutters give the cabin a human scale.
        foreach(float x in new[]{-2.5f,2.5f})
        {
            Box("Window dark recess",p+new Vector3(x,1.95f,-3.62f),new Vector3(1.45f,1.18f,.08f),metal);
            Box("Window glass",p+new Vector3(x,1.95f,-3.67f),new Vector3(1.25f,.97f,.025f),glass);
            Box("Window centre frame",p+new Vector3(x,1.95f,-3.70f),new Vector3(.055f,1.08f,.04f),wood);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Quaternion tilt=Quaternion.Euler(0,0,-side*19);
            Box("Pitched roof",p+new Vector3(side*2.25f,4.2f,0),new Vector3(4.9f,.12f,8.5f),roof,tilt);
            for(int i=0;i<31;i++) Box("Roof corrugation",p+new Vector3(side*2.25f,4.26f,-4.1f+i*.27f),new Vector3(4.9f,.035f,.045f),metal,tilt,false);
        }
        for(int row=0;row<6;row++) foreach(float z in new[]{-3.5f,3.5f})
            Box("Timber gable",p+new Vector3(0,3.48f+row*.24f,z),new Vector3(7.8f-row*1.3f,.235f,.17f),wood);
        Box("Roof ridge cap",p+new Vector3(0,5.04f,0),new Vector3(.28f,.09f,8.55f),metal);
        Box("Porch floor",p+new Vector3(0,.34f,-4.5f),new Vector3(8.8f,.16f,2.2f),wood);
        Box("Porch step",p+new Vector3(0,.14f,-5.75f),new Vector3(2.4f,.28f,.6f),wood);
        Lamp(p+new Vector3(.7f,2.8f,-3.9f));
        Sign("BLACKWOOD\nRANGER STATION",p+new Vector3(0,3.05f,-3.75f),1.3f);
        Box("Interior archive desk",p+new Vector3(-1,1.0f,1),new Vector3(3,.15f,1.2f),wood);
        Box("Radio cabinet",p+new Vector3(2,.9f,2),new Vector3(.9f,1.8f,.7f),metal);
    }
    private static void Bridge(float x,float z,float width,float length)
    {
        float y=Mathf.Max(Height(x,z-length/2),Height(x,z+length/2))+.12f;
        for(int i=0;i<30;i++) Box("Creek bridge plank",new Vector3(x,y,z-length/2+i*length/30),new Vector3(width,.14f,length/30-.02f),wood);
        for(int side=-1;side<=1;side+=2)
        {
            Box("Bridge stringer",new Vector3(x+side*(width/2-.22f),y-.23f,z),new Vector3(.22f,.32f,length+.35f),wood);
            for(int i=0;i<4;i++) Box("Bridge handrail post",new Vector3(x+side*width/2,y+.62f,z-length/2+i*length/3),new Vector3(.12f,1.3f,.12f),wood);
            Box("Bridge handrail",new Vector3(x+side*width/2,y+1.22f,z),new Vector3(.13f,.12f,length),wood);
        }
        for(int side=-1;side<=1;side+=2)
        {
            float end=z+side*(length/2+1.3f),ground=Height(x,end);
            Vector3 a=new Vector3(x,y-.02f,z+side*length/2),b=new Vector3(x,ground+.05f,end);
            var ramp=Box("Bridge approach",(a+b)/2,new Vector3(width,.15f,Vector3.Distance(a,b)),wood);
            ramp.transform.rotation=Quaternion.LookRotation(b-a);
        }
    }
    private static void Water()
    {
        var water=Surface("Cold shallow creek",new Color(.045f,.095f,.082f),null,.92f);
        water.shader=Shader.Find("GunQuest/Forest Creek");water.renderQueue=3000;
        var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
        for(int i=0;i<=100;i++)
        {
            float x=-70+i*1.4f,z=Creek(x);
            float y=0.55f+(z+50)*.022f-.66f;
            vertices.Add(new Vector3(x,y,z-1.65f)); vertices.Add(new Vector3(x,y,z+1.65f)); uv.Add(new Vector2(i*.25f,0));uv.Add(new Vector2(i*.25f,1));
            if(i<100) { int a=i*2; triangles.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2}); }
        }
        var mesh=new Mesh(); mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents(); Save(mesh,Art+"/Creek ribbon.asset");
        var go=new GameObject("Creek / winding watercourse",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(world);
        go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Creek ribbon.asset");go.GetComponent<MeshRenderer>().sharedMaterial=water;
    }
    private static void Mast(Vector2 point)
    {
        Vector3 p=Ground(point.x,point.y);
        Box("Transmitter concrete pad",p+Vector3.up*.15f,new Vector3(4,.3f,4),rock);
        for(int side=-1;side<=1;side+=2) Beam(p+new Vector3(side*1.4f,0,0),p+new Vector3(side*.2f,17,0),.12f,metal);
        for(int i=0;i<8;i++) { Beam(p+new Vector3(-1.4f+i*.14f,i*2,0),p+new Vector3(1.25f-i*.14f,i*2+2,0),.055f,metal); Beam(p+new Vector3(1.4f-i*.14f,i*2,0),p+new Vector3(-1.25f+i*.14f,i*2+2,0),.055f,metal); }
        for(int i=0;i<4;i++) Box("Directional aerial",p+new Vector3(0,14+i*.85f,0),new Vector3(3-i*.45f,.06f,.09f),metal);
        Box("Receiver electronics",p+new Vector3(2.3f,1.0f,0),new Vector3(.8f,2,.7f),metal); Lamp(p+new Vector3(.0f,17.2f,0));
        Sign("RELAY 03\nBLACKWOOD UPLINK",p+new Vector3(-2,1.7f,-1),1.2f);
    }
    private static void TrailPost(Vector2 p)
    {
        Vector3 point=Ground(p.x+2.6f,p.y); Box("Trail wayfinding stake",point+Vector3.up*.6f,new Vector3(.14f,1.2f,.14f),wood);
        Box("Faded trail blaze",point+new Vector3(0,.96f,-.08f),new Vector3(.13f,.17f,.018f),lamp,null,false);
    }
    private static void Sign(string text,Vector3 position,float width)
    {
        Box("Weathered district sign",position,new Vector3(width,.58f,.07f),wood);
        var go=new GameObject("Painted wayfinding");go.transform.SetParent(world);go.transform.position=position+Vector3.back*.042f;
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=.035f*width;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.48f,.51f,.43f);
        // Fit physical lettering to its board instead of estimating font metrics.
        var bounds=label.GetComponent<Renderer>().bounds;
        go.transform.localScale*=Mathf.Min(width*.87f/Mathf.Max(.001f,bounds.size.x),.45f/Mathf.Max(.001f,bounds.size.y));
    }
    private static void Lamp(Vector3 p)
    {
        Box("Shielded lamp casing",p,new Vector3(.22f,.28f,.18f),metal,null,false); Box("Lamp lens",p+Vector3.back*.10f,new Vector3(.13f,.17f,.02f),lamp,null,false);
        var light=new GameObject("Warm practical pool").AddComponent<Light>();light.transform.SetParent(world);light.transform.position=p+Vector3.back*.25f;light.type=LightType.Point;light.color=new Color(1,.72f,.43f);light.range=6;light.intensity=2.5f;
    }

    private static void Ecology()
    {
        var random=new System.Random(4217);
        // Hand-placed silhouette trees frame the opening view and each key turn.
        Vector2[] hero={new(-14,-48),new(-3,-45),new(-18,-39),new(-3,-33),new(7,-27),new(17,-19),new(-18,-17),new(15,-8),new(-4,7),new(13,15),new(-29,24),new(-4,27),new(17,36),new(-3,45)};
        int n=0;foreach(var p in hero) MatureTree(p,17+n%4*2.2f,n++*51);
        // Density is controlled by distance to authored routes and functional clearings.
        for(int i=0;i<380;i++)
        {
            float x=-76+(float)random.NextDouble()*152,z=-68+(float)random.NextDouble()*164;
            if(PathDistance(x,z)<5.1f || InClearing(x,z) || Mathf.Abs(z-Creek(x))<4f) continue;
            if(i%2==0 || Mathf.Abs(x)>38) MatureTree(new Vector2(x,z),16+(float)random.NextDouble()*10,(float)random.NextDouble()*360);
        }
        for(int i=0;i<180;i++)
        {
            float x=-42+(float)random.NextDouble()*84,z=-54+(float)random.NextDouble()*108;
            float distance=PathDistance(x,z);
            if(distance<3.4f || InClearing(x,z)) continue;
            if(i%4==0)
                Scan(rocks[i%rocks.Length],Ground(x,z)-Vector3.up*.12f,1.1f+(float)random.NextDouble()*2.2f,(float)random.NextDouble()*360,new[]{rock},true,true);
            else if(i%6==0)
                Scan(saplings[i%saplings.Length],Ground(x,z),1.3f+(float)random.NextDouble()*1.2f,(float)random.NextDouble()*360,new[]{saplingBark,saplingLeaf},false,false);
            else Scan(ferns[i%ferns.Length],Ground(x,z),.5f+(float)random.NextDouble()*.55f,(float)random.NextDouble()*360,new[]{fern},false,false);
        }
        // Fern beds run along the trail shoulders, never in a uniform grid across the path.
        for(int i=0;i<220;i++)
        {
            float z=-48+(float)random.NextDouble()*94,x=-32+(float)random.NextDouble()*58;
            float distance=PathDistance(x,z);
            if(distance<2.5f || distance>7 || InClearing(x,z) || Mathf.Abs(z-Creek(x))<1.8f) continue;
            Scan(ferns[i%ferns.Length],Ground(x,z),.45f+(float)random.NextDouble()*.55f,i*47,new[]{fern},false,false);
        }
        // Moss outcrops anchor the creek banks and distant ridge silhouette.
        for(int i=0;i<38;i++)
        {
            float x=-45+i*2.5f,z=Creek(x)+(i%2==0?-4f:4f);
            if(Mathf.Abs(x-8)<5 || Mathf.Abs(x+23)<4) continue;
            Scan(rocks[i%rocks.Length],Ground(x,z)-Vector3.up*.18f,2.2f+i%3*.5f,i*37,new[]{rock},true,true);
        }
    }
    private static bool InClearing(float x,float z) =>
        Vector2.Distance(new Vector2(x,z),new Vector2(-12,-28))<5.5f ||
        (x>-28 && x<-9 && z>17 && z<31) || Vector2.Distance(new Vector2(x,z),new Vector2(7,43))<6;

    private static void MatureTree(Vector2 p,float height,float yaw)
    {
        int variant=Mathf.Abs(Mathf.RoundToInt(yaw))%2;
        var tree=new GameObject("Scanned fir / three geometry LODs");tree.transform.SetParent(flora);tree.transform.position=Ground(p.x,p.y);
        var levels=new LOD[3];float[] distances={.28f,.10f,.018f};
        for(int lod=0;lod<3;lod++)
        {
            var source=firLods[variant,lod];var originals=source.GetComponent<Renderer>().sharedMaterials;
            var materials=new Material[originals.Length];
            for(int i=0;i<materials.Length;i++)
            {
                string name=originals[i].name;
                string part=name.Contains("twig")?"twig":name.Contains("trunk_a")?"trunk_a":name.Contains("trunk_b")?"trunk_b":name.Contains("trunk_c")?"trunk_c":"bark";
                materials[i]=firMaterials[part];
            }
            var mesh=Scan(source,Ground(p.x,p.y)-Vector3.up*.08f,height,yaw,materials,false,false);
            Object.DestroyImmediate(mesh.GetComponent<LODGroup>());mesh.transform.SetParent(tree.transform,true);
            levels[lod]=new LOD(distances[lod],new[]{mesh.GetComponent<Renderer>()});
        }
        var group=tree.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();
        if(Mathf.Abs(p.x)<46 && p.y>-60 && p.y<61)
        {
            var collision=new GameObject("Trunk collision",typeof(CapsuleCollider));collision.transform.SetParent(world);collision.transform.position=Ground(p.x,p.y)+Vector3.up*3;
            var capsule=collision.GetComponent<CapsuleCollider>();capsule.height=6;capsule.radius=.42f;
        }
    }
    private static GameObject Scan(MeshFilter source,Vector3 position,float size,float yaw,Material[] materials,bool collides,bool useWidth)
    {
        var go=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(flora);
        go.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;
        go.transform.rotation=Quaternion.Euler(0,yaw,0)*source.transform.rotation;go.transform.localScale=source.transform.lossyScale;
        var b=renderer.bounds;float dimension=useWidth?Mathf.Max(b.size.x,b.size.z):b.size.y;
        go.transform.localScale*=size/Mathf.Max(.01f,dimension);b=renderer.bounds;
        go.transform.position=position-new Vector3(b.center.x,b.min.y,b.center.z);
        if(collides)go.AddComponent<MeshCollider>().sharedMesh=source.sharedMesh;
        if(!collides) { var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,new Renderer[]{renderer})});lod.RecalculateBounds(); }
        return go;
    }
    private static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Quaternion? rotation=null,bool collides=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(world);go.transform.position=position;go.transform.localScale=size;
        if(rotation.HasValue)go.transform.rotation=rotation.Value;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collides)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    private static void Beam(Vector3 a,Vector3 b,float width,Material material)
    { var go=Box("Mast cross-bracing",(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),material,null,false);go.transform.up=(b-a).normalized; }
    private static void Save(Object asset,string path)
    {
        var old=AssetDatabase.LoadAssetAtPath<Object>(path);
        if(old==null)AssetDatabase.CreateAsset(asset,path);else{EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(asset);}
    }
}
