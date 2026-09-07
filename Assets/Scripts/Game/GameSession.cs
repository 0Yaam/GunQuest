using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GunQuest.Game;

namespace GunQuest.Game
{
    public enum SessionState { Menu, Playing, Paused, Victory, Defeat }
    public enum Difficulty { Recruit, Operator, Veteran }
    public enum EnemyRole { Striker, Runner, Juggernaut, Marksman }
}

public sealed class GameSession : MonoBehaviour
{
    public static readonly string[] MissionScenes = { "Outpost", "Blackwood", "Skyline" };
    public static readonly string[] MissionNames = { "OUTPOST", "BLACKWOOD", "SKYLINE" };

    [Header("Mission")]
    public string missionCode = "01";
    public string missionName = "OUTPOST";
    [TextArea(2, 3)] public string missionDescription = "An isolated station. Five hostile waves.\nOne operator to hold the perimeter.";
    [TextArea(2, 3)] public string victoryDescription = "All hostile waves eliminated.\nThe outpost is yours.";
    public Color missionAccent = new Color(0.28f, 0.94f, 0.79f);
    [Header("Combat")]
    public PlayerHealth player;
    public PlayerWeapon weapon;
    public Enemy enemyPrefab;
    public Transform[] spawnPoints;
    [Min(1)] public int totalWaves = 5;
    public SessionState State { get; private set; } = SessionState.Menu;
    public int Wave { get; private set; }
    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int ShotsFired { get; private set; }
    public int ShotsHit { get; private set; }
    public MissionObjectives Objectives { get; private set; }
    public PlayerOptions Options { get; private set; }
    public int UpgradeCredits { get; private set; }
    private readonly int[] upgradeLevels = new int[3];
    public bool CanUpgrade => Wave > 0 && Wave < totalWaves && enemies.Count == 0 &&
        (State == SessionState.Playing || State == SessionState.Paused);
    public int UpgradeLevel(int index) => index >= 0 && index < 3 ? upgradeLevels[index] : 0;
    public float Accuracy => ShotsFired > 0 ? ShotsHit * 100f / ShotsFired : 0f;
    public string PerformanceRank => State == SessionState.Victory ? CalculateRank(Accuracy, player.GetCurrentHealth() / player.maxHealth * 100f, Elapsed, Difficulty) : "--";
    public int BestScore { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public int MissionIndex
    {
        get
        {
            string scene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < MissionScenes.Length; i++) if (MissionScenes[i] == scene) return i;
            return 0;
        }
    }
    public bool HasNextMission => MissionIndex < MissionScenes.Length - 1;
    public string BestScoreKey => $"GunQuest.BestScore.{SceneManager.GetActiveScene().name}.{Difficulty}";
    public int EnemiesRemaining => enemies.Count;
    public string ThreatSummary
    {
        get
        {
            string summary = "";
            if (roleCounts[(int)EnemyRole.Runner] > 0) summary += $"{roleCounts[(int)EnemyRole.Runner]} RUN";
            if (roleCounts[(int)EnemyRole.Juggernaut] > 0) summary += (summary.Length > 0 ? "  /  " : "") + $"{roleCounts[(int)EnemyRole.Juggernaut]} JUG";
            if (roleCounts[(int)EnemyRole.Marksman] > 0) summary += (summary.Length > 0 ? "  /  " : "") + $"{roleCounts[(int)EnemyRole.Marksman]} MRK";
            return summary;
        }
    }
    public float NextWaveIn => Mathf.Max(0f, waveAt - Time.time);
    public float Elapsed { get; private set; }
    public string Notice { get; private set; } = "";
    public event System.Action StateChanged;
    public event System.Action Transmission;
    private readonly HashSet<EnemyHealth> enemies = new HashSet<EnemyHealth>();
    private readonly int[] roleCounts = new int[4];
    private float waveAt;
    private float noticeUntil;

    private void Awake()
    {
        Options = GetComponent<PlayerOptions>();
        if (Options == null) Options = gameObject.AddComponent<PlayerOptions>();
        Difficulty = (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt("GunQuest.Difficulty", 1), 0, 2);
        LoadBestScore();
        player.Died += OnPlayerDied;
        weapon.Fired += OnWeaponFired;
        weapon.Hit += OnWeaponHit;
        if (GetComponent<MissionSoundscape>() == null) gameObject.AddComponent<MissionSoundscape>();
        SetState(SessionState.Menu);
    }

