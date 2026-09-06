using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private float speed = 35f;
    [SerializeField]
    private float damage = 15f;
    [SerializeField]
    private float lifeTime = 5f;
    private Transform owner;
    private bool consumed;

    public void SetOwner(Transform value) => owner = value;
    public void Configure(float newDamage, float newSpeed)
    {
        damage = Mathf.Max(0f, newDamage);
        speed = Mathf.Max(1f, newSpeed);
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float distance = speed * Time.deltaTime;
        if (distance <= 0f || consumed) return;
        var hits = Physics.RaycastAll(transform.position, transform.forward, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (Consume(hit.collider)) return;
        }
        transform.position += transform.forward * distance;
    }

    void OnTriggerEnter(Collider other)
    {
        Consume(other);
    }

    private bool Consume(Collider other)
    {
        if (consumed || other.isTrigger || other.transform.IsChildOf(transform) ||
            (owner != null && other.transform.IsChildOf(owner)) || other.GetComponentInParent<Bullet>() != null) return false;
        consumed = true;
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
        Destroy(gameObject);
        return true;
    }
}
