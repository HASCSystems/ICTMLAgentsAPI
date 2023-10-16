using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Random = UnityEngine.Random;


[RequireComponent(typeof(ScoutAgent))]
public class HeuristicController : MonoBehaviour
{
    static readonly bool IS_DEBUG = false;

    public enum HeuristicControl
    {
        NONE,
        STATIONARY,
        RANDOM,
        USER_INPUT,
        STATE_MACHINE,
        FIXED_PATH
    }
    public HeuristicControl heuristicControl = HeuristicControl.RANDOM;

    protected float dangerousProbability = 0.9f, cautiousProbability = 0.5f;
    public GameController gameController = null;
    protected ScoutAgent agent = null;
    private AgentInput agentInput = null;
    int lastRotationDirection = 0;

    [Header("Fixed Path Heuristic")]
    public int fixedPathIndex = 0;
    public List<WaypointData> fullFixedPath = new List<WaypointData>();
    protected int lastFixedPathIndex = 0, numFixedPathTries = 0, tryCount = 0;


    private void Awake()
    {
        WaypointMeshController.onWaypointsSetup += SetupPaths;

        if (gameController == null)
        {
            gameController = GameObject.FindObjectOfType<GameController>();
        }
        if (agent == null)
        {
            agent = GetComponent<ScoutAgent>();
        }

        if (heuristicControl != HeuristicControl.NONE)
        {
            GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
        }

    }

    public void OnReset()
    {
        fixedPathIndex = 0;
        lastFixedPathIndex = 0;
        numFixedPathTries = 0;
        tryCount = 0;
    }

    private void OnDestroy()
    {
        WaypointMeshController.onWaypointsSetup -= SetupPaths;
    }

    private void SetupPaths()
    {
        if (fullFixedPath.Count <= 0 && 
            (
                (heuristicControl == HeuristicControl.FIXED_PATH) ||
                (heuristicControl == HeuristicControl.STATE_MACHINE)
            )
           )
        {
            fullFixedPath = WaypointMeshController.GetWaypointListFromString(agent.GetComponent<AgentMovement>().mainPath_raw);
        }
    }

    public enum SCENARIO_STATE
    {
        INITIAL,
        RETREAT,
        FORWARD,
        ASSIST,
        MOVE_TO_TARGET,
        OPFOR_IN_SIGHT,
        SEND_ASSIST_SIGNAL
    }

    public virtual void Heuristic(in ActionBuffers actionsOut)
    {
        switch (heuristicControl)
        {
            case HeuristicControl.STATIONARY:
                StationaryHeuristic(actionsOut);
                break;
            case HeuristicControl.RANDOM:
                RandomHeuristic(actionsOut);
                break;
            case HeuristicControl.USER_INPUT:
                InputHeuristic(actionsOut);
                break;
            case HeuristicControl.STATE_MACHINE:
                StateMachineHeuristic(actionsOut);
                break;
            case HeuristicControl.FIXED_PATH:
                FixedPathHeuristic(actionsOut);
                break;
            default:
                break;
        }
    }

