using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Random = UnityEngine.Random;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Data structure to know if a peak is occupied
/// </summary>
public class PeakFlagOccupancyStateData
{
    public enum PeakflagOccupancyState
    {
        EMPTY,
        BLUE,
        RED,
        BOTH
    }

    public bool isSelfOnFlagSpot = false;
    public int occupiedFlagSpotID = -1;
    public PeakflagOccupancyState[] occupancyStates;
}

/// <summary>
/// Main controller for the game logic that ties together other modules.
/// </summary>
public class GameController : MonoBehaviour
{
    public bool IS_DEBUG = false;

    public MLAgentsController mlAgentsController;

    public List<string> additiveScenesToLoad = new List<string>();

    //The GameObject of the human player
    //This will be used to determine proper "game over" state
    [Header("HUMAN PLAYER")] public GameObject PlayerGameObject;

    protected SimpleMultiAgentGroup m_Team0AgentGroup;
    protected SimpleMultiAgentGroup m_Team1AgentGroup;

    private int m_numMajorSteps = 0;

    [SerializeField]
    public List<AgentGroup> groups = new List<AgentGroup>();
    public static List<AgentGroup> _groups;

    public List<WaypointData> flagWaypoints = new List<WaypointData>();

    protected int m_NumberOfBluePlayersRemaining = 0, m_NumberOfRedPlayersRemaining = 0;

    protected string episodeResultDescription = "";

    [Serializable]
    public class PlayerInfo
    {
        public ScoutAgent Agent;
        public int HitPointsRemaining
        {
            get
            {
                return (int)Agent.Health.GetHealth();
            }

            set
            {
                Agent.Health.SetHealth(value);
            }
        }
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Collider Col;
        [HideInInspector]
        public int TeamID;

        public int maxStep = 5000;

        public void InitializeAgent()
        {

        }
    }

    private bool m_Initialized;
    public List<PlayerInfo> Team0Players;
    public List<PlayerInfo> Team1Players;

    public GameObject Team0Base;
    public GameObject Team1Base;

    protected int m_ResetTimer;
    private float m_TimeBonus = 1.0f;
    private float m_ReturnOwnFlagBonus = 0.0f;
    private List<bool> m_FlagsAtBase = new List<bool>() { true, true };
    private EnvironmentParameters m_EnvParameters;
    private StatsRecorder m_StatsRecorder;
    private int m_NumFlagDrops = 0;

    public int MaxEnvironmentSteps = 5000;
    public static int NumDecisionSteps = 0;

