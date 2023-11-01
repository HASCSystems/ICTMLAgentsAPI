using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class AgentTarget
{
    public ScoutAgent agent;
    public int[] targetPoints;

    public static int[] GetAllHits()
    {
        List<int> hitList = new List<int>();
        for (int i=0; i < Mathf.RoundToInt(VisibilityController.numVisibilityChecks); ++i)
        {
            hitList.Add(1);
        }
        return hitList.ToArray();
    }
}

public class AgentAction
{
    public Direction movementDirection, facingDirection;
}

/// <summary>
/// Scout Agent script that controls and ties together components on ScoutAgent such as health, movement, heuristic control, etc.
/// </summary>
public class ScoutAgent : Agent
{
    public bool IS_DEBUG = true;
    public static readonly int numberOfRotationDirections = 8;
    public int initialStepPause = 0;

    public string initialWaypointID;
    public WaypointData currentWaypoint, lastWaypoint, nextWaypoint;
    protected bool m_isFastMovement = false;
    [SerializeField]
    protected bool m_isStanding = true;

    [Header("TEAM")]

    public int teamID;
    private AgentMovement m_AgentMovement;
    public int lastMoveAction = 0;
    public RangedAttack rangedAttack;
    public HeuristicController heuristicController;

    [Header("HEALTH")] public AgentHealth Health;

    [Header("INPUT")]
    public AgentInput input;

    public bool UseVectorObs;
    public GameController m_GameController;

    private Vector3 m_StartingPos;
    private Quaternion m_StartingRot;

    [Header("Attacking")]
    [Tooltip("spawnPos object that spawns projectiles/bullets")]
    public Transform projectileSpawnerForAiming;
    [Tooltip("Object to aim at, usually an opponent")]
    public Transform aimTarget;
    private ScoutAgent target;

    [Header("OTHER")] public bool m_PlayerInitialized;
    [HideInInspector]
    public BehaviorParameters m_BehaviorParameters;

    public float m_InputH;
    private Vector3 m_HomeBasePosition;
    private Vector3 m_HomeDirection;
    private float m_InputV;
    private float m_Rotate;
    public float m_ShootInput;
    private bool m_FirstInitialize = true, m_IsInitialized = false;
    private float m_LocationNormalizationFactor = 80.0f; // About the size of a reasonable stage
    private EnvironmentParameters m_EnvParameters;
    private RewardsCalculator rewardsCalculator;

    public BufferSensorComponent m_OtherAgentsBuffer;

    //is the current step a decision step for the agent
    private bool m_IsDecisionStep;

    [HideInInspector]
    //because heuristic only runs every 5 fixed update steps, the input for a human feels really bad
    //set this to true on an agent that you want to be human playable and it will collect input every
    //FixedUpdate tick instead of every decision step
    public bool disableInputCollectionInHeuristicCallback;

#if UNITY_EDITOR
    [Header("Testing")]
    public bool test = false;

    protected virtual void Update()
    {
        if (test)
        {
            //GetOpponentsInSight();
            RequestDecision();
            test = false;
        }
    }

#endif


    protected virtual void Awake()
    {
        rewardsCalculator = GetComponent<RewardsCalculator>();

        MLAgentsController.OnBroadcastDecision += OnDecisionIntent;
        AgentHealth.AgentHit += OnAgentHit;
        AgentHealth.AgentDied += OnAgentDied;
    }

    protected virtual void OnDestroy()
    {
        MLAgentsController.OnBroadcastDecision -= OnDecisionIntent;
        AgentHealth.AgentHit -= OnAgentHit;
        AgentHealth.AgentDied += OnAgentDied;
    }

    protected virtual void OnAgentHit(ScoutAgent attacker, ScoutAgent attackee, float damageInflicted)
    {
        if (this == attacker)
        {
            AddReward(rewardsCalculator.perHPLossInflictedReward * damageInflicted);
        }
        if (this == attackee)
        {
            AddReward(rewardsCalculator.perHPLostReward * damageInflicted);
        }
    }

    protected virtual void OnAgentDied(ScoutAgent deadAgent)
    {
        if (this == deadAgent)
        {
            AddReward(rewardsCalculator.onDiedReward);
        }
    }

