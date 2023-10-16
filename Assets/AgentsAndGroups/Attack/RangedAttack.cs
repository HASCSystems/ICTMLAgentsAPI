using UnityEngine;

public abstract class RangedAttack : MonoBehaviour
{
    public virtual void Attack(AgentTarget targetOpponent, string shooterPos, string targetPos)
    {
        // Override this for attacks
    }

    /// <summary>
    /// When the episode is reset, call this command
    /// </summary>
    public virtual void OnReset()
    {
        // Override this for attacks
    }
}
