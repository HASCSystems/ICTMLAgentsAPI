using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardsCalculator : MonoBehaviour
{
    protected Dictionary<ScoutAgent, float> rewardBuffers = new Dictionary<ScoutAgent, float>();

    [Tooltip("A reward added whenever a hit taken by agent (usually negative)")]
    public float perHPLostReward = -1f;
    [Tooltip("A reward added whenever attacking another agent (usually positive)")]
    public float perHPLossInflictedReward = 1f;
    [Tooltip("A reward added to the dead agent whenever they die (usually negative)")]
    public float onDiedReward = -10f;
    /*[Tooltip("A reward added whenever killing another agent (usually positive)")]
    public float onKilledReward = 10f;*/

    /// <summary>
    /// Calculate reward based on a no-reward tier, a linear tier, and a constant tier
    /// </summary>
    /// <param name="steps">steps taken</param>
    /// <param name="tier0_steps">Below this, no reward</param>
    /// <param name="tier1_steps">Below this and above tier0_steps, linear reward. Above this, maxReward</param>
    /// <param name="maxReward">Reward to scale tier1 rewards to and max reward to get above tier1_steps</param>
    /// <returns>Reward as float</returns>
    public virtual float CalculateTieredReward(
        int steps, // steps taken
        int tier0_steps, // below this, no reward
        int tier1_steps, // linear reward
        float maxReward
        )
    {
        if ( tier0_steps <= 0 || tier1_steps <= 0 ||
            (tier0_steps <= tier1_steps) )
        {
            throw new System.Exception("Invalid tier step(s)");
        }

        if (steps <= tier0_steps)
        {
            return 0f;
        }
        else if (steps <= tier1_steps)
        {
            return maxReward * (steps - tier0_steps) / (tier1_steps - tier0_steps);
        }
        else
        {
            return maxReward;
        }
    }

    public virtual void AddRewardToBuffer(ScoutAgent agent, float reward)
    {
        if (!rewardBuffers.ContainsKey(agent))
        {
            rewardBuffers.Add(agent, reward);
        }
        else
        {
            rewardBuffers[agent] += reward;
        }
    }

    public virtual float RetrieveAndClearBuffer(ScoutAgent agent)
    {
        if (rewardBuffers.ContainsKey(agent))
        {
            float rew = rewardBuffers[agent];
            rewardBuffers[agent] = 0f;
            return rew;
        }
        else
        {
            return 0f;
        }
    }

    public virtual void ClearAllBuffers()
    {
        foreach(KeyValuePair<ScoutAgent, float> kvp in rewardBuffers)
        {
            rewardBuffers[kvp.Key] = 0f;
        }
    }

}
