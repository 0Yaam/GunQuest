using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyHealth : MonoBehaviour
{
    public event System.Action<EnemyHealth> Died;
    [Min(1f)] public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private float hitFlash;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        properties = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (hitFlash <= 0f) return;
        hitFlash -= Time.unscaledDeltaTime;
        float intensity = Mathf.Clamp01(hitFlash / 0.09f);
        properties.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(1f, 0.25f, 0.08f), intensity));
        foreach (var visual in renderers) visual.SetPropertyBlock(properties);
        if (hitFlash <= 0f) foreach (var visual in renderers) visual.SetPropertyBlock(null);
    }

    public void Configure(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        CurrentHealth = maxHealth;
    }

    public bool TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f || !float.IsFinite(damage)) return false;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        hitFlash = 0.09f;
        if (CurrentHealth > 0f) return false;
        IsDead = true;
        if (TryGetComponent<StateMachine>(out var machine)) machine.enabled = false;
        if (TryGetComponent<NavMeshAgent>(out var agent)) agent.enabled = false;
        if (TryGetComponent<Enemy>(out var enemy)) enemy.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        StartCoroutine(DeathAnimation());
        Died?.Invoke(this);
        return true;
    }

    private System.Collections.IEnumerator DeathAnimation()
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.75f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.75f);
            transform.localScale = Vector3.Lerp(start, new Vector3(start.x * 1.25f, 0.05f, start.z * 1.25f), t * t);
            transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 220f, Space.World);
            yield return null;
        }
        Destroy(gameObject);
    }
}
