using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ScoutAgent))]
public class AgentMovement : MonoBehaviour
{
    public bool IS_DEBUG = false;

    public bool isMoving = false;
    private int moveCounter = 0;

    protected ScoutAgent agent;

    public enum WaypointMovementModality
    {
        LERP,
        TELEPORT
    }
    public WaypointMovementModality waypointMovementModality = WaypointMovementModality.LERP;

    private WaypointData initialWaypointData = null;
    public WaypointMeshController waypointMeshController;
    public float movementSpeed = 10f, movementTolerance = 0.1f, movementForce = 1f;
    public int numWaypointsPerStep = 3, numWaypointsMovedThisStep = 0;
    public Vector3 hop = new Vector3(0f, 3f, 0f);
    public float agentHeight = 1f;
    private float distanceToDestination = 0f;
    [SerializeField]
    protected float distanceToNextWaypoint = 0f;

    private HeuristicController heuristicController;

    [TextArea(5,10)]
    public string mainPath_raw = string.Empty;
    private List<WaypointData> mainPathWaypoints = new List<WaypointData>();

    public enum WaypointMovementStatus
    {
        WAITING_TO_MOVE,
        MOVING,
        FINISHED_MOVING
    }
    public WaypointMovementStatus currentMovementStatus = WaypointMovementStatus.WAITING_TO_MOVE;

    private void Awake()
    {
        WaypointMeshController.onWaypointsSetup += SetupMainPath;
        agent = GetComponent<ScoutAgent>();
        heuristicController = GetComponent<HeuristicController>();
    }

    private void OnDestroy()
    {
        WaypointMeshController.onWaypointsSetup -= SetupMainPath;
    }

    private void SetupMainPath()
    {
        mainPathWaypoints = WaypointMeshController.GetWaypointListFromString(mainPath_raw);

        WaypointData lastWaypoint = mainPathWaypoints[mainPathWaypoints.Count - 1];
        if (!WaypointMeshController.establishedRoutes.ContainsKey(lastWaypoint))
        {
            WaypointMeshController.establishedRoutes.Add(lastWaypoint, new List<RouteObject>() {
                        new RouteObject(mainPathWaypoints)
                        });
        }
        else
        {
            WaypointMeshController.establishedRoutes[lastWaypoint].Add(new RouteObject(mainPathWaypoints));
        }


    }

    private void Update()
    {
        /*
        if (WaypointMeshController.isReady)
        {
            WaypointMeshController.GetWaypointListFromString(mainPath_raw);
            waypointListIsSetup = true;
        }*/
    }

    public void ResetAgent()
    {
        if (agent == null) agent = GetComponent<ScoutAgent>();

        Debug.Log("A) Reset Agent: " + gameObject.name + "; agent null? " + (agent == null) + "; wmc? " + (waypointMeshController == null));
        if (initialWaypointData == null)
        {
            initialWaypointData = WaypointMeshController.GetWaypointData(agent.initialWaypointID);
        }
        agent.currentWaypoint = initialWaypointData;
        agent.nextWaypoint = initialWaypointData;
        if (initialWaypointData != null)
            TeleportToWaypoint(initialWaypointData);
        else
            Debug.Log("initialWaypointData null for " + gameObject.name);
        Debug.Log("B) Reset Agent. " + gameObject.name);
    }

    protected virtual void FixedUpdate()
    {
        // New movement state machine
        switch (currentMovementStatus)
        {
            default:
            case WaypointMovementStatus.WAITING_TO_MOVE:
                // Do nothing here - Set waypoint to move toward -- See MoveAgent()                
                agent.OnWaitingToMove();
                break;
            case WaypointMovementStatus.MOVING:

                if (waypointMovementModality == WaypointMovementModality.LERP)
                {
                    // Move incrementally

                    Vector3 currPos = agent.transform.position;
                    Vector3 nextDir = (agent.nextWaypoint.location - agent.transform.position).normalized;
                    Vector3 newPos = currPos + nextDir * movementSpeed * Time.fixedDeltaTime;
                    Vector3 incPos = (newPos - currPos);
                    if (IS_DEBUG) Debug.Log("incPos = " + incPos + "; timescale=" + Time.timeScale);
                    //Vector3 hop = new Vector3(0f, 2.5f, 0f);
                    incPos += hop;
                    transform.position += incPos;
                    transform.position -= hop;
                    transform.position = new Vector3(
                        transform.position.x,
                        agent.nextWaypoint.location.y + agentHeight,
                        transform.position.z
                        );

                    // If close enough, mark as finished moving.
                    distanceToDestination = Vector2.Distance(
                        new Vector2(agent.transform.position.x, agent.transform.position.z),
                        new Vector2(agent.nextWaypoint.location.x, agent.nextWaypoint.location.z)
                        );

                    if (distanceToDestination <= movementTolerance)
                    {
                        CompleteWaypointArrival();
                    }
                }
                else if (waypointMovementModality == WaypointMovementModality.TELEPORT)
                {
                    CompleteWaypointArrival();
                }
                break;
            case WaypointMovementStatus.FINISHED_MOVING:
                // Stay in this mode until all other agents are done moving
                // Once all done, switch back to WAITING_TO_MOVE -or- have them all switch to this externally
                numWaypointsMovedThisStep = 0;
                agent.OnFinishedMoving();
                break;
        }
    }

