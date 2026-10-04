using UnityEngine;
using UnityEngine.AI;
using GunQuest.Game;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(StateMachine))]
public class Enemy : MonoBehaviour
{
    private StateMachine stateMachine;
    private NavMeshAgent agent;
    private GameObject player;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float nextSightCheck;
    private bool canSeePlayer;

    public NavMeshAgent Agent => agent;
    public GameObject Player => player;
    public StateMachine StateMachine => stateMachine;
    public EnemyRole Role { get; set; }

    [Header("Patrol Settings")]
    public Path path;
    public bool huntPlayer;
    private float nextHunt;

    [Header("Sight Settings")]
    public float sightDistance = 20f;
    public float fieldOfView = 85f;
    public float eyeHeight = 1.6f;

    [Header("Weapon & Combat")]
    public Transform gunBarrel;
    public GameObject bulletPrefab;
    [Range(0.1f, 10f)]
    public float fireRate = 1.2f;
    public float bulletDamage = 15f;
    public float bulletSpeed = 35f;

    [Header("Debug")]
    [SerializeField]
    private string currentState;

    public Vector3 LastKnownPosition { get; set; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = GetComponent<StateMachine>();
        if (GetComponent<EnemyHealth>() == null) gameObject.AddComponent<EnemyHealth>();
    }

    void Start()
    {
        // Find player in scene if not assigned
        PlayerMotor playerMotor = Object.FindAnyObjectByType<PlayerMotor>();
        if (playerMotor != null)
        {
            player = playerMotor.gameObject;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
            nextSightCheck = Time.time + Random.Range(0f, 0.12f);
        }

        stateMachine.Initialise();
    }

    void Update()
    {
        if (huntPlayer && player != null && agent.isOnNavMesh && Time.time >= nextHunt && !(stateMachine.activeState is AttackState))
        {
            agent.SetDestination(playerTransform.position);
            nextHunt = Time.time + 0.5f;
        }
    }

    public void SetCurrentState(string value) => currentState = value;

    public bool CanSeePlayer()
    {
        if (playerTransform == null || (playerHealth != null && playerHealth.IsDead))
        {
            canSeePlayer = false;
            return false;
        }

        if (Time.time < nextSightCheck) return canSeePlayer;
        nextSightCheck = Time.time + 0.12f;

        Vector3 eyePos = transform.position + (Vector3.up * eyeHeight);
        Vector3 targetPos = playerTransform.position + Vector3.up;
        Vector3 directionToPlayer = targetPos - eyePos;
        float sqrDistance = directionToPlayer.sqrMagnitude;

        if (sqrDistance <= sightDistance * sightDistance)
        {
            float distanceToPlayer = Mathf.Sqrt(sqrDistance);
            Vector3 sightDirection = directionToPlayer / Mathf.Max(distanceToPlayer, 0.001f);
            float minimumDot = Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (Vector3.Dot(transform.forward, sightDirection) >= minimumDot)
            {
                Ray ray = new Ray(eyePos, sightDirection);
                if (Physics.Raycast(ray, out RaycastHit hit, distanceToPlayer, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        Debug.DrawRay(ray.origin, ray.direction * distanceToPlayer, Color.red);
                        canSeePlayer = true;
                        return canSeePlayer;
                    }
                }
            }
        }

        canSeePlayer = false;
        return canSeePlayer;
    }
}