    public override void Initialize()
    {
        Debug.Log("Agent Initialize()");
        var bufferSensors = GetComponentsInChildren<BufferSensorComponent>();
        if (bufferSensors != null && bufferSensors.Length > 0)
            m_OtherAgentsBuffer = bufferSensors[0];

        m_AgentMovement = GetComponent<AgentMovement>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

#if !UNITY_EDITOR
        // If built, red agents MUST be set to Default or it will cause an error
        if (teamID == 0)
        {
            m_BehaviorParameters.BehaviorType = BehaviorType.Default;
        }
#endif

        input = GetComponent<AgentInput>();

        //if (!m_GameController.IsInitialized()) return;

        if (m_FirstInitialize)
        {
            m_StartingPos = transform.position;
            m_StartingRot = transform.rotation;
            m_FirstInitialize = false;
        }
        m_EnvParameters = Academy.Instance.EnvironmentParameters;

        m_AgentMovement.ResetAgent();

        GetAllParameters();

        m_IsInitialized = true;
    }

    //Get all environment parameters for agent
    private void GetAllParameters()
    {
        //m_StunTime = m_EnvParameters.GetWithDefault("stun_time", 10.0f);
        //m_OpponentHasFlagPenalty = m_EnvParameters.GetWithDefault("opponent_has_flag_penalty", 0f);
        //m_TeamHasFlagBonus = m_EnvParameters.GetWithDefault("team_has_flag_bonus", 0f);
        //m_BallHoldBonus = m_EnvParameters.GetWithDefault("ball_hold_bonus", 0f);
    }

    public void ResetAgent()
    {
        GetAllParameters();
        StopAllCoroutines();
        transform.position = m_StartingPos;
        if (GetComponent<AgentMovement>() != null)
        {
            GetComponent<AgentMovement>().ResetAgent();
        }
        if (rangedAttack != null)
        {
            rangedAttack.OnReset();
        }
        heuristicController.OnReset();

        if (m_GameController.mlAgentsController.decisionMode == MLAgentsController.DECISION_MODE.UPON_ARRIVAL)
        {
            RequestDecision();
        }

        rewardsCalculator.ClearAllBuffers();
    }


    protected int m_AgentStepCount; //current agent step
    protected virtual void FixedUpdate()
    {
        // TODO: REWRITE for more nuanced step 
        if (StepCount % 5 == 0)
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;
        }

        if (StepCount % 2 == 0) // Since about 50 steps between waypoints and we want 25 chances to attack, set it to 2
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;

