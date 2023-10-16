using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentHealth : MonoBehaviour
{
    public int MaxHealth;
    public int Health;

    public delegate void AgentDiedDelegate(ScoutAgent agent);
    public static event AgentDiedDelegate AgentDied = null;

    public delegate void AgentHitDelegate(ScoutAgent attacker, ScoutAgent attackee, float damgeInflicted);
    public static event AgentHitDelegate AgentHit = null;


    public virtual int GetHealth()
    {
        return Health;
    }

    public virtual void SetHealth(int newHealth)
    {
        Health = Mathf.RoundToInt(Mathf.Clamp(newHealth, 0f, MaxHealth));

        if (Health <= 0)
        {
            if (AgentDied != null)
            {
                AgentDied(GetComponent<ScoutAgent>());
            }
        }
    }

    public virtual void RestoreHealth()
    {
        Health = MaxHealth;
    }

    public virtual void SubtractHealth(int amountToSubtract, ScoutAgent attacker)
    {
        if (Health - Math.Abs(amountToSubtract) <= 0)
        {
            if (AgentHit != null)
            {
                AgentHit(GetComponent<ScoutAgent>(), attacker, Health);
            }
            Health = 0;
        }
        else
        {
            if (AgentHit != null)
            {
                AgentHit(GetComponent<ScoutAgent>(), attacker, amountToSubtract);
            }
            Health -= Math.Abs(amountToSubtract);
        }

        Debug.Log("ATTACK: Subtract health: " + amountToSubtract + " from " + gameObject.name + ". Current Health=" + Health);

        if (Health <= 0)
        {
            if (AgentDied != null)
            {
                AgentDied(GetComponent<ScoutAgent>());
            }
        }

    }

    public virtual void AddHealth(int amountToAdd)
    {
        if (Health + Math.Abs(amountToAdd) >= MaxHealth)
        {
            Health = MaxHealth;
        }
        else
        {
            Health += Math.Abs(amountToAdd);
        }
    }

    public virtual float GetHealthPercent()
    {
        return (float)Health / (float)MaxHealth;
    }
}