    private void Start()
    {
        Objectives = GetComponent<MissionObjectives>();
        if (Objectives == null) Objectives = gameObject.AddComponent<MissionObjectives>();
        Objectives.Initialize(this);
        if (player.GetComponent<OperatorAudio>() == null) player.gameObject.AddComponent<OperatorAudio>();
    }

    public bool PurchaseUpgrade(int index)
    {
        if (!CanUpgrade || UpgradeCredits <= 0 || index < 0 || index >= 3 || upgradeLevels[index] >= 3) return false;
        UpgradeCredits--;
        upgradeLevels[index]++;
        if (index == 0) weapon.damage += 5f;
        else if (index == 1) weapon.reloadDuration = Mathf.Max(0.6f, weapon.reloadDuration - 0.25f);
        else { player.maxHealth += 15f; player.RestoreHealth(30f); }
        Announce(index == 0 ? "UPGRADE / Rifle damage +5" : index == 1 ? "UPGRADE / Reload time -0.25s" : "UPGRADE / Max health +15, recover 30 health");
        return true;
    }

    public void CallNextWave()
    {
        if (State == SessionState.Playing && CanUpgrade) waveAt = Time.time;
    }

    public void CompleteExtraction()
    {
        if (State == SessionState.Playing && Wave >= totalWaves && enemies.Count == 0 && Objectives != null && Objectives.IsComplete)
            Finish(SessionState.Victory);
    }

    public void SetDifficulty(Difficulty value)
    {
        if (State != SessionState.Menu) return;
        Difficulty = value;
        PlayerPrefs.SetInt("GunQuest.Difficulty", (int)value);
        LoadBestScore();
        StateChanged?.Invoke();
    }

    private void LoadBestScore() => BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    public static bool IsMissionCleared(int index) => index >= 0 && index < MissionScenes.Length && PlayerPrefs.GetInt("GunQuest.Cleared." + MissionScenes[index], 0) == 1;
    public static int ClearedMissionCount
    {
        get { int count = 0; for (int i = 0; i < MissionScenes.Length; i++) if (IsMissionCleared(i)) count++; return count; }
    }

    public static string CalculateRank(float accuracy, float health, float seconds, Difficulty difficulty)
    {
        float rating = Mathf.Clamp(accuracy, 0f, 100f) * 0.55f + Mathf.Clamp(health, 0f, 100f) * 0.35f;
        rating += seconds <= 150f ? 10f : seconds <= 240f ? 6f : seconds <= 360f ? 3f : 0f;
        if (difficulty == Difficulty.Veteran) rating += 5f;
        else if (difficulty == Difficulty.Recruit) rating -= 4f;
        if (rating >= 82f) return "S";
        if (rating >= 68f) return "A";
        if (rating >= 52f) return "B";
        return "C";
    }

    public static int EnemyCountForWave(Difficulty difficulty, int wave)
    {
        wave = Mathf.Max(1, wave);
        return difficulty switch
        {
            Difficulty.Recruit => 2 + wave,
            Difficulty.Veteran => 2 + wave * 3,
            _ => 2 + wave * 2
        };
    }

    public static EnemyRole RoleForSpawn(int wave, int index)
    {
        if (wave >= 4 && index % 7 == 6) return EnemyRole.Marksman;
        if (wave >= 3 && index % 5 == 4) return EnemyRole.Juggernaut;
        if (wave >= 2 && index % 3 == 2) return EnemyRole.Runner;
        return EnemyRole.Striker;
    }

    public void StartRun()
    {
        if (State != SessionState.Menu) return;
        waveAt = Time.time + 3f;
        SetState(SessionState.Playing);
        Announce("INCOMING SIGNAL / Prepare your position");
    }

