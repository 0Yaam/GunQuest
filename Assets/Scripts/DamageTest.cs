using UnityEngine;

public class DamageTest : MonoBehaviour
{
    [SerializeField]
    private float damageAmount = 15f;
    [SerializeField]
    private bool heals;
    [SerializeField]
    private float healAmount = 20f;

    public void Configure(bool asHealingZone) => heals = asHealingZone;

    // Apply one configured effect when the player enters this tutorial trigger.
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth health))
        {
            if (heals) health.RestoreHealth(healAmount);
            else health.TakeDamage(damageAmount);
        }
    }
}