    protected virtual void CompleteWaypointArrival()
    {
        transform.position = agent.nextWaypoint.location + new Vector3(0, agentHeight, 0);

        agent.lastWaypoint = agent.currentWaypoint;
        /*if (agent.IsMovingFast())
        {
            agent.currentWaypoint = agent.nextWaypoint;
        }*/
        agent.currentWaypoint = agent.nextWaypoint;

        distanceToDestination = -1f;

        numWaypointsMovedThisStep++;

        bool isAtFlag = false;
        for (int i = 0; i < agent.m_GameController.flagWaypoints.Count; ++i)
        {
            if (agent.currentWaypoint.waypointID == agent.m_GameController.flagWaypoints[i].waypointID)
            {
                isAtFlag = true;
                break;
            }
        }


        // Done
        if ((numWaypointsMovedThisStep >= numWaypointsPerStep) ||
            isAtFlag
            )
        {
            isMoving = false;
            currentMovementStatus = WaypointMovementStatus.FINISHED_MOVING;
        }
        else // Keep going
        {
            if ((heuristicController != null) &&
                heuristicController.heuristicControl == HeuristicController.HeuristicControl.FIXED_PATH)
            {
                int dir = heuristicController.GetFixedPathDirectionAndIncrement();
                if (IS_DEBUG) Debug.Log("dir = " + dir + "; timescale=" + Time.timeScale);
                MoveToNextWaypoint(dir);
            }
            else if (mainPathWaypoints.Contains(agent.currentWaypoint))
            {
                int i = mainPathWaypoints.IndexOf(agent.currentWaypoint);
                if (i < mainPathWaypoints.Count - 1)
                {
                    WaypointData nextWaypoint = mainPathWaypoints[i + 1];

                    int dir = agent.currentWaypoint.GetDirectionOfNeighbor(nextWaypoint);

                    MoveToNextWaypoint(dir);
                    if (IS_DEBUG) Debug.Log("dir = " + dir + "; timescale=" + Time.timeScale);

                }
                else
                {
                    if (IS_DEBUG) Debug.Log("dir = 0; zero move");
                    MoveToNextWaypoint(0);
                }
            }
            else
            {
                if (IS_DEBUG) Debug.Log("dir = " + agent.lastMoveAction + "; lastMoveAction move");
                MoveToNextWaypoint(agent.lastMoveAction);
            }
        }

    }

    public virtual void MoveToNextWaypoint(WaypointData waypoint)
    {
        MoveToNextWaypoint(WaypointMeshController.GetDirectionFromWaypoint(agent, waypoint).dir);
    }

    // Occurs after agents have all finished moving 
    public virtual void MoveToNextWaypoint(int index)
    {
        //Debug.Log("MoveToNextWaypoint: " + index + "; is current null? " + (agent.currentWaypoint == null));
        if (agent.currentWaypoint == null) return;

        currentMovementStatus = WaypointMovementStatus.MOVING;

        // Check movement direction
        // Movement is NONE
        if (index == 0)
        {
            // do NOT update waypoint or index!
            transform.position = agent.currentWaypoint.location + new Vector3(0f,agentHeight,0f);
            isMoving = false;
            currentMovementStatus = WaypointMovementStatus.FINISHED_MOVING;
            moveCounter++;
            agent.nextWaypoint = agent.currentWaypoint;
            return;
        }
        else
        {
            WaypointData candidateNext = WaypointMeshController.GetWaypointData(agent.currentWaypoint.neighborIDs[index]);
            if (candidateNext == null)
            {
                int altIndex = FindAlternateMovementIndex(agent.currentWaypoint, index);
                //Debug.Log("Find alt index: " + index + " -> " + altIndex + " for " + agent.currentWaypoint.waypointID);
                if (altIndex < 0) return;
                candidateNext = WaypointMeshController.GetWaypointData(agent.currentWaypoint.neighborIDs[altIndex]);
            }
            agent.nextWaypoint = candidateNext;
            if (IS_DEBUG) Debug.Log("Current Waypoint=" + agent.currentWaypoint.waypointID + "; candidateNext=" + candidateNext.waypointID + "; index=" + index);
            isMoving = true;
            return;
        }
    }

    public virtual void TeleportToWaypoint(WaypointData destination)
    {
        transform.position = destination.location + new Vector3(0f, agentHeight, 0f);
        agent.currentWaypoint = destination;
    }

    /// <summary>
    /// Find an alternate waypoint if no neighbor available in a direction
    /// Can be altered to be stochastic, but is currently deterministic.
    /// </summary>
    /// <param name="wd"></param>
    /// <param name="idx">is number 1-8 inclusive</param>
    /// <returns>index from 1-8</returns>
    private int FindAlternateMovementIndex(WaypointData wd, int idx)
    {
        if (wd.HasNeighborInDirection(idx)) return idx;

        int nn = WaypointMeshController.NumWaypointConnections;
        for (int d = 1; d <= nn / 2; ++d)
        {
            int dp = (idx-1 + d) % nn;
            if (wd.HasNeighborInDirection(dp+1))
            {
                return dp+1;
            }
            int dm = (idx-1 - d + nn) % nn;
            if (wd.HasNeighborInDirection(dm+1))
            {
                return dm+1;
            }
        }
        return -1; // fail state
    }

}