    private void Update()
    {
        bool pause = (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        if (pause) TogglePause();
        if (State != SessionState.Playing) return;
        if (CanUpgrade && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) PurchaseUpgrade(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) PurchaseUpgrade(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) PurchaseUpgrade(2);
            if (Keyboard.current.enterKey.wasPressedThisFrame) CallNextWave();
        }
        Elapsed += Time.deltaTime;
        if (Time.time > noticeUntil) Notice = "";
        if (player.transform.position.y < -10f) player.TakeDamage(player.maxHealth);
        if (State != SessionState.Playing) return;
        if (enemies.Count == 0 && Wave < totalWaves && Time.time >= waveAt) SpawnWave();
    }

    private void SpawnWave()
    {
        if (Wave >= totalWaves) return;
        Wave++;
        int count = EnemyCountForWave(Difficulty, Wave);
        float difficultyScale = Difficulty == Difficulty.Recruit ? 0.78f : Difficulty == Difficulty.Veteran ? 1.28f : 1f;
        for (int i = 0; i < count; i++)
        {
            // Prefer distant entrances so a wave never materializes beside the player.
            Transform spawn = null;
            float bestDistance = -1f;
            for (int j = 0; j < spawnPoints.Length; j++)
            {
                var candidate = spawnPoints[(i + j) % spawnPoints.Length];
                float distance = Vector3.Distance(candidate.position, player.transform.position);
                if (distance > bestDistance) { bestDistance = distance; spawn = candidate; }
                if (distance > 16f && j >= i % spawnPoints.Length) { spawn = candidate; break; }
            }
            if (spawn == null || !NavMesh.SamplePosition(spawn.position + new Vector3(i % 2, 0f, i / 2), out var hit, 5f, NavMesh.AllAreas)) continue;
            var enemy = Instantiate(enemyPrefab, hit.position, spawn.rotation);
            enemy.huntPlayer = true;
            enemy.fireRate = Mathf.Max(0.5f, (1.8f - Wave * 0.15f) / difficultyScale);
            enemy.Agent.speed = (2.5f + Wave * 0.2f) * Mathf.Lerp(0.9f, 1.08f, (difficultyScale - 0.78f) / 0.5f);
            EnemyRole role = RoleForSpawn(Wave, i);
            ConfigureRole(enemy, role);
            var health = enemy.GetComponent<EnemyHealth>();
            float roleHealth = role == EnemyRole.Runner ? 0.68f : role == EnemyRole.Juggernaut ? 1.85f : role == EnemyRole.Marksman ? 0.82f : 1f;
            Color roleColor = role == EnemyRole.Runner ? new Color(0.25f, 1f, 0.35f) : role == EnemyRole.Juggernaut ? new Color(1f, 0.16f, 0.06f) : role == EnemyRole.Marksman ? new Color(0.15f, 0.72f, 1f) : new Color(1f, 0.38f, 0.08f);
            health.Configure((68f + Wave * 9f) * difficultyScale * roleHealth, roleColor);
            health.Died += OnEnemyDied;
            enemies.Add(health);
            roleCounts[(int)role]++;
        }
        if (enemies.Count == 0)
        {
            Debug.LogError("No valid arena spawn points on the NavMesh.");
            Finish(SessionState.Defeat);
            return;
        }
        string specialThreats = ThreatSummary;
        Announce($"WAVE {Wave:00} / {enemies.Count} hostiles" + (specialThreats.Length > 0 ? " / " + specialThreats : " approaching"));
    }

