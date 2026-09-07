using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using PlayState = GunQuest.Game.SessionState;

/// <summary>Smoke-tests every campaign scene in real Play Mode and captures visual evidence.</summary>
[InitializeOnLoad]
public static class CampaignValidation
{
    private const string RunningKey = "GunQuest.CampaignValidation.Running";
    private const string IndexKey = "GunQuest.CampaignValidation.Index";
    private const string DifficultyKey = "GunQuest.CampaignValidation.Difficulty";
    private static readonly string[] ScenePaths =
    {
        OutpostBuilder.ScenePath,
        OutpostBuilder.BlackwoodScenePath,
        OutpostBuilder.SkylineScenePath
    };
    private static int phase;
    private static double phaseAt;
    private static double startedAt;
    private static GameSession session;
    private static bool opening;

    static CampaignValidation()
    {
        if (UnityEditor.SessionState.GetBool(RunningKey, false)) BeginWatching();
    }

    [MenuItem("GunQuest/Validation/Run campaign scene checks")]
    public static void Run()
    {
        CoreValidation.Run();
        UnityEditor.SessionState.SetBool(RunningKey, true);
        UnityEditor.SessionState.SetInt(IndexKey, 0);
        UnityEditor.SessionState.SetInt(DifficultyKey, PlayerPrefs.GetInt("GunQuest.Difficulty", 1));
        PlayerPrefs.SetInt("GunQuest.Difficulty", 1);
        BeginWatching();
    }

    private static void BeginWatching()
    {
        phase = 0;
        opening = false;
        session = null;
        phaseAt = startedAt = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling) return;
        try
        {
            int index = UnityEditor.SessionState.GetInt(IndexKey, 0);
            if (index >= ScenePaths.Length)
            {
                Finish(0);
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || opening) return;
                opening = true;
                EditorSceneManager.OpenScene(ScenePaths[index]);
                EditorApplication.EnterPlaymode();
                return;
            }
            if (EditorApplication.timeSinceStartup - startedAt > 45) throw new Exception("Campaign validation timed out on " + ScenePaths[index]);
            if (session == null) session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (session == null || EditorApplication.timeSinceStartup - phaseAt < 0.5) return;
            if (phase == 0)
            {
                string expectedName = GameSession.MissionNames[index];
                Require(session.State == PlayState.Menu && Time.timeScale == 0f, "Mission must begin at its deployment menu.");
                Require(session.missionName == expectedName && session.MissionIndex == index, "Mission metadata must match the active scene.");
                Require(session.spawnPoints.Length >= 4, "Mission must expose at least four hostile approaches.");
                Require(UnityEngine.Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>() != null, "Mission must contain baked navigation.");
                Require(NavMesh.SamplePosition(session.player.transform.position, out _, 4f, NavMesh.AllAreas), "Operator must begin on the NavMesh.");
                NavMesh.SamplePosition(session.player.transform.position, out var operatorHit, 4f, NavMesh.AllAreas);
                Require(session.Objectives != null, "Each map must initialize spatial mission objectives.");
                for (int relay = 0; relay < MissionObjectives.RelayCount; relay++)
                {
                    var relayRoute = new NavMeshPath();
                    Require(NavMesh.CalculatePath(operatorHit.position, session.Objectives.RelayPosition(relay), NavMesh.AllAreas, relayRoute) && relayRoute.status == NavMeshPathStatus.PathComplete,
                        "Every relay must be reachable from deployment on every map.");
                }
                foreach (var spawn in session.spawnPoints)
                {
                    Require(NavMesh.SamplePosition(spawn.position, out var spawnHit, 6f, NavMesh.AllAreas), "Every hostile entry must reach the NavMesh.");
                    var route = new NavMeshPath();
                    Require(NavMesh.CalculatePath(spawnHit.position, operatorHit.position, NavMesh.AllAreas, route) && route.status == NavMeshPathStatus.PathComplete,
                        "Hostile entry must have a complete route to the operator after environment reconstruction.");
                }
                for (int mission = 0; mission < GameSession.MissionScenes.Length; mission++)
                    Require(Application.CanStreamedLevelBeLoaded(GameSession.MissionScenes[mission]), "Every campaign scene must be in build settings.");
                Capture(expectedName.ToLowerInvariant() + "-menu");
                session.StartRun();
                session.player.GetComponent<InputManager>().enabled = false;
                phase = 1;
                phaseAt = EditorApplication.timeSinceStartup;
                return;
            }
            if (phase == 1 && session.Wave > 0)
            {
                Require(session.Wave == 1 && session.EnemiesRemaining == 4, "Operator difficulty must spawn four enemies in wave one.");
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
                    Require(enemy.Agent.isOnNavMesh, "Every spawned enemy must be placed on navigation.");
                phase = 2;
                phaseAt = EditorApplication.timeSinceStartup;
                return;
            }
            if (phase == 2 && EditorApplication.timeSinceStartup - phaseAt > 2)
            {
                Capture(GameSession.MissionNames[index].ToLowerInvariant() + "-gameplay");
                var enemy = UnityEngine.Object.FindAnyObjectByType<Enemy>();
                enemy.Agent.Warp(new Vector3(0, 0, -16));
                enemy.Agent.isStopped = true;
                enemy.enabled = false;
                enemy.GetComponent<StateMachine>().enabled = false;
                enemy.transform.rotation = Quaternion.Euler(0, 180, 0);
                phase = 3;
                phaseAt = EditorApplication.timeSinceStartup;
                return;
            }
            if (phase == 3 && EditorApplication.timeSinceStartup - phaseAt > 0.8)
            {
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
                {
                    var visuals = enemy.GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (visuals.Length == 0) continue;
                    var bounds = visuals[0].bounds;
                    foreach (var visual in visuals) bounds.Encapsulate(visual.bounds);
                    Require(bounds.size.y > 1.5f && bounds.size.y < 3.5f, "Animated enemy must preserve a human-scale visible body.");
                    Require(Vector3.Distance(bounds.center, enemy.transform.position) < 3f, "Animated body must remain aligned with its combat collider.");
                    var animator = enemy.GetComponentInChildren<Animator>();
                    Require(animator != null && animator.HasState(0, Animator.StringToHash("WalkFront_Shoot_AR")), "Enemy must retain its movement animation.");
                }
                Capture(GameSession.MissionNames[index].ToLowerInvariant() + "-character-check");
                UnityEditor.SessionState.SetInt(IndexKey, index + 1);
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Directory.CreateDirectory("Logs/Screenshots");
        var camera = session.weapon.aimCamera;
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        var texture = new RenderTexture(1600, 900, 24);
        var oldActive = RenderTexture.active;
        var oldTarget = camera.targetTexture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.1f;
        camera.targetTexture = texture;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = texture;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes("Logs/Screenshots/" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(image);
        texture.Release();
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void Require(bool condition, string message) => CoreValidation.Require(condition, message);

    private static void Finish(int code)
    {
        UnityEditor.SessionState.SetBool(RunningKey, false);
        PlayerPrefs.SetInt("GunQuest.Difficulty", UnityEditor.SessionState.GetInt(DifficultyKey, 1));
        PlayerPrefs.Save();
        EditorApplication.update -= Tick;
        Time.timeScale = 1f;
        if (code == 0) Debug.Log("GUNQUEST CAMPAIGN VALIDATION PASSED: all three scenes, mission metadata, build navigation, hostile entries and wave-one spawns.");
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
