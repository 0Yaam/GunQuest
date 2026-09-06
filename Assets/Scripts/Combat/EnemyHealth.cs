using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyHealth : MonoBehaviour
{
    public event System.Action<EnemyHealth> Died;
    [Min(1f)] public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake() => CurrentHealth = maxHealth;

    public void Configure(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        CurrentHealth = maxHealth;
    }

    public bool TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f || !float.IsFinite(damage)) return false;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (CurrentHealth > 0f) return false;
        IsDead = true;
        if (TryGetComponent<StateMachine>(out var machine)) machine.enabled = false;
        if (TryGetComponent<NavMeshAgent>(out var agent)) agent.enabled = false;
        if (TryGetComponent<Enemy>(out var enemy)) enemy.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        Died?.Invoke(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
