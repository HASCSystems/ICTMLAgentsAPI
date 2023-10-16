using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Random = UnityEngine.Random;
using TMPro;

public class GameController_DodgeBall : GameController
{
    
    public override void AgentDied(ScoutAgent deadAgent)
    {
        Debug.Log("AgentDied: " + deadAgent.gameObject.name);

        //SET AGENT/TEAM REWARDS HERE
        int hitTeamID = deadAgent.teamID;
        int throwTeamID = deadAgent.teamID == 0 ? 1 : 0;
        var HitAgentGroup = hitTeamID == 1 ? m_Team1AgentGroup : m_Team0AgentGroup;
        var ThrowAgentGroup = hitTeamID == 1 ? m_Team0AgentGroup : m_Team1AgentGroup;
        //float hitBonus = GameMode == GameModeType.Elimination ? EliminationHitBonus : CTFHitBonus;


        int liveRed = 0;
        foreach (PlayerInfo pi in Team0Players)
        {
            if (pi.Agent.Health.GetHealth() > 0)
                liveRed++;
        }
        int liveBlue = 0;
        foreach (PlayerInfo pi in Team1Players)
        {
            if (pi.Agent.Health.GetHealth() > 0)
                liveBlue++;
        }

        m_NumberOfRedPlayersRemaining = liveRed;
        m_NumberOfBluePlayersRemaining = liveBlue;


        // The current agent was just killed and is the final agent
        if (IS_DEBUG) Debug.Log("m_NumberOfBluePlayersRemaining =" + m_NumberOfBluePlayersRemaining);
        if ((m_NumberOfBluePlayersRemaining == 0) || m_NumberOfRedPlayersRemaining == 0)
        {
            int m_TimeBonus = 1;
            ThrowAgentGroup.AddGroupReward(2.0f - m_TimeBonus * (m_ResetTimer / MaxEnvironmentSteps));
            HitAgentGroup.AddGroupReward(-1.0f);    
            ThrowAgentGroup.EndGroupEpisode();
            HitAgentGroup.EndGroupEpisode();
            print($"Team {throwTeamID} Won");
            int winningTeamID = GetOpposingTeamID(deadAgent.teamID);
            UpdateEpisodeResultDescription(winningTeamID);
            EndGame(winningTeamID);
        }
        // The current agent was just killed but there are other agents
        else
        {
            deadAgent.gameObject.SetActive(false);
        }   
    }
}
