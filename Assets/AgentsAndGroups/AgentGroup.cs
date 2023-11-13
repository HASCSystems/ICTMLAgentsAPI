using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AgentGroup
{
    private static bool IS_DEBUG = false;

    public int index;
    public int lastActedStep = -1; // If the current step doesn't equal this one, then do it.This way, agents in the group can drive action.

    public List<ScoutAgent> agents;
    public bool isSendingAssistanceSignal = false, shelterBound = false;
    public string goalWaypointID, safetyWaypointID;
    public WaypointData goalWaypoint, safetyWaypoint;
    public HeuristicController.SCENARIO_STATE currentScenarioState = HeuristicController.SCENARIO_STATE.INITIAL;

    public delegate void AssistSignalActivationDelegate(int index, bool isOn);
    public static event AssistSignalActivationDelegate onAssistSignal = null;

    protected virtual void Awake()
    {
        WaypointMeshController.onWaypointsSetup += SetupWaypoints;
    }

    protected virtual void OnDestroy()
    {
        WaypointMeshController.onWaypointsSetup -= SetupWaypoints;
    }

    public virtual void Reset()
    {
        SetAssistanceSignal(false);
        SetShelterBound(false);
        SetSpeed(true);
        lastActedStep = -1;
        currentScenarioState = HeuristicController.SCENARIO_STATE.INITIAL;
    }

    public virtual void SetupWaypoints()
    {
        goalWaypoint = WaypointMeshController.GetWaypointData(goalWaypointID);
        safetyWaypoint = WaypointMeshController.GetWaypointData(safetyWaypointID);
    }


    public virtual WaypointData GetCurrentWaypoint()
    {
        foreach (ScoutAgent agent in agents)
        {
            if (agent.Health.GetHealth() > 0)
            {
                return agent.currentWaypoint;
            }
        }
        return null;
    }

    public virtual void SetAssistanceSignal(bool isSet)
    {
        //if (IS_DEBUG) Debug.Log("Scout Group " + index + " assistance signal set to " + isSet);
        isSendingAssistanceSignal = isSet;
        if (onAssistSignal != null)
            onAssistSignal(index, isSet);
    }

    public virtual bool GetAssistanceSignal()
    {
        return isSendingAssistanceSignal;
    }

    public virtual void SetShelterBound(bool isSet)
    {
        shelterBound = isSet;
    }

    public virtual bool GetShelterBound()
    {
        return shelterBound;
    }

    public virtual float GetGroupHealthPercent()
    {
        int totalMaxHealth = 0;
        int totalCurrentHealth = 0;
        foreach(ScoutAgent agent in agents)
        {
            totalMaxHealth += agent.Health.MaxHealth;
            totalCurrentHealth += agent.Health.GetHealth();
        }
        return (float)totalCurrentHealth / (float)totalMaxHealth;
    }

    public virtual void SetSpeed(bool isFast)
    {
        foreach (ScoutAgent agent in agents)
        {
            agent.ChangeMovementSpeed(isFast);
        }
    }

    public virtual Direction MoveToWaypoint(WaypointData waypoint)
    {
        if (waypoint == null) return Directions.NONE;

        foreach (ScoutAgent agent in agents)
        {
            if (agent.Health.GetHealth() > 0)
            {
                WaypointData[] route = WaypointMeshController.GetRoute(agent.currentWaypoint, waypoint);
                if (route == null)
                {
                    /*if (IS_DEBUG) Debug.Log(GetGroupSteps(group) + "steps: MoveToWaypoint:  ROUTE is NULL. " + agent.currentWaypoint.waypointID + " -> " + waypoint.waypointID);*/
                    continue;
                }
                if (route.Length <= 1)
                {
                    //agent.MoveToNextWaypoint(0);
                    /*if (IS_DEBUG) Debug.Log(GetGroupSteps(group) + " steps: MoveToWaypoint:  ROUTE is of length=" + route.Length + ". " + agent.currentWaypoint.waypoint.name + " -> " + waypoint.name);*/
                    return Directions.NONE;
                }
                else
                {
                    //agent.MoveToNextWaypoint(route[1]);
                   /* if (IS_DEBUG) Debug.Log(" steps: MoveToWaypoint: Next=" + (route[1]?.gameObject.name ?? "NULL") + ". " + agent.currentWaypoint.waypoint.name + "-> " + waypoint.name + "; dir=" + (FourConnectedNode.DIRECTION)agent.GetDirectionFromWaypoint(route[1]));*/
                    return WaypointMeshController.GetDirectionFromWaypoint(agent, route[1]);
                }
            }
        }
        return Directions.NONE;

    }

    public virtual List<AgentGroup> GetOtherGroups(AgentGroup groupToExclude)
    {
        List<AgentGroup> allGroups = new List<AgentGroup>(GameController._groups);
        allGroups.Remove(groupToExclude);
        return allGroups;
    }

    public virtual bool DoOtherGroupsNeedAssistance()
    {
        List<AgentGroup> otherGroups = GetOtherGroups(this);
        foreach (AgentGroup ag in otherGroups)
        {
            if (ag.isSendingAssistanceSignal)
                return true;
        }
        return false;
    }

    public virtual ScoutAgent GetTarget()
    {
        foreach (ScoutAgent agent in agents)
        {
            if (agent.Health.GetHealth() > 0)
            {
                return agent.GetTargetAgent();
            }
        }
        return null;
    }

    public virtual bool IsGroupEngaged()
    {
        foreach(ScoutAgent agent in agents)
        {
            if (agent.IsEngaged())
            {
                return true;
            }
        }
        return false;
    }

    public virtual bool IsOpponentInSight()
    {
        foreach (ScoutAgent agent in agents)
        {
            if (agent.GetOpponentsInSight().Count > 0)
            {
                return true;
            }
        }
        return false;
    }
}
