using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in standalone smoke test. Inactive during ordinary play.</summary>
public sealed class RuntimeVisualCheck : MonoBehaviour
{
    private GameSession session;
    private string destination;
    private bool failed;
    private float startedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-gunquest-visual-check");
        if(index<0||index+1>=args.Length)return;
        Application.runInBackground=true;
        var check=new GameObject("Standalone visual validation").AddComponent<RuntimeVisualCheck>();
        check.destination=System.IO.Path.GetFullPath(args[index+1]);
    }
    private void OnEnable(){startedAt=Time.realtimeSinceStartup;Application.logMessageReceived+=OnLog;}
    private void OnDestroy()=>Application.logMessageReceived-=OnLog;
    private void Update()
    {
        if(Time.realtimeSinceStartup-startedAt>60) { Debug.LogError("Standalone visual check timed out.");Application.Quit(1); }
    }
    private void OnLog(string message,string stack,LogType type)
    {
        if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;
        failed=true;
    }
    private IEnumerator Start()
    {
        Directory.CreateDirectory(destination);
        yield return new WaitForSecondsRealtime(3);
        session=FindAnyObjectByType<GameSession>();
        if(session==null||session.missionName!="BLACKWOOD") { Debug.LogError("Standalone must boot into Blackwood.");Application.Quit(1);yield break; }
        yield return Capture("blackwood-menu");
        Click("OPTIONS");yield return Capture("blackwood-options");Click("CLOSE");
        Click("BEGIN OPERATION");session.player.GetComponent<InputManager>().enabled=false;
        yield return Capture("blackwood-trail");
        View(new Vector2(4,-11),new Vector3(8,3,7));yield return Capture("blackwood-creek");
        View(new Vector2(-8,11),new Vector3(-20,4,25));yield return Capture("blackwood-station");
        View(new Vector2(0,31),new Vector3(8,13,45));yield return Capture("blackwood-ridge");
        session.TogglePause();Click("FIELD GUIDE");yield return Capture("blackwood-field-guide");
        Debug.Log(failed?"GUNQUEST STANDALONE VISUAL CHECK FAILED":"GUNQUEST STANDALONE VISUAL CHECK PASSED: forest boot, menu buttons, deploy, four vistas, pause and field guide.");
        Application.Quit(failed?1:0);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.5f);
        // Hidden Windows players do not present a swapchain: explicitly render the real
        // camera and UGUI to an offscreen target instead of saving a black desktop frame.
        VisualCapture.Capture(session.weapon.aimCamera,name,1600,900,destination);
        yield return null;
    }
    private void Click(string name)
    {
        foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if(button.name==name&&button.isActiveAndEnabled&&button.interactable) { button.onClick.Invoke();return; }
        Debug.LogError("Missing usable UI button: "+name);
    }
    private void View(Vector2 p,Vector3 target)
    {
        var terrain=FindAnyObjectByType<Terrain>();var controller=session.player.GetComponent<CharacterController>();controller.enabled=false;
        var ground=new Vector3(p.x,0,p.y);ground.y=terrain.SampleHeight(ground)+terrain.transform.position.y+.1f;
        session.player.transform.position=ground;controller.enabled=true;session.player.transform.rotation=Quaternion.identity;
        session.weapon.aimCamera.transform.LookAt(target);
    }
}
