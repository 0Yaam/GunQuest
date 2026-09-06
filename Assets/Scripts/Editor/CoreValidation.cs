using System;
using GunQuest.Combat;
using UnityEditor;
using UnityEngine;

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
        Debug.Log("GUNQUEST CORE VALIDATION PASSED: ammo conservation, supply bounds, health clamps, one-shot death.");
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
