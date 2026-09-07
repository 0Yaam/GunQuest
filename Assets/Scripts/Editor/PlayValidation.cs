using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using PlayState = GunQuest.Game.SessionState;

/// <summary>Runs real scene, physics, combat, pause and wave checks in Play Mode.</summary>
[InitializeOnLoad]
public static class PlayValidation
{
    private const string RunningKey = "GunQuest.Validation.Running";
    private const string ValidationBestKey = "GunQuest.BestScore.Outpost.Operator";
    private const string ValidationClearedKey = "GunQuest.Cleared.Outpost";
    private static int stage;
    private static double stageAt;
    private static double startedAt;
    private static GameSession session;
    private static EnemyHealth target;
    private static GameObject wall;
    private static float pausedTime;
    private static Vector3 originalPosition;
    private static Vector3 enemyPosition;
    private static Enemy trackedEnemy;
    private static float healthBeforeBullet;
    private static bool failed;
    private static bool interruptedUpload;
    private static bool leftExtraction;
    private static bool testedUpgradeCap;
    private static int lastFrame = -1;
    private static int lastUpgradeWave = 1;
    private static int reloadStarts, reloadCompletions;
    private static bool testedContested;
    private static float uploadAtPause;
    private static int objectivePausePhase;
    private static double objectivePauseAt;

    static PlayValidation()
    {
        if (!UnityEditor.SessionState.GetBool(RunningKey, false)) return;
        BeginWatching();
    }

    private static void BeginWatching()
    {
        stageAt = startedAt = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("GunQuest/Validation/Run outpost play checks")]
    public static void Run()
    {
        CoreValidation.Run();
        EditorSceneManager.OpenScene(OutpostBuilder.ScenePath);
        UnityEditor.SessionState.SetInt("GunQuest.Validation.Best", PlayerPrefs.GetInt(ValidationBestKey, 0));
        UnityEditor.SessionState.SetInt("GunQuest.Validation.Cleared", PlayerPrefs.GetInt(ValidationClearedKey, 0));
        UnityEditor.SessionState.SetInt("GunQuest.Validation.Difficulty", PlayerPrefs.GetInt("GunQuest.Difficulty", 1));
        UnityEditor.SessionState.SetInt("GunQuest.Validation.CampaignComplete", PlayerPrefs.GetInt("GunQuest.CampaignComplete", 0));
        PlayerPrefs.SetInt("GunQuest.Difficulty", 1);
        UnityEditor.SessionState.SetBool(RunningKey, true);
        BeginWatching();
        EditorApplication.EnterPlaymode();
    }

