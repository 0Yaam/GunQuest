using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyVisualController : MonoBehaviour
{
    private Enemy enemy;
    private Animator animator;
    private int currentState;
    private bool dying;
    private static readonly int Idle = Animator.StringToHash("Idle_Shoot_Ar");
    private static readonly int Walk = Animator.StringToHash("WalkFront_Shoot_AR");
    private static readonly int Fire = Animator.StringToHash("Shoot_Autoshot_AR");
    private static readonly int Death = Animator.StringToHash("Die");

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            Play(Idle, 0f);
        }
    }

    private void Update()
    {
        if (dying || animator == null || enemy == null || enemy.Agent == null || !enemy.Agent.enabled) return;
        int wanted = enemy.StateMachine != null && enemy.StateMachine.activeState is AttackState
            ? Fire
            : enemy.Agent.velocity.sqrMagnitude > 0.05f ? Walk : Idle;
        Play(wanted, 0.16f);
    }

    public void Die()
    {
        dying = true;
        Play(Death, 0.08f);
    }

    private void Play(int state, float fade)
    {
        if (animator == null || currentState == state || !animator.HasState(0, state)) return;
        currentState = state;
        if (fade <= 0f) animator.Play(state, 0, 0f);
        else animator.CrossFade(state, fade, 0);
    }
}
