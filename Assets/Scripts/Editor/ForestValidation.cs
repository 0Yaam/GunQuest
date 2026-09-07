using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class ForestValidation
{
    private const string Key="GunQuest.ForestValidation";
    private static GameSession session;
    private static int phase;
    private static double since;
    static ForestValidation(){if(UnityEditor.SessionState.GetBool(Key,false))Watch();}
    public static void RebuildAndRun(){ForestChapterBuilder.Generate();Run();}
    public static void Run(){EditorSceneManager.OpenScene(OutpostBuilder.BlackwoodScenePath);UnityEditor.SessionState.SetBool(Key,true);Watch();EditorApplication.EnterPlaymode();}
    private static void Watch(){since=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    private static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(session==null)session=Object.FindAnyObjectByType<GameSession>();
            if(session==null||EditorApplication.timeSinceStartup-since<1)return;
            if(phase==0)
            {
                CoreValidation.Require(session.MissionIndex==0&&session.missionName=="BLACKWOOD","Forest must be the opening chapter.");
                CoreValidation.Require(session.GetComponent<ExpeditionHud>()!=null,"Opening chapter must use the expedition UI.");
                CoreValidation.Require(Object.FindAnyObjectByType<Terrain>()!=null,"Blackwood must use a contoured terrain, not an industrial floor.");
                var terrain=Object.FindAnyObjectByType<Terrain>();var data=terrain.terrainData;
                int tx=Mathf.RoundToInt((-9-terrain.transform.position.x)/data.size.x*(data.alphamapWidth-1));
                int tz=Mathf.RoundToInt((-49-terrain.transform.position.z)/data.size.z*(data.alphamapHeight-1));
                CoreValidation.Require(data.GetAlphamaps(tx,tz,1,1)[0,0,1]>.8f,"The authored dirt trail must survive save/reload.");
                for(int i=0;i<3;i++)
                {var path=new NavMeshPath();NavMesh.SamplePosition(session.player.transform.position,out var hit,4,NavMesh.AllAreas);CoreValidation.Require(NavMesh.CalculatePath(hit.position,session.Objectives.RelayPosition(i),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Every authored site needs a complete route.");}
                VisualCapture.Capture(session.weapon.aimCamera,"forest-opening");
                session.StartRun();session.player.GetComponent<InputManager>().enabled=false;
            }
            else if(phase==1){VisualCapture.Capture(session.weapon.aimCamera,"forest-trail");View(new Vector2(4,-11),new Vector3(8,3,7));}
            else if(phase==2){VisualCapture.Capture(session.weapon.aimCamera,"forest-creek");View(new Vector2(-8,11),new Vector3(-20,4,25));}
            else if(phase==3){VisualCapture.Capture(session.weapon.aimCamera,"forest-station");View(new Vector2(0,31),new Vector3(8,13,45));}
            else if(phase==4){VisualCapture.Capture(session.weapon.aimCamera,"forest-ridge");CrossBridge();Debug.Log("GUNQUEST FOREST VALIDATION PASSED: opening chapter, painted terrain, complete objective routes, character-controller bridge crossing and four authored vistas.");Finish(0);return;}
            phase++;since=EditorApplication.timeSinceStartup;
        }
        catch(System.Exception e){Debug.LogException(e);Finish(1);}
    }
    private static void View(Vector2 p,Vector3 target)
    {
        var controller=session.player.GetComponent<CharacterController>();controller.enabled=false;
        session.player.transform.position=new Vector3(p.x,ForestChapterBuilder.Height(p.x,p.y)+.1f,p.y);controller.enabled=true;
        session.player.transform.rotation=Quaternion.identity;
        session.weapon.aimCamera.transform.LookAt(target);
    }
    private static void CrossBridge()
    {
        var controller=session.player.GetComponent<CharacterController>();controller.enabled=false;
        session.player.transform.position=new Vector3(8,ForestChapterBuilder.Height(8,-9)+.1f,-9);controller.enabled=true;
        Physics.SyncTransforms();
        for(int i=0;i<140;i++) { controller.Move(new Vector3(0,0,.15f));controller.Move(new Vector3(0,-.09f,0)); }
        Debug.Log("FOREST BRIDGE CROSSING ended at "+session.player.transform.position);
        if(session.player.transform.position.z<=7)
            foreach(var hit in Physics.RaycastAll(session.player.transform.position+Vector3.up*.25f,Vector3.forward,2))Debug.Log("FOREST BRIDGE OBSTRUCTION "+hit.collider.name+" at "+hit.point);
        CoreValidation.Require(session.player.transform.position.z>7,"The player must be able to cross both bridge ramps without jumping.");
    }
    private static void Finish(int code){UnityEditor.SessionState.SetBool(Key,false);EditorApplication.update-=Tick;if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.ExitPlaymode();}
}