    public AgentAction CheckState(AgentGroup group)
    {
        AgentAction groupAction = new AgentAction();

        if (group == null)
        {
            if (IS_DEBUG) Debug.Log("CheckState. Group is NULL");
            groupAction.movementDirection = Directions.NONE;
            groupAction.facingDirection = Directions.NONE;
            return groupAction;
        }

        switch (group.currentScenarioState)
        {
            case SCENARIO_STATE.INITIAL:
                if (group.GetGroupHealthPercent() < 0.25f)
                {
                    group.currentScenarioState = SCENARIO_STATE.RETREAT;
                }
                else
                {
                    group.currentScenarioState = SCENARIO_STATE.FORWARD;
                }
                return CheckState(group);
            case SCENARIO_STATE.RETREAT:
                if (IsInZone(dangerousProbability, group.GetCurrentWaypoint())) // inside dangerous zone
                {
                    group.SetSpeed(false);
                }
                else
                {
                    group.SetSpeed(true);
                }
                group.SetShelterBound(true);
                groupAction.movementDirection = group.MoveToWaypoint(group.safetyWaypoint);
                return groupAction;
            case SCENARIO_STATE.FORWARD:
                if (group.DoOtherGroupsNeedAssistance()
                    && !group.IsGroupEngaged()
                    )
                {
                    group.currentScenarioState = SCENARIO_STATE.ASSIST;
                }
                else
                {
                    group.currentScenarioState = SCENARIO_STATE.MOVE_TO_TARGET;
                }
                return CheckState(group);
            case SCENARIO_STATE.ASSIST:
                if ((group.GetGroupHealthPercent() > 0.75f) &&
                    !IsInZone(cautiousProbability, group.GetCurrentWaypoint()))
                {
                    group.SetSpeed(true);
                }
                else
                {
                    group.SetSpeed(false);
                }
                //group.SetShelterBound(false);
                if (group.GetTarget() != null)
                {
                    groupAction.movementDirection = group.MoveToWaypoint(group.GetTarget().currentWaypoint);
                }
                else
                {
                    groupAction.movementDirection = group.MoveToWaypoint(group.GetOtherGroups(group)[0].GetCurrentWaypoint());
                }
                return groupAction;
            case SCENARIO_STATE.MOVE_TO_TARGET:
                if (group.IsOpponentInSight())
                {
                    group.currentScenarioState = SCENARIO_STATE.OPFOR_IN_SIGHT;
                    return CheckState(group);
                }
                else
                {
                    if (IS_DEBUG) Debug.Log("OpponentNotInSight");
                    group.SetSpeed(true);
                    groupAction.movementDirection = group.MoveToWaypoint(group.goalWaypoint);
                    return groupAction;
                }
            case SCENARIO_STATE.OPFOR_IN_SIGHT:
                if (group.GetGroupHealthPercent() >= 0.75f)
                {
                    group.SetSpeed(false);
                    groupAction.movementDirection = group.MoveToWaypoint(group.goalWaypoint);
                    /*Debug.Log("OPFOR_IN_SIGHT. Current: " + group.agents[0].currentWaypoint.waypoint + "; goal=" + group.goalWaypoint + "; direction=" + groupAction.movementDirection_ecn);*/
                    return groupAction;
                }
                else
                {
                    group.SetAssistanceSignal(true);
                    group.currentScenarioState = SCENARIO_STATE.SEND_ASSIST_SIGNAL;
                    return CheckState(group);
                }
            case SCENARIO_STATE.SEND_ASSIST_SIGNAL:
                if (group.GetGroupHealthPercent() < 0.50f)
                {
                    groupAction.movementDirection = Directions.NONE;
                    groupAction.facingDirection = Directions.NONE;
                    return groupAction;
                }
                else
                {
                    group.SetSpeed(false);
                    groupAction.movementDirection = group.MoveToWaypoint(group.goalWaypoint);
                    return groupAction;
                }
            default:
                if (IS_DEBUG) Debug.LogError("Unknown state:  " + group.currentScenarioState);
                AgentAction stationaryAction = new AgentAction();
                stationaryAction.movementDirection = Directions.NONE;
                stationaryAction.facingDirection = Directions.NONE;
                return stationaryAction;
        }
    }

    public virtual bool IsInZone(float probability, WaypointData waypoint)
    {
        return ProbabilityFromOppforAtPoint(waypoint) < probability;
    }

    public virtual float ProbabilityFromOppforAtPoint(WaypointData measuredWaypoint)
    {
        List<float> probabilities = new List<float>();
        foreach (GameController.PlayerInfo oppforPlayerInfo in gameController.Team1Players)
        {
            ScoutAgent oppAgent = oppforPlayerInfo.Agent;
            if (oppAgent.Health.GetHealth() > 0)
            {
                //WaypointData currwp = agent.currentWaypoint;
                probabilities.Add(WaypointMeshController.GetProbability(agent.gameObject, oppAgent.gameObject));
            }
        }
        float finalProb = 0f;
        foreach (float probability in probabilities)
        {
            finalProb = Mathf.Max(finalProb, probability);
        }
        return finalProb;
    }