            if ((rangedAttack != null) &&
                rangedAttack.enabled)
            {
                List<AgentTarget> visibleOpponents = GetOpponentsInSight();
                if (visibleOpponents.Count > 0)
                {
                    List<AgentTarget> viableVisibleOpponents = new List<AgentTarget>();
                    foreach (AgentTarget opp in visibleOpponents)
                    {
                        if ((currentWaypoint != null) &&
                            (opp.agent.currentWaypoint != null) &&
                            (opp.agent.Health.GetHealth() > 0)
                            )
                        {
                            viableVisibleOpponents.Add(opp);
                        }
                    }

                    if (viableVisibleOpponents.Count > 0)
                    {
                        AgentTarget targetOpp = viableVisibleOpponents[UnityEngine.Random.Range(0, visibleOpponents.Count)]; // Get random agent

                        rangedAttack.Attack(targetOpp, currentWaypoint.waypointID, targetOpp.agent.currentWaypoint.waypointID);
                    }
                }
            }
        }
    }

    public virtual bool IsReadyForDecision()
    {
        return m_AgentMovement.currentMovementStatus != AgentMovement.WaypointMovementStatus.MOVING;
    }

    public virtual void OnDecisionIntent()
    {
        if (IS_DEBUG) Debug.Log("OnDecisionIntent for " + name + ". Isready? " + IsReadyForDecision() + "; @ " + Time.realtimeSinceStartup);
        if (IsReadyForDecision())
        {
            RequestDecision();
        }
    }

    public virtual void OnWaitingToMove()
    {
        //RequestDecision();
    }

    public virtual void OnFinishedMoving()
    {
        if (m_GameController.mlAgentsController.decisionMode == MLAgentsController.DECISION_MODE.UPON_ARRIVAL)
        {
            m_GameController.SetAgentReadyToMove(this);
            RequestDecision();
        }
    }

    //Collect observations, to be used by the agent in ML-Agents.
    public override void CollectObservations(VectorSensor sensor)
    {
        if (UseVectorObs)
        {
            sensor.AddObservation(Health.GetHealthPercent()); //Remaining Hit Points Normalized

            sensor.AddObservation(transform.InverseTransformDirection(m_HomeDirection));
            // Location to base
        }

        List<GameController.PlayerInfo> teamList;
        List<GameController.PlayerInfo> opponentsList;
        if (m_BehaviorParameters.TeamId == 0)
        {
            teamList = m_GameController.Team0Players;
            opponentsList = m_GameController.Team1Players;
        }
        else
        {
            teamList = m_GameController.Team1Players;
            opponentsList = m_GameController.Team0Players;
        }

        foreach (var info in teamList)
        {
            if (info.Agent != this && info.Agent.gameObject.activeInHierarchy)
            {
                m_OtherAgentsBuffer.AppendObservation(GetOtherAgentData(info));
            }
        }
        //Only opponents who picked up the flag are visible
        //var currentFlagPosition = TeamFlag.transform.position;
        int numEnemiesRemaining = 0;
        foreach (var info in opponentsList)
        {
            if (info.Agent.gameObject.activeInHierarchy)
            {
                numEnemiesRemaining++;
            }
        }
        var portionOfEnemiesRemaining = (float)numEnemiesRemaining / (float)opponentsList.Count;

        //Location to flag
        //sensor.AddObservation(GetRelativeCoordinates(currentFlagPosition));
    }

    //Get normalized position relative to agent's current position.
    private float[] GetRelativeCoordinates(Vector3 pos)
    {
        Vector3 relativeHome = transform.InverseTransformPoint(pos);
        var relativeCoordinate = new float[2];
        relativeCoordinate[0] = (relativeHome.x) / m_LocationNormalizationFactor;
        relativeCoordinate[1] = (relativeHome.z) / m_LocationNormalizationFactor;
        return relativeCoordinate;
    }

    //Get information of teammate
    private float[] GetOtherAgentData(GameController.PlayerInfo info)
    {
        var otherAgentdata = new float[6];
        otherAgentdata[0] = info.Agent.Health.GetHealthPercent();
        var relativePosition = transform.InverseTransformPoint(info.Agent.transform.position);
        otherAgentdata[1] = relativePosition.x / m_LocationNormalizationFactor;
        otherAgentdata[2] = relativePosition.z / m_LocationNormalizationFactor;
        otherAgentdata[3] = info.TeamID == teamID ? 0.0f : 1.0f;
        otherAgentdata[4] = 0.0f; // Has Enemy Flag
        otherAgentdata[5] = 0.0f; // Stunned
        return otherAgentdata;

    }

    //Execute agent movement
    public virtual void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        aimTarget = GetTargetAgent()?.transform;
        //m_ThrowInput = aimTarget == null ? 0 : 1;

        if (numberOfRotationDirections <= 0)
        {
            m_Rotate = continuousActions[0];
            //m_CubeMovement.Look(m_Rotate);
        }
        else
        {
            m_Rotate = 0f;
            Vector3 newAngle = new Vector3(
                0f,
                Mathf.Lerp(0f, 360f, (float)(discreteActions[0] - 1) / (float)numberOfRotationDirections),
                0f);
            transform.localEulerAngles = newAngle;
            //Debug.Log("New Angle: " + discreteActions[0] + " -> " + newAngle + "; " + transform.localEulerAngles);
        }
        //m_DashInput = 0; // (int)discreteActions[2]; // 2

        //HANDLE ROTATION


        //HANDLE XZ MOVEMENT
        if (IS_DEBUG) Debug.Log("Current movement status for " + name + " = " + m_AgentMovement.currentMovementStatus + "; moveDir=" + discreteActions[0] + " @ " + Time.realtimeSinceStartup);
        if (m_AgentMovement.currentMovementStatus == AgentMovement.WaypointMovementStatus.WAITING_TO_MOVE)
        {
            lastMoveAction = discreteActions[0];
            m_AgentMovement.MoveToNextWaypoint(discreteActions[0]);
        }

        //perform discrete actions only once between decisions
        if (m_IsDecisionStep)
        {
            m_IsDecisionStep = false;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (IS_DEBUG) Debug.Log("OnActionReceived for " + name + " @ " + Time.realtimeSinceStartup);
        MoveAgent(actionBuffers);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (IS_DEBUG) Debug.Log("OnCollisionEnter: " + col.gameObject.name);
    }


    //Used for human input
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (heuristicController == null) return;

        // Stay put if there is an initial step pause
        if (GameController.NumDecisionSteps < initialStepPause)
        {
            heuristicController.StationaryHeuristic(actionsOut);
            return;
        }

        heuristicController.Heuristic(actionsOut);
    }

    public virtual void ChangeMovementSpeed(bool isFast)
    {
        m_isFastMovement = isFast;
    }

    public virtual bool IsMovingFast()
    {
        return m_isFastMovement;
    }

    public virtual bool IsStanding()
    {
        return m_isStanding;
    }

    // TODO: Take into account FOV of 180, might be trivial
    public virtual List<AgentTarget> GetOpponentsInSight()
    {
        List<AgentTarget> opponentsInSight = new List<AgentTarget>();

        int opponentTeamID = (teamID == 0) ? 1 : 0;
        List<GameController.PlayerInfo> opponentTeam = (opponentTeamID == 0) ? 
            m_GameController.Team0Players : 
            m_GameController.Team1Players;

        int agentIndex = 0;

        if (IS_DEBUG) Debug.Log("GetOpponentsInSight: inited? " + m_IsInitialized + "; opponentTeam null? " + (opponentTeam == null));
        if (m_IsInitialized && (opponentTeam != null))
        {
            foreach (GameController.PlayerInfo playerInfo in opponentTeam)
            {
                if (IS_DEBUG) Debug.Log(agentIndex + ") Opponent agent = " + playerInfo.Agent.name + "; HP=" + playerInfo.Agent.Health.GetHealth());
                if (playerInfo.Agent.Health.GetHealth() > 0)
                {
                    if ((currentWaypoint != null) &&
                        (playerInfo.Agent.currentWaypoint != null) &&
                        (playerInfo.Agent.currentWaypoint.waypointID == currentWaypoint.waypointID)
                        )
                    {
                        if (IS_DEBUG) Debug.Log("Same waypoint!");

                        AgentTarget tgt = new AgentTarget();
                        tgt.agent = (ScoutAgent)playerInfo.Agent;
                        tgt.targetPoints = AgentTarget.GetAllHits();
                        opponentsInSight.Add(tgt);
                        continue;
                    }

                    int[] visPoints = VisibilityController.GetArrayOfBodyPointsVisible(
                        transform, playerInfo.Agent.transform,
                        IsStanding(), playerInfo.Agent.IsStanding(),
                        IS_DEBUG
                        );

                    float visPct = VisibilityController.GetPercentageOfBodyPointsVisible(visPoints);

                    if (visPct > 0f)
                    {
                        AgentTarget tgt = new AgentTarget();
                        tgt.agent = (ScoutAgent)playerInfo.Agent;
                        tgt.targetPoints = visPoints;
                        opponentsInSight.Add(tgt);
                        continue;
                    }
                    else
                    {
                        if (IS_DEBUG) Debug.Log(agentIndex + "A) No Hit");
                    }
                }
                agentIndex++;
            }
        }

        if (IS_DEBUG)
        {
            string oppsInSight = name + ": Opponents in Sight: ";
            foreach(AgentTarget atgt in opponentsInSight)
            {
                oppsInSight += atgt.agent.name + ",";
            }
            Debug.Log(oppsInSight);
        }

        return opponentsInSight;
    }

    public ScoutAgent GetTargetAgent()
    {
        target = null;
        List<AgentTarget> inSightOpponents = GetOpponentsInSight();

        Dictionary<ScoutAgent, float> rangeDic = new Dictionary<ScoutAgent, float>();
        float minD = float.MaxValue;
        foreach (AgentTarget inSightOpponent in inSightOpponents)
        {
            if (inSightOpponent.agent.Health.GetHealth() <= 0)
                continue;

            float d = Vector3.Distance(transform.position, inSightOpponent.agent.transform.position);
            if (d < GetAttackRange())
            {
                rangeDic.Add(inSightOpponent.agent, d);
                if (d < minD)
                {
                    target = inSightOpponent.agent;
                    minD = d;
                }
            }
        }

        return target;
    }

    protected virtual float GetAttackRange()
    {
        Debug.Log("<color=red>Need to do full calculation</color>");
        return 100f;
    }

    public virtual bool IsEngaged()
    {
        return GetTargetAgent() != null;
    }
}