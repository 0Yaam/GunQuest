using GunQuest.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeapon : MonoBehaviour
{
    public Camera aimCamera;
    public Transform muzzle;
    [Min(1f)] public float damage = 34f;
    [Min(0.01f)] public float shotInterval = 0.12f;
    [Min(0.1f)] public float reloadDuration = 1.5f;
    public LayerMask hitMask = ~0;
    public AmmoMagazine Ammo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadProgress => IsReloading ? Mathf.Clamp01(1f - (reloadEnd - Time.time) / reloadDuration) : 0f;
    public event System.Action<bool> Hit;
    public event System.Action Fired;
    private float nextShot;
    private float reloadEnd;
    private PlayerHealth health;

    private void Awake()
    {
        Ammo = new AmmoMagazine(30, 120);
        health = GetComponent<PlayerHealth>();
        if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (health != null && health.IsDead) { IsReloading = false; return; }
        if (IsReloading && Time.time >= reloadEnd)
        {
            Ammo.Reload();
            IsReloading = false;
        }
    }

    public void BeginReload()
    {
        if (Time.timeScale == 0f || IsReloading || !Ammo.CanReload || (health != null && health.IsDead)) return;
        IsReloading = true;
        reloadEnd = Time.time + reloadDuration;
    }

    public bool TryFire()
    {
        if (aimCamera == null || Time.timeScale == 0f || IsReloading || Time.time < nextShot || (health != null && health.IsDead)) return false;
        if (!Ammo.TryFire()) { BeginReload(); return false; }
        nextShot = Time.time + shotInterval;
        var ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 end = ray.GetPoint(150f);
        var hits = Physics.RaycastAll(ray, 150f, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            end = hit.point;
            var target = hit.collider.GetComponentInParent<EnemyHealth>();
            if (target != null && !target.IsDead) Hit?.Invoke(target.TakeDamage(damage));
            CombatFeedback.Impact(hit.point, hit.normal, target != null);
            break;
        }
        CombatFeedback.Tracer(muzzle != null ? muzzle.position : ray.origin + ray.direction * 0.5f, end, new Color(0.3f, 1f, 0.85f));
        Fired?.Invoke();
        return true;
    }

    private void OnDisable() => IsReloading = false;
}
