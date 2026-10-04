using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        var look=session.player.GetComponent<PlayerLook>();
        if(look==null||!float.IsFinite(look.xSensitivity)||look.xSensitivity<5f||look.xSensitivity>60f)
            Debug.LogError("Standalone look sensitivity did not migrate into its supported range.");
        if(!float.IsFinite(AudioListener.volume)||AudioListener.volume<0f||AudioListener.volume>1f)
            Debug.LogError("Standalone audio preference did not migrate into its supported range.");
        yield return Capture("blackwood-menu");
        Click("OPTIONS");yield return Capture("blackwood-options");Click("CLOSE");
        Click("BEGIN OPERATION");session.player.GetComponent<InputManager>().enabled=false;
        yield return Capture("blackwood-trail");
        yield return CaptureShot("blackwood-rifle-fx");
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
        float sampleStart=Time.realtimeSinceStartup,worstFrame=0f;int frames=0;
        while(Time.realtimeSinceStartup-sampleStart<1f)
        {
            yield return null;frames++;worstFrame=Mathf.Max(worstFrame,Time.unscaledDeltaTime);
        }
        float elapsed=Time.realtimeSinceStartup-sampleStart;
        Debug.Log($"GUNQUEST PERFORMANCE SAMPLE {name}: {frames/elapsed:0.0} average FPS, {worstFrame*1000f:0.0} ms worst frame.");
        // Hidden Windows players do not present a swapchain: explicitly render the real
        // camera and UGUI to an offscreen target instead of saving a black desktop frame.
        VisualCapture.Capture(session.weapon.aimCamera,name,1600,900,destination);
        yield return null;
    }
    private IEnumerator CaptureShot(string name)
    {
        yield return new WaitForSecondsRealtime(.35f);
        session.weapon.TryFire();
        yield return null;
        VisualCapture.Capture(session.weapon.aimCamera,name,1600,900,destination);
        yield return null;
    }
    private void Click(string name)
    {
        foreach(var button in FindObjectsByType<Button>())
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

/// <summary>Standalone direction-sensitive FPS check for every campaign scene.</summary>
public sealed class RuntimeCampaignPerformanceCheck : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-gunquest-campaign-performance-check")<0)return;
        Application.runInBackground=true;
        var check=new GameObject("Campaign performance validation").AddComponent<RuntimeCampaignPerformanceCheck>();
        DontDestroyOnLoad(check.gameObject);
    }

    private IEnumerator Start()
    {
        bool failed=false;
        for(int sceneIndex=0;sceneIndex<SceneManager.sceneCountInBuildSettings;sceneIndex++)
        {
            if(SceneManager.GetActiveScene().buildIndex!=sceneIndex)
            {
                var load=SceneManager.LoadSceneAsync(sceneIndex);
                while(!load.isDone)yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
            var session=FindAnyObjectByType<GameSession>();
            if(session==null){Debug.LogError("Missing GameSession in build scene "+sceneIndex);failed=true;continue;}
            var input=session.player.GetComponent<InputManager>();if(input!=null)input.enabled=false;
            var camera=session.weapon.aimCamera;
            Vector3 center=session.missionName=="BLACKWOOD"?new Vector3(0,3,0):session.missionName=="OUTPOST"?new Vector3(0,3,4):new Vector3(0,4,2);
            camera.transform.LookAt(center);
            yield return Sample(session.missionName.ToLowerInvariant()+"-center");
            camera.transform.LookAt(camera.transform.position+(camera.transform.position-center).normalized*30f);
            yield return Sample(session.missionName.ToLowerInvariant()+"-edge");
        }
        Debug.Log(failed?"GUNQUEST CAMPAIGN PERFORMANCE CHECK FAILED":"GUNQUEST CAMPAIGN PERFORMANCE CHECK PASSED.");
        Application.Quit(failed?1:0);
    }

    private static IEnumerator Sample(string label)
    {
        yield return new WaitForSecondsRealtime(.5f);
        float start=Time.realtimeSinceStartup,worstFrame=0f;int frames=0;
        while(Time.realtimeSinceStartup-start<1.5f)
        {
            yield return null;frames++;worstFrame=Mathf.Max(worstFrame,Time.unscaledDeltaTime);
        }
        float elapsed=Time.realtimeSinceStartup-start;
        Debug.Log($"GUNQUEST CAMPAIGN PERFORMANCE {label}: {frames/elapsed:0.0} average FPS, {worstFrame*1000f:0.0} ms worst frame.");
    }
}