    private static void ConfigureRole(Enemy enemy, EnemyRole role)
    {
        enemy.Role = role;
        switch (role)
        {
            case EnemyRole.Runner:
                enemy.transform.localScale = Vector3.one * 0.82f;
                enemy.Agent.speed *= 1.35f;
                enemy.Agent.stoppingDistance = 4.5f;
                enemy.fireRate *= 0.82f;
                enemy.bulletDamage = 10f;
                enemy.bulletSpeed = 42f;
                break;
            case EnemyRole.Juggernaut:
                enemy.transform.localScale = Vector3.one * 1.24f;
                enemy.Agent.speed *= 0.72f;
                enemy.Agent.stoppingDistance = 7f;
                enemy.fireRate *= 1.18f;
                enemy.bulletDamage = 22f;
                enemy.bulletSpeed = 30f;
                break;
            case EnemyRole.Marksman:
                enemy.transform.localScale = new Vector3(0.92f, 1.08f, 0.92f);
                enemy.Agent.speed *= 0.82f;
                enemy.Agent.stoppingDistance = 16f;
                enemy.sightDistance = 68f;
                enemy.fireRate *= 1.65f;
                enemy.bulletDamage = 30f;
                enemy.bulletSpeed = 68f;
                break;
            default:
                enemy.bulletDamage = 15f;
                enemy.bulletSpeed = 35f;
                break;
        }
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        enemy.Died -= OnEnemyDied;
        if (!enemies.Remove(enemy) || State != SessionState.Playing) return;
        Kills++;
        EnemyRole role = enemy.TryGetComponent<Enemy>(out var defeated) ? defeated.Role : EnemyRole.Striker;
        roleCounts[(int)role] = Mathf.Max(0, roleCounts[(int)role] - 1);
        float scoreMultiplier = Difficulty == Difficulty.Recruit ? 0.75f : Difficulty == Difficulty.Veteran ? 1.5f : 1f;
        float roleReward = role == EnemyRole.Runner ? 1.15f : role == EnemyRole.Juggernaut ? 2f : role == EnemyRole.Marksman ? 1.75f : 1f;
        Score += Mathf.RoundToInt((100 + Wave * 25) * scoreMultiplier * roleReward);
        if (enemies.Count > 0) return;
        Score += Mathf.RoundToInt(250 * scoreMultiplier);
        if (Wave >= totalWaves)
        {
            Announce("HOSTILES CLEARED / Finish the uplink and reach extraction");
            return;
        }
        UpgradeCredits++;
        float healthReward = Difficulty == Difficulty.Veteran ? 12f : Difficulty == Difficulty.Recruit ? 30f : 20f;
        int ammoReward = Difficulty == Difficulty.Veteran ? 45 : Difficulty == Difficulty.Recruit ? 90 : 60;
        player.RestoreHealth(healthReward);
        weapon.Ammo.Supply(ammoReward);
        waveAt = Time.time + 20f;
        Announce($"SECTOR CLEAR / +{healthReward:0} health  +{ammoReward} ammo  +1 upgrade credit");
    }

    public void Announce(string message) { Notice = message; noticeUntil = Time.time + 4f; Transmission?.Invoke(); }
    private void OnWeaponFired() => ShotsFired++;
    private void OnWeaponHit(bool killed) => ShotsHit++;
    private void OnPlayerDied() => Finish(SessionState.Defeat);

    private void Finish(SessionState result)
    {
        if (State == SessionState.Victory || State == SessionState.Defeat) return;
        if (result == SessionState.Victory)
        {
            Score += Mathf.RoundToInt(player.GetCurrentHealth() * 10f);
            PlayerPrefs.SetInt("GunQuest.Cleared." + SceneManager.GetActiveScene().name, 1);
            if (ClearedMissionCount == MissionScenes.Length) PlayerPrefs.SetInt("GunQuest.CampaignComplete", 1);
        }
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
        }
        PlayerPrefs.Save();
        SetState(result);
    }

    public void TogglePause()
    {
        if (State == SessionState.Playing) SetState(SessionState.Paused);
        else if (State == SessionState.Paused) SetState(SessionState.Playing);
    }

    private void SetState(SessionState value)
    {
        if (Options != null) Options.Save();
        State = value;
        bool playing = value == SessionState.Playing;
        Time.timeScale = playing ? 1f : 0f;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
        StateChanged?.Invoke();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMission(int index)
    {
        if (State != SessionState.Menu && State != SessionState.Victory) return;
        index = Mathf.Clamp(index, 0, MissionScenes.Length - 1);
        string scene = MissionScenes[index];
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogWarning($"Mission scene '{scene}' is not available in this build.");
            return;
        }
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("GunQuest.Mission", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene(scene);
    }

    public void LoadNextMission()
    {
        if (HasNextMission) LoadMission(MissionIndex + 1);
        else Restart();
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && State == SessionState.Playing && !Application.isBatchMode) SetState(SessionState.Paused);
    }

    private void OnDestroy()
    {
        if (player != null) player.Died -= OnPlayerDied;
        if (weapon != null)
        {
            weapon.Fired -= OnWeaponFired;
            weapon.Hit -= OnWeaponHit;
        }
        foreach (var enemy in enemies) if (enemy != null) enemy.Died -= OnEnemyDied;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
