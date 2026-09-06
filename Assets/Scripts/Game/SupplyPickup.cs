using UnityEngine;

public sealed class SupplyPickup : MonoBehaviour
{
    public bool heals;
    public float respawnSeconds = 20f;
    private float availableAt;
    private Renderer[] visuals;
    private float baseY;

    private void Awake()
    {
        visuals = GetComponentsInChildren<Renderer>();
        baseY = transform.position.y;
    }

    private void Update()
    {
        bool available = Time.time >= availableAt;
        foreach (var visual in visuals) visual.enabled = available;
        if (!available) return;
        transform.Rotate(Vector3.up, 35f * Time.deltaTime);
        var position = transform.position;
        position.y = baseY + Mathf.Sin(Time.time * 2f) * 0.12f;
        transform.position = position;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.timeScale == 0f || Time.time < availableAt) return;
        var player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;
        if (heals)
        {
            if (player.GetCurrentHealth() >= player.maxHealth) return;
            player.RestoreHealth(35f);
        }
        else
        {
            var weapon = player.GetComponent<PlayerWeapon>();
            if (weapon == null || weapon.Ammo.Reserve >= 999) return;
            weapon.Ammo.Supply(60);
        }
        availableAt = Time.time + respawnSeconds;
        Object.FindFirstObjectByType<GameSession>()?.Announce(heals ? "MEDKIT / +35 health" : "AMMO CACHE / +60 rounds");
    }
}
