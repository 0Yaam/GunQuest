using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private float speed = 35f;
    [SerializeField]
    private float damage = 15f;
    [SerializeField]
    private float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if hit player
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth health))
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Don't collide with other bullets or triggers unless environment
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
