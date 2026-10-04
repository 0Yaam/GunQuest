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
    private static readonly RaycastHit[] Hits = new RaycastHit[16];

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
        int count = Physics.RaycastNonAlloc(transform.position, transform.forward, Hits, distance, ~0, QueryTriggerInteraction.Ignore);
        int nearest = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (ShouldIgnore(Hits[i].collider) || Hits[i].distance >= nearestDistance) continue;
            nearest = i;
            nearestDistance = Hits[i].distance;
        }
        if (nearest >= 0 && Consume(Hits[nearest].collider)) return;
        transform.position += transform.forward * distance;
    }

    void OnTriggerEnter(Collider other)
    {
        Consume(other);
    }

    private bool Consume(Collider other)
    {
        if (consumed || ShouldIgnore(other)) return false;
        consumed = true;
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
        Destroy(gameObject);
        return true;
    }

    private bool ShouldIgnore(Collider other) => other == null || other.isTrigger || other.transform.IsChildOf(transform) ||
        (owner != null && other.transform.IsChildOf(owner)) || other.GetComponentInParent<Bullet>() != null;
}
