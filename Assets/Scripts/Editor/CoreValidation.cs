using System;
using GunQuest.Combat;
using UnityEditor;
using UnityEngine;
using GunQuest.Game;

public static class CoreValidation
{
    [MenuItem("GunQuest/Validation/Validate combat rules")]
    public static void Run()
    {
        var ammo = new AmmoMagazine(3, 2);
        Require(ammo.TryFire() && ammo.TryFire() && ammo.TryFire(), "Loaded rounds must fire.");
        Require(!ammo.TryFire() && ammo.Loaded == 0, "Empty fire must not underflow.");
        ammo.Reload();
        Require(ammo.Loaded == 2 && ammo.Reserve == 0, "Partial reload must conserve ammo.");
        ammo.Reload();
        Require(ammo.Loaded == 2 && !ammo.CanReload, "Empty reserve must not create rounds.");
        ammo.Supply(-20);
        Require(ammo.Reserve == 0, "Negative supplies must be ignored.");
        ammo.Supply(int.MaxValue);
        Require(ammo.Reserve == 999, "Supply must be bounded without overflow.");
        ammo.Reload();
        Require(ammo.Loaded == 3 && ammo.Reserve == 998, "Tactical reload must transfer only missing rounds.");
        int recruitEnemies = 0, operatorEnemies = 0, veteranEnemies = 0;
        for (int wave = 1; wave <= 5; wave++)
        {
            recruitEnemies += GameSession.EnemyCountForWave(Difficulty.Recruit, wave);
            operatorEnemies += GameSession.EnemyCountForWave(Difficulty.Operator, wave);
            veteranEnemies += GameSession.EnemyCountForWave(Difficulty.Veteran, wave);
        }
        Require(recruitEnemies == 25 && operatorEnemies == 40 && veteranEnemies == 55,
            "Threat profiles must preserve their intended five-wave progression.");
        Require(GameSession.RoleForSpawn(1, 6) == EnemyRole.Striker, "The opening wave must teach the standard striker.");
        Require(GameSession.RoleForSpawn(2, 2) == EnemyRole.Runner, "Runners must enter from wave two.");
        Require(GameSession.RoleForSpawn(3, 4) == EnemyRole.Juggernaut, "Juggernauts must enter from wave three.");
        Require(GameSession.RoleForSpawn(4, 6) == EnemyRole.Marksman, "Marksmen must enter from wave four.");
        Require(GameSession.CalculateRank(80f, 90f, 120f, Difficulty.Operator) == "S", "Excellent runs must receive an S rank.");
        Require(GameSession.CalculateRank(20f, 20f, 500f, Difficulty.Operator) == "C", "Weak runs must receive a C rank.");

        var go = new GameObject("Health validation");
        try
        {
            var health = go.AddComponent<PlayerHealth>();
            health.SendMessage("Awake");
            int deaths = 0;
            health.Died += () => deaths++;
            health.TakeDamage(-10);
            health.TakeDamage(float.NaN);
            Require(health.GetCurrentHealth() == 100f, "Invalid damage must be ignored.");
            health.TakeDamage(30);
            health.RestoreHealth(999);
            Require(health.GetCurrentHealth() == 100f, "Healing must clamp immediately.");
            health.TakeDamage(999);
            health.TakeDamage(1);
            health.RestoreHealth(100);
            Require(health.IsDead && health.GetCurrentHealth() == 0f && deaths == 1, "Death must fire once and prevent revival.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        Debug.Log("GUNQUEST CORE VALIDATION PASSED: ammo conservation, threat profiles, health clamps, one-shot death.");
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