    private static void OnLog(string message, string trace, LogType type)
    {
        // Unity 6000.4 may throw while starting its editor-only search index in batch mode.
        // It does not run in the player; retain the original log and exclude only this stack.
        if (trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup")) return;
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true;
    }

    private static void Next() { stage++; stageAt = EditorApplication.timeSinceStartup; }
    private static void Check(bool condition, string message) => CoreValidation.Require(condition, message);

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 180) throw new Exception("Play validation timed out at stage " + stage);
            if (failed) throw new Exception("Runtime logged an error; inspect the preceding log.");
            double wait = EditorApplication.timeSinceStartup - stageAt;
            if (session == null) session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (session == null || wait < 0.25) return;
            switch (stage)
            {
                case 0:
                    Check(session.State == PlayState.Menu && Time.timeScale == 0, "Scene must begin at the deployment menu.");
                    Check(session.Difficulty == GunQuest.Game.Difficulty.Operator, "Operator must be the deterministic validation profile.");
                    Check(session.Objectives != null && session.Objectives.RelaysSecured == 0, "Every run must initialize three relay objectives.");
                    Check(!session.Objectives.TryBeginUpload() && !session.PurchaseUpgrade(0), "Menus must reject objective interactions and upgrades.");
                    Check(session.Options != null && session.Options.FieldOfView >= 65f && session.Options.FieldOfView <= 100f, "Player preferences must initialize inside supported ranges.");
                    ValidateOptions();
                    session.weapon.ReloadStarted += () => reloadStarts++;
                    session.weapon.ReloadCompleted += () => reloadCompletions++;
                    session.SetDifficulty(GunQuest.Game.Difficulty.Recruit);
                    Check(session.Difficulty == GunQuest.Game.Difficulty.Recruit, "Menu must allow threat-level changes.");
                    session.SetDifficulty(GunQuest.Game.Difficulty.Operator);
                    Capture("menu");
                    session.StartRun();
                    session.player.GetComponent<InputManager>().enabled = false;
                    Next();
                    break;
                case 1:
                    if (session.Wave == 0) return;
                    Check(session.Wave == 1 && session.EnemiesRemaining == 4, "First wave must spawn four enemies.");
                    trackedEnemy = UnityEngine.Object.FindAnyObjectByType<Enemy>();
                    Check(trackedEnemy.Agent.isOnNavMesh, "Enemy must spawn on baked navigation.");
                    enemyPosition = trackedEnemy.transform.position;
                    Next();
                    break;
                case 2:
                    if (wait < 1.5) return;
                    Check(Vector3.Distance(enemyPosition, trackedEnemy.transform.position) > 0.1f, "AI must navigate toward the operator.");
                    Capture("gameplay");
                    session.TogglePause();
                    pausedTime = Time.time;
                    Check(!session.weapon.TryFire(), "Paused weapon must reject shots.");
                    Next();
                    break;
                case 3:
                    Check(Time.time == pausedTime && session.State == PlayState.Paused, "Pause must freeze simulation time.");
                    Check(!session.Objectives.TryBeginUpload(), "Paused objective interactions must be rejected.");
                    session.TogglePause();
                    ValidateDryFire();
                    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
                    {
                        enemy.enabled = false;
                        enemy.GetComponent<StateMachine>().enabled = false;
                        enemy.Agent.isStopped = true;
                    }
                    originalPosition = session.player.transform.position;
                    Teleport(new Vector3(0, 20, 0));
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Validation target";
                    cube.transform.position = new Vector3(0, 21.7f, 5);
                    target = cube.AddComponent<EnemyHealth>();
                    Physics.SyncTransforms();
                    Check(session.weapon.TryFire(), "Loaded rifle must fire.");
                    Check(target.CurrentHealth == 66f, "Aimed shot must damage its target.");
                    Check(!session.weapon.TryFire(), "Fire rate must reject two shots in the same frame.");
                    Next();
                    break;
                case 4:
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.position = new Vector3(0, 21.7f, 2);
                    wall.transform.localScale = new Vector3(2, 2, 0.03f);
                    Physics.SyncTransforms();
                    Check(session.weapon.TryFire(), "Second shot must fire after cooldown.");
                    Check(target.CurrentHealth == 66f, "Thin wall must block rifle damage.");
                    UnityEngine.Object.Destroy(wall);
                    UnityEngine.Object.Destroy(target.gameObject);
                    session.weapon.BeginReload();
                    Check(session.weapon.IsReloading && !session.weapon.TryFire(), "Reload must block fire.");
                    Next();
                    break;
                case 5:
                    if (session.weapon.IsReloading) return;
                    Check(session.weapon.Ammo.Loaded == 30 && session.weapon.Ammo.Reserve == 118, "Timed reload must conserve two fired rounds.");
                    Check(reloadStarts == 1 && reloadCompletions == 1, "Reload must emit one start and one completion audio cue.");
                    healthBeforeBullet = session.player.GetCurrentHealth();
                    // A single frame travels farther than this thin wall's thickness.
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.position = new Vector3(0, 21, 1);
                    wall.transform.localScale = new Vector3(2, 2, 0.015f);
                    var bullet = new GameObject("Swept projectile validation").AddComponent<Bullet>();
                    bullet.transform.position = new Vector3(0, 21, 2);
                    bullet.transform.forward = Vector3.back;
                    Physics.SyncTransforms();
                    Next();
                    break;
                case 6:
                    if (wait < 0.5) return;
                    Check(session.player.GetCurrentHealth() == healthBeforeBullet, "Enemy projectile must stop at thin cover.");
                    UnityEngine.Object.Destroy(wall);
                    var direct = new GameObject("Direct projectile validation").AddComponent<Bullet>();
                    direct.transform.position = new Vector3(0, 21, 2);
                    direct.transform.forward = Vector3.back;
                    Physics.SyncTransforms();
                    Next();
                    break;
                case 7:
                    if (wait < 0.5) return;
                    Check(session.player.GetCurrentHealth() == healthBeforeBullet - 15f, "Unobstructed enemy projectile must damage the player once.");
                    Teleport(originalPosition);
                    ClearWave();
                    Check(session.Score == 750 && session.EnemiesRemaining == 0, "Wave completion must award four kills and clear bonus.");
                    Check(session.weapon.Ammo.Reserve == 178, "Wave completion must supply ammo.");
                    Check(session.UpgradeCredits == 1 && session.PurchaseUpgrade(0), "Clearing a wave must grant a spendable upgrade credit.");
                    Check(session.UpgradeCredits == 0 && session.weapon.damage == 39f && !session.PurchaseUpgrade(0), "Upgrade must apply once without credit underflow.");
                    session.CallNextWave();
                    Next();
                    break;
                case 8:
                    if (session.State == PlayState.Victory)
                    {
                        Check(session.Kills == 40 && session.Wave == 5, "All five waves must contain forty total enemies.");
                        Check(session.ShotsFired == 2 && session.ShotsHit == 1 && Mathf.Approximately(session.Accuracy, 50f), "Mission stats must track fired and connected shots.");
                        Check(GameSession.IsMissionCleared(0), "Victory must mark the active mission as cleared.");
                        Capture("victory");
                        session.Restart();
                        session = null;
                        Next();
                    }
                    else if (session.EnemiesRemaining > 0)
                    {
                        Check(!session.PurchaseUpgrade(0), "Upgrades must be unavailable during combat.");
                        ValidateWaveRoles();
                        ClearWave();
                    }
                    else if (session.Wave < session.totalWaves)
                    {
                        if (session.Wave == lastUpgradeWave) return;
                        lastUpgradeWave = session.Wave;
                        if (session.UpgradeLevel(0) < 3) Check(session.PurchaseUpgrade(0), "Available credits must purchase damage upgrades between waves.");
                        else if (!testedUpgradeCap)
                        {
                            Check(!session.PurchaseUpgrade(0) && session.UpgradeCredits == 1, "A capped branch must reject purchase without consuming its credit.");
                            Check(session.PurchaseUpgrade(2) && session.player.maxHealth == 115f, "Vitality must increase maximum health.");
                            testedUpgradeCap = true;
                        }
                        session.CallNextWave();
                    }
                    else ValidateObjectives();
                    break;
                case 9:
                    Check(session.State == PlayState.Menu && session.Wave == 0 && session.Kills == 0, "Restart must restore a fresh menu and run.");
                    session.StartRun();
                    session.player.TakeDamage(10000);
                    Check(session.State == PlayState.Defeat && Time.timeScale == 0, "Lethal damage must freeze the run and show defeat.");
                    Capture("defeat");
                    Debug.Log("GUNQUEST PLAY VALIDATION PASSED: menu, navigation, pause, hits, cover, reload, projectile sweep, five waves, upgrade credits/caps, relay interruption, extraction reset, victory, restart, defeat.");
                    Finish(0);
                    break;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    private static void ValidateObjectives()
    {
        var objective = session.Objectives;
        if (objectivePausePhase == 1)
        {
            Check(Mathf.Approximately(objective.Progress, uploadAtPause), "Pausing must freeze the relay upload timer.");
            if (EditorApplication.timeSinceStartup - objectivePauseAt < 0.5) return;
            session.TogglePause();
            objectivePausePhase = 2;
        }
        Check(session.State == PlayState.Playing, "Clearing all waves must not bypass relay and extraction objectives.");
        if (objective.RelaysSecured < MissionObjectives.RelayCount)
        {
            if (objective.Uploading)
            {
                if (objectivePausePhase == 0 && objective.Progress > 0.1f)
                {
                    uploadAtPause = objective.Progress;
                    session.TogglePause();
                    objectivePauseAt = EditorApplication.timeSinceStartup;
                    objectivePausePhase = 1;
                    return;
                }
                if (!interruptedUpload && objective.Progress > 0.15f)
                {
                    Teleport(originalPosition);
                    interruptedUpload = true;
                }
                return;
            }
            if (interruptedUpload && objective.RelaysSecured == 0) Check(objective.Progress == 0f, "Leaving a relay must reset upload progress.");
            Teleport(objective.TargetPosition + Vector3.back * 1.5f);
            Physics.SyncTransforms();
            if (!testedContested)
            {
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                blocker.name = "Validation contested relay";
                blocker.transform.position = objective.TargetPosition + Vector3.right * 2f + Vector3.up;
                blocker.AddComponent<EnemyHealth>();
                Physics.SyncTransforms();
                Check(!objective.TryBeginUpload(), "A living hostile inside the ring must block upload.");
                blocker.GetComponent<Collider>().enabled = false;
                UnityEngine.Object.Destroy(blocker);
                testedContested = true;
            }
            Check(objective.TryBeginUpload(), "Unlocked uncontested relay must accept interaction from inside its ring.");
            return;
        }
        Check(objective.Extracting, "All relays and hostiles must unlock extraction.");
        if (!leftExtraction && objective.Progress > 0.15f)
        {
            Teleport(objective.TargetPosition + Vector3.forward * 8f);
            leftExtraction = true;
            return;
        }
        if (leftExtraction && objective.Distance > 3.5f) Check(objective.Progress == 0f, "Leaving extraction must reset its countdown.");
        Teleport(objective.TargetPosition);
    }

    private static void ValidateDryFire()
    {
        var probeObject = new GameObject("Validation empty weapon");
        var probe = probeObject.AddComponent<PlayerWeapon>();
        probe.aimCamera = session.weapon.aimCamera;
        for (int i = 0; i < 30; i++) probe.Ammo.TryFire();
        int emptyCues = 0, reloadCues = 0;
        probe.DryFired += () => emptyCues++;
        probe.ReloadStarted += () => reloadCues++;
        Check(!probe.TryFire() && !probe.TryFire(), "Empty weapon must reject shots.");
        Check(emptyCues == 1 && reloadCues == 1 && probe.IsReloading, "Empty fire must cue once and begin automatic reload without sound spam.");
        UnityEngine.Object.Destroy(probeObject);
    }

    private static void ValidateOptions()
    {
        var options = session.Options;
        float originalFov = options.FieldOfView;
        int originalGraphics = options.GraphicsPreset;
        bool originalInvert = options.InvertY, originalMotion = options.ReducedMotion, originalLimit = options.FrameLimit;
        try
        {
            options.SetFieldOfView(500);
            Check(options.FieldOfView == 100, "FOV must clamp at the supported maximum.");
            options.SetFieldOfView(-50);
            Check(options.FieldOfView == 65, "FOV must clamp at the supported minimum.");
            options.SetFieldOfView(float.NaN);
            Check(options.FieldOfView == 65, "Invalid preference values must not poison the camera FOV.");
            options.ToggleInvert(); options.ToggleMotion(); options.ToggleFrameLimit();
            Check(options.InvertY != originalInvert && options.ReducedMotion != originalMotion && options.FrameLimit != originalLimit, "Accessibility controls must toggle independently.");
            for (int preset = 0; preset < 3; preset++)
            {
                options.SetGraphics(preset);
                var pipeline = QualitySettings.renderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                Check(pipeline != null && !AssetDatabase.Contains(pipeline), "Quality changes must target a runtime copy, not a project asset.");
                Check(Mathf.Approximately(pipeline.renderScale, preset == 0 ? 0.75f : preset == 1 ? 1f : 1.15f), "Graphics preset must apply its render scale.");
            }
        }
        finally
        {
            options.SetFieldOfView(originalFov);
            options.SetGraphics(originalGraphics);
            if (options.InvertY != originalInvert) options.ToggleInvert();
            if (options.ReducedMotion != originalMotion) options.ToggleMotion();
            if (options.FrameLimit != originalLimit) options.ToggleFrameLimit();
            options.Save();
        }
    }

    private static void Teleport(Vector3 position)
    {
        var controller = session.player.GetComponent<CharacterController>();
        controller.enabled = false;
        session.player.transform.position = position;
        controller.enabled = true;
    }

    private static void ClearWave()
    {
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>()) enemy.TakeDamage(10000);
    }