    public virtual void StationaryHeuristic(in ActionBuffers actionsOut)
    {
        if (agent.disableInputCollectionInHeuristicCallback)
        {
            return;
        }
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;
        //agent.aimTarget = agent.GetTargetAgent()?.transform;
        //discreteActionsOut[1] = (agent.aimTarget == null) ? 0 : 1;

        if (ScoutAgent.numberOfRotationDirections <= 0)
        {
            var contActionsOut = actionsOut.ContinuousActions;
            contActionsOut[0] = 0f; //rotate
        }
        else
        {
            discreteActionsOut[1] = 0;
        }
    }


    protected virtual void InputHeuristic(in ActionBuffers actionsOut)
    {
        if (agent.disableInputCollectionInHeuristicCallback)
        {
            return;
        }

        if (agentInput == null)
        {
            agentInput = GetComponent<AgentInput>();
        }

        if (agentInput != null)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;

            int moveInput = agentInput.CurrentInput;

            if (IS_DEBUG) Debug.Log(gameObject.name + ": heuristic moveInput=" + moveInput);
            discreteActionsOut[0] = moveInput;
            agent.aimTarget = agent.GetTargetAgent()?.transform;

            if (moveInput != 0)
            {
                if (ScoutAgent.numberOfRotationDirections <= 0)
                {
                    var contActionsOut = actionsOut.ContinuousActions;
                    //contActionsOut[0] = freezeRotation ? 0 : (input.rotateInput) * 3; //rotate
                }
                else
                {
                    // Rotation based on mouse
                    // int currentRotationSteps = Mathf.RoundToInt((transform.localEulerAngles.y / 360f) * (float)ScoutAgent.numberOfRotationDirections);
                    // discreteActionsOut[2] = freezeRotation ? 0 : currentRotationSteps + ((input.rotateInput > 0.25f) ? 1 : ((input.rotateInput < -0.25f) ? -1 : 0));

                    // Rotation based on enemy target
                    if (agent.aimTarget == null)
                    {
                        discreteActionsOut[1] = discreteActionsOut[0] - 1;
                    }
                    else
                    {
                        transform.LookAt(agent.aimTarget);
                        discreteActionsOut[1] = GetDiscreteRotationIndexFromAngle(transform.eulerAngles.y, ScoutAgent.numberOfRotationDirections);
                    }
                    lastRotationDirection = discreteActionsOut[1];
                }
            }
            else
            {
                discreteActionsOut[1] = lastRotationDirection;
            }
        }
    }

    public static int GetDiscreteRotationIndexFromAngle(float angle, int numRotationDirections)
    {
        //Debug.Log("A) DiscreteRotationIndexFromAngle. Angle=" + angle);
        float divAngle = 360f / numRotationDirections;
        angle += divAngle / 2f; // offset it
        while (angle < 0f)
        {
            angle += 360f;
        }
        while (angle > 360f)
        {
            angle -= 360f;
        }

        int index = Mathf.Clamp(Mathf.FloorToInt(angle / divAngle), 0, numRotationDirections - 1);
        //Debug.Log("B) DiscreteRotationIndexFromAngle. Angle=" + angle + "; index=" + index);
        return index;
    }

    protected virtual void RandomHeuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        int moveInput = Random.Range(0, 1 + WaypointMeshController.NumWaypointConnections);

        if (IS_DEBUG) Debug.Log(gameObject.name + ": heuristic moveInput=" + moveInput);
        discreteActionsOut[0] = moveInput;
        //discreteActionsOut[1] = (Random.value > 0.5f ? 1 : 0); //dash
        if (ScoutAgent.numberOfRotationDirections <= 0)
        {
            var contActionsOut = actionsOut.ContinuousActions;
            if (contActionsOut.Length > 0)
                contActionsOut[0] = 0f;// freezeRotation ? 0 : Random.Range(-1f, 1f) * 3; //rotate
        }
        else
        {
            if (discreteActionsOut.Length > 1)
                discreteActionsOut[1] = Random.Range(0, ScoutAgent.numberOfRotationDirections);//freezeRotation ? 0 : Random.Range(0, ScoutAgent.numberOfRotationDirections);
        }

    }


    protected virtual void StateMachineHeuristic(in ActionBuffers actionsOut)
    {
        /*Debug.Log("StateMachineHeuristic. disableHeuristic=" + disableInputCollectionInHeuristicCallback + "; stunned? " + m_IsStunned);*/

        // FSM code
        AgentGroup agentGroup = gameController.GetGroupForAgent(agent);
        if (MLAgentsController.IsDecisionStep && (agent.StepCount != agentGroup.lastActedStep) && gameController.IsInitialized())
        {
            agentGroup.lastActedStep = agent.StepCount;
        }

        var discreteActionsOut = actionsOut.DiscreteActions;
        if (gameController.IsInitialized())
        {
            AgentAction actionData = CheckState(agentGroup);
            //if (IS_DEBUG) Debug.Log(gameObject.name + " FSM move to [4]" + actionData.movementDirection_ecn + "; [8]" + actionData.movementDirection);
            discreteActionsOut[0] = actionData.movementDirection.dir;
            agent.aimTarget = agent.GetTargetAgent()?.transform;
        }
        else
        {
            discreteActionsOut[0] = 0;
        }

        if (ScoutAgent.numberOfRotationDirections <= 0)
        {
            var contActionsOut = actionsOut.ContinuousActions;
            contActionsOut[0] = 0f; //rotate
        }
        else
        {
            if (agent.aimTarget == null)
            {
                discreteActionsOut[1] = discreteActionsOut[0]; // Same as movement direction
            }
            else
            {
                // Get float angle to agent.aimTarget
                transform.LookAt(agent.aimTarget);
                discreteActionsOut[1] = GetDiscreteRotationIndexFromAngle(transform.eulerAngles.y, ScoutAgent.numberOfRotationDirections);
            }
        }
    }

    /// <summary>
    /// Points don't have to be adjacent. It'll use pathfinding to get between points
    /// </summary>
    /// <param name="waypoints"></param>
    protected virtual void FixedPathHeuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        WaypointData wd = null;
        if (fixedPathIndex >= fullFixedPath.Count)
        {
            wd = fullFixedPath[fullFixedPath.Count - 1];
        }
        else
        {
            wd = fullFixedPath[fixedPathIndex];
        }
        if (wd == null) return;

        if (agent.currentWaypoint.waypointID == wd.waypointID)
        {
            if (tryCount >= numFixedPathTries)
            {
                //fixedPathIndex++;
                tryCount = 0;
                discreteActionsOut[0] = (fixedPathIndex < fullFixedPath.Count) ? WaypointMeshController.GetDirectionFromWaypoint(agent, fullFixedPath[fixedPathIndex]).dir : 0;
            }
            else
            {
                tryCount++;
                //discreteActionsOut[0] = 0;
                return;
            }
        }
        else if (fixedPathIndex < fullFixedPath.Count)
        {
            if (IS_DEBUG) Debug.Log(name + ": " + fixedPathIndex + ": Direction to " + fullFixedPath[fixedPathIndex].waypointID + " from "+agent.currentWaypoint.waypointID+": " + WaypointMeshController.GetDirectionFromWaypoint(agent, fullFixedPath[fixedPathIndex]).dir);
            discreteActionsOut[0] = (fixedPathIndex < fullFixedPath.Count) ? WaypointMeshController.GetDirectionFromWaypoint(agent, fullFixedPath[fixedPathIndex]).dir : 0;
        }
        if (IS_DEBUG) Debug.Log("FixedPathHeuristic. Move=" + discreteActionsOut[0]);

        agent.aimTarget = agent.GetTargetAgent()?.transform;

        if (ScoutAgent.numberOfRotationDirections <= 0)
        {
            var contActionsOut = actionsOut.ContinuousActions;
            //contActionsOut[0] = freezeRotation ? 0 : (input.rotateInput) * 3; //rotate
        }
        else
        {
            // Rotation based on enemy target
            if (agent.aimTarget == null)
            {
                discreteActionsOut[1] = discreteActionsOut[0];
            }
            else
            {
                transform.LookAt(agent.aimTarget);
                discreteActionsOut[1] = GetDiscreteRotationIndexFromAngle(transform.eulerAngles.y, ScoutAgent.numberOfRotationDirections);
            }
        }
        fixedPathIndex++;
    }

    public int GetFixedPathDirectionAndIncrement()
    {
        return WaypointMeshController.GetDirectionFromWaypoint(agent, fullFixedPath[fixedPathIndex++]).dir;
    }

}
