using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GunQuest.Game;

namespace GunQuest.Game
{
    public enum SessionState { Menu, Playing, Paused, Victory, Defeat }
}

public sealed class GameSession : MonoBehaviour
{
    public PlayerHealth player;
    public PlayerWeapon weapon;
    public Enemy enemyPrefab;
    public Transform[] spawnPoints;
    [Min(1)] public int totalWaves = 5;
    public SessionState State { get; private set; } = SessionState.Menu;
    public int Wave { get; private set; }
    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int BestScore { get; private set; }
    public int EnemiesRemaining => enemies.Count;
    public float NextWaveIn => Mathf.Max(0f, waveAt - Time.time);
    public float Elapsed { get; private set; }
    public string Notice { get; private set; } = "";
    public event System.Action StateChanged;
    private readonly HashSet<EnemyHealth> enemies = new HashSet<EnemyHealth>();
    private float waveAt;
    private float noticeUntil;

    private void Awake()
    {
        BestScore = PlayerPrefs.GetInt("GunQuest.BestScore", 0);
        player.Died += OnPlayerDied;
        SetState(SessionState.Menu);
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
        Elapsed += Time.deltaTime;
        if (Time.time > noticeUntil) Notice = "";
        if (player.transform.position.y < -10f) player.TakeDamage(player.maxHealth);
        if (State != SessionState.Playing) return;
        if (enemies.Count == 0 && Time.time >= waveAt) SpawnWave();
    }

    private void SpawnWave()
    {
        if (Wave >= totalWaves) { Finish(SessionState.Victory); return; }
        Wave++;
        int count = 2 + Wave * 2;
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
            enemy.fireRate = Mathf.Max(0.65f, 1.8f - Wave * 0.15f);
            enemy.Agent.speed = 2.5f + Wave * 0.2f;
            var health = enemy.GetComponent<EnemyHealth>();
            health.Configure(68f + Wave * 9f);
            health.Died += OnEnemyDied;
            enemies.Add(health);
        }
        if (enemies.Count == 0)
        {
            Debug.LogError("No valid arena spawn points on the NavMesh.");
            Finish(SessionState.Defeat);
            return;
        }
        Announce($"WAVE {Wave:00} / {enemies.Count} hostiles approaching");
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        enemy.Died -= OnEnemyDied;
        if (!enemies.Remove(enemy) || State != SessionState.Playing) return;
        Kills++;
        Score += 100 + Wave * 25;
        if (enemies.Count > 0) return;
        Score += 250;
        if (Wave >= totalWaves) { Finish(SessionState.Victory); return; }
        player.RestoreHealth(20f);
        weapon.Ammo.Supply(60);
        waveAt = Time.time + 7f;
        Announce("SECTOR CLEAR / +20 health  +60 reserve ammo");
    }

    public void Announce(string message) { Notice = message; noticeUntil = Time.time + 4f; }
    private void OnPlayerDied() => Finish(SessionState.Defeat);

    private void Finish(SessionState result)
    {
        if (State == SessionState.Victory || State == SessionState.Defeat) return;
        if (result == SessionState.Victory) Score += Mathf.RoundToInt(player.GetCurrentHealth() * 10f);
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt("GunQuest.BestScore", BestScore);
            PlayerPrefs.Save();
        }
        SetState(result);
    }

    public void TogglePause()
    {
        if (State == SessionState.Playing) SetState(SessionState.Paused);
        else if (State == SessionState.Paused) SetState(SessionState.Playing);
    }

    private void SetState(SessionState value)
    {
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
        foreach (var enemy in enemies) if (enemy != null) enemy.Died -= OnEnemyDied;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