    private static void ValidateWaveRoles()
    {
        bool runner = false, juggernaut = false, marksman = false;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
        {
            runner |= enemy.Role == GunQuest.Game.EnemyRole.Runner;
            juggernaut |= enemy.Role == GunQuest.Game.EnemyRole.Juggernaut;
            marksman |= enemy.Role == GunQuest.Game.EnemyRole.Marksman;
        }
        if (session.Wave >= 2) Check(runner, "Wave two and later must field runners.");
        if (session.Wave >= 3) Check(juggernaut, "Wave three and later must field a juggernaut.");
        if (session.Wave >= 4) Check(marksman, "Wave four and later must field a marksman.");
    }

    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Directory.CreateDirectory("Logs/Screenshots");
        var cam = session.weapon.aimCamera;
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        var texture = new RenderTexture(1600, 900, 24);
        var previous = RenderTexture.active;
        var oldTarget = cam.targetTexture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.1f;
        cam.targetTexture = texture;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = texture;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes("Logs/Screenshots/" + name + ".png", image.EncodeToPNG());
        cam.targetTexture = oldTarget;
        RenderTexture.active = previous;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(image);
        texture.Release();
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void Finish(int code)
    {
        UnityEditor.SessionState.SetBool(RunningKey, false);
        PlayerPrefs.SetInt(ValidationBestKey, UnityEditor.SessionState.GetInt("GunQuest.Validation.Best", 0));
        PlayerPrefs.SetInt(ValidationClearedKey, UnityEditor.SessionState.GetInt("GunQuest.Validation.Cleared", 0));
        PlayerPrefs.SetInt("GunQuest.Difficulty", UnityEditor.SessionState.GetInt("GunQuest.Validation.Difficulty", 1));
        PlayerPrefs.SetInt("GunQuest.CampaignComplete", UnityEditor.SessionState.GetInt("GunQuest.Validation.CampaignComplete", 0));
        PlayerPrefs.Save();
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
