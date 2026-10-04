using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public BaseState activeState;
    private Enemy enemy;

    private void Awake() => enemy = GetComponent<Enemy>();

    public void Initialise()
    {
        ChangeState(new PatrolState());
    }

    void Update()
    {
        if (activeState != null)
        {
            activeState.Perform();
        }
    }

    public void ChangeState(BaseState newState)
    {
        if (activeState != null)
        {
            activeState.Exit();
        }

        activeState = newState;

        if (activeState != null)
        {
            activeState.stateMachine = this;
            activeState.enemy = enemy;
            activeState.Enter();
            enemy?.SetCurrentState(activeState.GetType().Name);
        }
    }
}