    private void Awake()
    {
        WaypointMeshController.onWaypointsSetup += Initialize;
        _groups = groups;
        AgentHealth.AgentDied += AgentDied;

        foreach(string sceneName in additiveScenesToLoad)
        {
            Scene additiveScene = SceneManager.GetSceneByName(sceneName);
            if ((additiveScene == null) ||
                (!additiveScene.isLoaded))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        WaypointMeshController.onWaypointsSetup -= Initialize;
        AgentHealth.AgentDied -= AgentDied;
    }

#if UNITY_EDITOR
    [Header("Editor Only")]
    public bool test = false;
    public string test_waypointID = "(106,103)";

    protected virtual void Update()
    {
        if (test)
        {
            Debug.Log("Are blue agents at peaks? " + AreBlueAgentsAtPeaks());
            List<ScoutAgent> agentsAtWP = GetAgentsAtWaypoint(test_waypointID);
            foreach (ScoutAgent sa in agentsAtWP)
            {
                Debug.Log("GetAgentsAtWaypoint: " + sa.name);
            }
            test = false;
        }
    }
#endif

    protected virtual void Initialize()
    {
        if (!m_Initialized)
        {
            m_StatsRecorder = Academy.Instance.StatsRecorder;
            m_EnvParameters = Academy.Instance.EnvironmentParameters;
            m_Team0AgentGroup = new SimpleMultiAgentGroup();
            m_Team1AgentGroup = new SimpleMultiAgentGroup();

            foreach(AgentGroup grp in groups)
            {
                grp.SetupWaypoints();
            }
            _groups = groups;

            //INITIALIZE AGENTS
            foreach (var item in Team0Players)
            {
                item.Agent.Initialize();
                item.Agent.m_BehaviorParameters.TeamId = 0;
                item.TeamID = 0;
                m_Team0AgentGroup.RegisterAgent(item.Agent);
            }
            foreach (var item in Team1Players)
            {
                item.Agent.Initialize();
                item.Agent.m_BehaviorParameters.TeamId = 1;
                item.TeamID = 1;
                m_Team1AgentGroup.RegisterAgent(item.Agent);
            }

            m_Initialized = true;
            ResetScene();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!m_Initialized) return;

        //RESET SCENE IF WE MaxEnvironmentSteps
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            m_Team0AgentGroup.GroupEpisodeInterrupted();
            m_Team1AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    protected virtual void LateUpdate()
    {
        if (mlAgentsController.decisionMode == MLAgentsController.DECISION_MODE.FIXED_INTERVAL)
        {
            CheckIfAllMovesDone();
            //CheckEpisodeEndConditions(); // In case missed by AgentDied() [Seen rarely in 40x mode]
        }
        else // Still need to check if agents are at peaks (Could do on arrival, but this is more robust)
        {
            if (AreBlueAgentsAtPeaks())
            {
                m_Team0AgentGroup.EndGroupEpisode();
                m_Team1AgentGroup.EndGroupEpisode();
                EndGame(1, 0);
            }
        }
    }

    public virtual void AgentDied(ScoutAgent deadAgent)
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
        if ((m_NumberOfBluePlayersRemaining == 0) || 
            (m_NumberOfRedPlayersRemaining == 0)
            )
        {
            //SetEndOfEpisodeRewards(ThrowAgentGroup, HitAgentGroup);
            
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

    protected virtual void UpdateEpisodeResultDescription(int winningTeamID)
    {
        string winningColor = winningTeamID == 0 ? "Red" : "Blue";
        episodeResultDescription = winningColor + " team won. Number of Red Teammates remaining=" + m_NumberOfRedPlayersRemaining + "; Number of Blue Teammates remaining=";
    }

    protected int GetOpposingTeamID(int teamID)
    {
        return teamID == 0 ? 1 : 0;
    }

    /*/// <summary>
    /// ToDo: Port to separate class
    /// </summary>
    private void CheckEpisodeEndConditions()
    {
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

        if (m_NumberOfBluePlayersRemaining < Team1Players.Count) // If one blue dies end episode
        {
            //SetEndOfEpisodeRewards(ThrowAgentGroup, HitAgentGroup);
            m_Team0AgentGroup.EndGroupEpisode();
            m_Team1AgentGroup.EndGroupEpisode();
            EndGame(0);
        }
        else if (m_NumberOfRedPlayersRemaining == 0)
        {
            m_Team0AgentGroup.EndGroupEpisode();
            m_Team1AgentGroup.EndGroupEpisode();
            EndGame(1);
        }
    }*/



    //Has this game ended? Used in Game Mode.
    //Prevents multiple coroutine calls when showing the win screen
    protected bool m_GameEnded = false;
    public virtual void ShowWinScreen(int winningTeam, float delaySeconds)
    {
        if (m_GameEnded) return;
        m_GameEnded = true;
        StartCoroutine(ShowWinScreenThenReset(winningTeam, delaySeconds));
    }

    // End the game, resetting if in training mode and showing a win screen if in game mode.
    public virtual void EndGame(int winningTeam, float delaySeconds = 1.0f)
    {
        ResetScene();
    }

    public virtual IEnumerator ShowWinScreenThenReset(int winningTeam, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        ResetScene();
    }


    protected virtual void GetAllParameters()
    {
        /*//Set time bonus to 1 if Elimination, 0 if CTF
        float defaultTimeBonus = GameMode == GameModeType.CaptureTheFlag ? 0.0f : 1.0f;
        m_TimeBonus = m_EnvParameters.GetWithDefault("time_bonus_scale", defaultTimeBonus);
        m_ReturnOwnFlagBonus = m_EnvParameters.GetWithDefault("return_flag_bonus", 0.0f);
        CTFHitBonus = m_EnvParameters.GetWithDefault("ctf_hit_reward", CTFHitBonus);
        EliminationHitBonus = m_EnvParameters.GetWithDefault("elimination_hit_reward", EliminationHitBonus);
        */
    }

    protected virtual void ResetScene()
    {
        StopAllCoroutines();

        m_GameEnded = false;
        m_NumFlagDrops = 0;
        m_ResetTimer = 0;

        GetAllParameters();

        print($"Resetting {gameObject.name}");
        //Reset the agents
        foreach (var item in Team0Players)
        {
            item.Agent.Health.RestoreHealth();
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            //m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.Health.RestoreHealth();
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            //m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

    }

    /*// Update is called once per frame
    void Update()
    {
        if (!m_Initialized)
        {
            Initialize();
        }
    }*/

    public virtual bool IsInitialized()
    {
        return m_Initialized;
    }

    public virtual AgentGroup GetGroupForAgent(ScoutAgent agent)
    {
        foreach (AgentGroup group in groups)
        {
            if (group.agents.Contains(agent))
                return group;
        }
        return null;
    }

    public virtual bool AreBlueAgentsAtPeaks()
    {
        List<WaypointData> tmp_flagWaypoints = new List<WaypointData>(flagWaypoints);
        int numFlagWaypoints = flagWaypoints.Count;
        int numAliveAgents = 0;
        foreach (PlayerInfo pi in Team1Players)
        {
            if (pi.Agent.Health.GetHealth() > 0)
            {
                numAliveAgents++;
            }
        }

        for (int i = 0; i < numFlagWaypoints; ++i)
        {
            WaypointData flagWaypoint = flagWaypoints[i];

            foreach (PlayerInfo pi in Team1Players)
            {
                if (pi.Agent.Health.GetHealth() > 0)
                {
                    PeakFlagOccupancyStateData occupancyData = GetOccupancyData(pi.Agent);
                    if (!occupancyData.isSelfOnFlagSpot)
                        return false; // One of them is not on the peak and is alive, so automatically know not at peaks
                    else if (occupancyData.occupiedFlagSpotID == i)
                        tmp_flagWaypoints.Remove(flagWaypoint);
                }
            }
        }
        return (numAliveAgents > 0) && (tmp_flagWaypoints.Count == (numFlagWaypoints - numAliveAgents));
    }

    public virtual PeakFlagOccupancyStateData GetOccupancyData(ScoutAgent agent)
    {
        int numFlagWaypoints = flagWaypoints.Count;
        //int teamID = agent.teamID; // 0 is blue, 1 is purple

        // Setup Data
        PeakFlagOccupancyStateData data = new PeakFlagOccupancyStateData();
        data.isSelfOnFlagSpot = false;
        data.occupancyStates = new PeakFlagOccupancyStateData.PeakflagOccupancyState[numFlagWaypoints];
        for (int i = 0; i < numFlagWaypoints; ++i)
        {
            data.occupancyStates[i] = PeakFlagOccupancyStateData.PeakflagOccupancyState.EMPTY;
        }

        // Iterate through flag waypoints
        for (int i = 0; i < numFlagWaypoints; ++i)
        {
            WaypointData flagWaypoint = flagWaypoints[i];
            List<ScoutAgent> waypointAgents = GetAgentsAtWaypoint(flagWaypoint.waypointID, true);
            foreach (ScoutAgent waypointAgent in waypointAgents)
            {
                if (waypointAgent.Health.GetHealth() > 0)
                {
                    if (waypointAgent == agent)
                    {
                        data.isSelfOnFlagSpot = true;
                        data.occupiedFlagSpotID = i;
                    }
                    PeakFlagOccupancyStateData.PeakflagOccupancyState waypointAgentTeam = GetAgentColor(waypointAgent);
                    switch (data.occupancyStates[i])
                    {
                        case PeakFlagOccupancyStateData.PeakflagOccupancyState.EMPTY:
                            data.occupancyStates[i] = waypointAgentTeam;
                            break;
                        case PeakFlagOccupancyStateData.PeakflagOccupancyState.BLUE:
                            if (waypointAgentTeam == PeakFlagOccupancyStateData.PeakflagOccupancyState.RED)
                                data.occupancyStates[i] = PeakFlagOccupancyStateData.PeakflagOccupancyState.BOTH;
                            break;
                        case PeakFlagOccupancyStateData.PeakflagOccupancyState.RED:
                            if (waypointAgentTeam == PeakFlagOccupancyStateData.PeakflagOccupancyState.BLUE)
                                data.occupancyStates[i] = PeakFlagOccupancyStateData.PeakflagOccupancyState.BOTH;
                            break;
                        case PeakFlagOccupancyStateData.PeakflagOccupancyState.BOTH:
                            // Do nothing
                            break;
                    }
                }
            }
        }

        return data;
    }

    public virtual PeakFlagOccupancyStateData.PeakflagOccupancyState GetAgentColor(ScoutAgent agent)
    {
        return agent.teamID == 0 ? PeakFlagOccupancyStateData.PeakflagOccupancyState.BLUE : PeakFlagOccupancyStateData.PeakflagOccupancyState.RED;
    }

    public virtual void CheckIfAllMovesDone()
    {
        if (DoAllAgentsHaveMovementStatus(AgentMovement.WaypointMovementStatus.FINISHED_MOVING))
        {
            if (IS_DEBUG) Debug.Log("Are agents at peak? " + AreBlueAgentsAtPeaks());
            if (AreBlueAgentsAtPeaks())
            {
                m_Team0AgentGroup.EndGroupEpisode();
                m_Team1AgentGroup.EndGroupEpisode();
                EndGame(1, 0);
            }
            else
            {
                SetAllAgentsReadyToMove();
            }
        }
    }

    public virtual bool DoAllAgentsHaveMovementStatus(AgentMovement.WaypointMovementStatus status)
    {
        List<PlayerInfo> allPlayers = new List<PlayerInfo>(Team0Players);
        allPlayers.AddRange(Team1Players);
        foreach (PlayerInfo pi in allPlayers)
        {
            if (pi.Agent.gameObject.activeSelf &&
                pi.Agent.GetComponent<AgentMovement>().currentMovementStatus != status
                )
            {
                return false;
            }
        }
        return true;
    }

    public virtual void SetAllAgentsReadyToMove()
    {
        List<PlayerInfo> allPlayers = new List<PlayerInfo>(Team0Players);
        allPlayers.AddRange(Team1Players);
        foreach (PlayerInfo pi in allPlayers)
        {
            pi.Agent.GetComponent<AgentMovement>().currentMovementStatus =
                AgentMovement.WaypointMovementStatus.WAITING_TO_MOVE;
        }

        // Reset state to initial
        foreach (AgentGroup group in groups)
        {
            group.currentScenarioState = HeuristicController.SCENARIO_STATE.INITIAL;
        }

        m_numMajorSteps++; // Unused
        //if (IS_DEBUG) Debug.Log("<color=red>major step incremented to " + m_numMajorSteps + "</color>");
    }

    public virtual void SetAgentReadyToMove(ScoutAgent agent)
    {
        agent.GetComponent<AgentMovement>().currentMovementStatus =
                AgentMovement.WaypointMovementStatus.WAITING_TO_MOVE;

        // Reset state to initial
        foreach (AgentGroup group in groups)
        {
            foreach (ScoutAgent groupAgent in group.agents)
            {
                if (groupAgent == agent) // Assume that agents are arriving in concert
                    group.currentScenarioState = HeuristicController.SCENARIO_STATE.INITIAL;
            }
        }
    }

    public virtual List<ScoutAgent> GetAgentsAtWaypoint(string waypointID, bool onlyGetNonMoving = false)
    {
        List<ScoutAgent> agentsOnWaypoint = new List<ScoutAgent>();

        List<PlayerInfo> allPlayers = new List<PlayerInfo>(Team0Players);
        allPlayers.AddRange(Team1Players);
        foreach (PlayerInfo pi in allPlayers)
        {
            if (pi.Agent.gameObject.activeSelf &&
                (pi.Agent.currentWaypoint.waypointID == waypointID) &&
                (onlyGetNonMoving ? (pi.Agent.GetComponent<AgentMovement>().currentMovementStatus != AgentMovement.WaypointMovementStatus.MOVING) : true)
                )
            {
                agentsOnWaypoint.Add((ScoutAgent)pi.Agent);
            }
        }
        return agentsOnWaypoint;
    }

}
