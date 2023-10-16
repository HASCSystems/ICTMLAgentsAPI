using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RouteObject
{
    public Dijkstra dijkstra;
    public List<WaypointData> shortestPath;

    public RouteObject(Dijkstra dijkstra, List<WaypointData> shortestPath)
    {
        this.dijkstra = dijkstra;
        this.shortestPath = shortestPath;
    }

    public RouteObject(List<WaypointData> shortestPath)
    {
        this.dijkstra = null;
        this.shortestPath = shortestPath;
    }

    public List<WaypointData> GetPartialFromShortestPath(WaypointData partialStart)
    {
        int partialStartIndex = shortestPath.IndexOf(partialStart);
        if (partialStartIndex < 0) return null;
        List<WaypointData> partialList = new List<WaypointData>();
        for (int i = partialStartIndex; i < shortestPath.Count; ++i)
        {
            partialList.Add(shortestPath[i]);
        }
        return partialList;
    }

    public bool ContainsWaypoint(WaypointData waypoint)
    {
        return shortestPath.Contains(waypoint);
    }
}

/// <summary>
/// Main runtime waypoint data controller. 
/// Note: When making a new WaypointMeshData, copy the data from waypointDataList after filling it using one of the 
/// test scripts below. Then paste it into the new WaypointMeshData.
/// </summary>
public class WaypointMeshController : MonoBehaviour
{
    public static readonly int NumWaypointConnections = 8;

    public WaypointMeshData waypointMeshData;
    private static WaypointMeshData _waypointMeshData;

    public GameObject waypointPrefab;

    [Header("Waypoint Setup")]
    public TextAsset waypointData;
    public Transform waypointParent;
    [SerializeField]
    public List<WaypointData> waypointDataList = new List<WaypointData>();

    public delegate void WaypointsSetupDelegate();
    public static event WaypointsSetupDelegate onWaypointsSetup = null;

    public static Dictionary<WaypointData, List<RouteObject>> establishedRoutes = new Dictionary<WaypointData, List<RouteObject>>();

    // Prone
    protected bool isGoingProne = false;


    #region SETUP_MESH
    public bool doSetup = false;
    public bool doSetupDictionary = false;

    public TextAsset misalignedNodeTextAsset, listedNodeTextAsset;

    private static int count = 0, countLimit = 1000;
    private static List<WaypointData> inputList = new List<WaypointData>();

    public bool drawDebugLines = false;

    [Header("Click here to create prefabs for every point in the WaypointMeshData")]
    [Header("as children of this GameObject. Once done, drag this object into your")]
    [Header("Project to form a prefab, rename it, and remove any components. Now you")]
    [Header("have a prefab with visualizations of all the waypoints. This is very big,")]
    [Header("so it is not committed. Also, remove it before building.")]
    public bool visualizeWaypoints = false;

    [Header("---Functionality Testing---")]
    [Header("Functionality Testing Parameters")]
    public GameObject testAgentA;
    public GameObject testAgentB;
    public Transform testWaypointA, testWaypointB;
    public Transform clusterCentersParent;
    public float testRadius = 10f;
    public string testSaveLoc = "2023-09-25_TestClusters0";
    public bool testIsStandingA = false, testIsStandingB = false;
    [Tooltip("Set this to -1 to do full cluster check. Otherwise set it to 2 or higher")]
    public int testNumClusters = -1;
    [Tooltip("Small list of clusters to compare against each other for hit checking")]
    public List<Transform> clusterSubList = new List<Transform>();
    public TextAsset clusterPairList;

    [Header("Functionality Testing Functions")]
    public bool runPointToPointVisualizationTest = false;
    public bool runClusterListCheck = false;
    public bool runFileClusterListCheck = false;
    [Header("NOTE: This will take a LONG time!")]
    [Tooltip("This will take a long time. Best to run it overnight!")]
    public bool doFullClusterCheck = false;

    [Header("---Developer Testing [DO NOT USE]---")]
    public bool test = false;
    public bool test2 = false;
    public string testID = "(0,0)";
    public Transform testTransformA, testTransformB;
    //public string test_WPA = "(32,31)", test_WPB = "(32,54)";

    private void Awake()
    {
        _waypointMeshData = waypointMeshData;

        //StanceController.HasStartedGoingProne += OnStartedProne;
        //StanceController.HasFinishedGoingProne += OnFinishedProne;
    }

    private void Update()
    {
        if (visualizeWaypoints)
        {
            VisualizeWaypoints();
            visualizeWaypoints = false;
        }

        if (doSetup)
        {
            SetupWaypointsFromFile_DataDriven(waypointData);
            doSetup = false;
        }

        if (doSetupDictionary)
        {
            waypointMeshData.SetupDictionary();
            doSetupDictionary = false;
        }

#if UNITY_EDITOR
        if (runPointToPointVisualizationTest)
        {
            StartCoroutine(_PointToPointTest());
            IEnumerator _PointToPointTest()
            {
                // Create WaypointData from waypoint IDs
                WaypointData wpA = _waypointMeshData.waypointLookupTable[testWaypointA.name];
                WaypointData wpB = _waypointMeshData.waypointLookupTable[testWaypointB.name];

                // Move agents to waypoints instantly
                testAgentA.GetComponent<AgentMovement>().TeleportToWaypoint(wpA);
                testAgentB.GetComponent<AgentMovement>().TeleportToWaypoint(wpB);

                // Get waypointVisibilityControllers for each agent so we can test visibility with raycasts
                WaypointVisibilityController wvcA = testAgentA.GetComponent<WaypointVisibilityController>();
                WaypointVisibilityController wvcB = testAgentB.GetComponent<WaypointVisibilityController>();

                //yield return new WaitForFixedUpdate();

                // Check all four possibilities: stand/prone vs. stand/prone
                if (testIsStandingA && testIsStandingB) // stand / stand
                {
                    wvcA.SetStanding();
                    wvcB.SetStanding();
                    yield return new WaitForFixedUpdate();
                }
                else if (testIsStandingA && !testIsStandingB) // stand / prone
                {
                    wvcA.SetStanding();
                    wvcB.SetStanding(); // Reset position
                    yield return new WaitForFixedUpdate(); // wait
                    yield return wvcB.SetProneAsync(testAgentA.transform); // Asynchronously have agent fall into prone position
                }
                else if (!testIsStandingA && testIsStandingB) // prone / stand
                {
                    wvcB.SetStanding();
                    wvcA.SetStanding();
                    yield return new WaitForFixedUpdate();
                    yield return wvcA.SetProneAsync(testAgentB.transform);
                }
                else // prone / prone
                {
                    wvcA.SetStanding();
                    wvcB.SetStanding();
                    yield return new WaitForFixedUpdate();
                    yield return wvcA.SetProneAsync(testAgentB.transform);
                    yield return wvcB.SetProneAsync(testAgentA.transform);
                }
                yield return new WaitForFixedUpdate();

                // Check Head->{Head,Torso,Legs} and Torso->{Head,Torso,Legs}
                int[] vis = VisibilityController.GetArrayOfBodyPointsVisible(
                    testAgentA.transform, testAgentB.transform, testIsStandingA, testIsStandingB, true
                    );
                string line = testWaypointA.name + "\t" + testWaypointB.name + "\t" + string.Join("\t", vis) + "\n";
                Debug.Log(line); // line data
            }
            runPointToPointVisualizationTest = false;
        }

        if (runClusterListCheck)
        {
            string dir = string.Empty;
            if (!string.IsNullOrEmpty(testSaveLoc))
            {
                dir = "C:\\test\\" + testSaveLoc + "\\";
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllText(dir + "\\columnheader.txt",
                    "Source\tTarget\tHH\tHT\tHL\tTH\tTT\tTL"
                    );
            }

            StartCoroutine(
                RecordClusterToClusterVisibilityData(
                    clusterSubList, dir,
                    testAgentA, testAgentB,
                    testRadius,
                    testIsStandingA, testIsStandingB)
            );
            runClusterListCheck = false;
        }

        if (runFileClusterListCheck)
        {
            string dir = string.Empty;
            if (!string.IsNullOrEmpty(testSaveLoc))
            {
                dir = "C:\\test\\" + testSaveLoc + "\\";
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllText(dir + "\\columnheader.txt",
                    "Source\tTarget\tHH\tHT\tHL\tTH\tTT\tTL"
                    );
            }
            StartCoroutine(
                RecordClusterToClusterVisibilityData(
                    clusterPairList, clusterCentersParent, dir,
                    testAgentA, testAgentB,
                    testRadius,
                    testIsStandingA, testIsStandingB)
                );
            runFileClusterListCheck = false;
        }

        if (doFullClusterCheck)
        {
            string dir = string.Empty;
            if (!string.IsNullOrEmpty(testSaveLoc))
            {
                dir = "C:\\test\\" + testSaveLoc + "\\";
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllText(dir + "\\columnheader.txt",
                    "Source\tTarget\tHH\tHT\tHL\tTH\tTT\tTL"
                    );
            }

            StartCoroutine(
                RecordClusterToClusterVisibilityData(
                    clusterCentersParent, dir,
                    testAgentA, testAgentB,
                    testRadius,
                    testIsStandingA, testIsStandingB,
                    testNumClusters) // Cannot be 1
            );
            doFullClusterCheck = false;
        }

        if (test)
        {
            /*
            WaypointData wd = GetWaypointData(testID);
            if (wd != null)
                Debug.Log(string.Join(";", GetWaypointData(testID).Value.neighborIDs));
            */
            /*
            float spacing = 1.25f;
            string offsets = CalculateOffsetsOfWaypoints(spacing, waypointMeshData);
            System.IO.File.WriteAllText("C:\\test\\2023-07-24_offsets_c.txt", offsets);
            */
            /*
            //waypointMeshData.waypointData[0].neighborIDs[8] = "test"; // Works
            float spacing = 1.25f;
            //FixOffsetsOfWaypoints(spacing, waypointMeshData, 0.05f);
            string offsets = CalculateOffsetsOfWaypoints(spacing, waypointMeshData, true);
            System.IO.File.WriteAllText("C:\\test\\2023-07-07_offsets_misaligned.txt", offsets);
            */
            /*
            RemoveMisalignedNodes(waypointMeshData, misalignedNodeTextAsset);
            UnityEditor.EditorUtility.SetDirty(waypointMeshData); // MUST BE DONE TO SAVE IT BETWEEN EDITOR RELOADS
            */
            /*
            CreateListOfConnectedPoints(_waypointMeshData.waypointLookupTable["(0,0)"]);
            string connPts = "";
            foreach(WaypointData wd in inputList)
            {
                connPts += wd.waypointID + "\n";
            }
            System.IO.File.WriteAllText("C:\\test\\2023-07-31_connectedPoints.txt", connPts);
            */
            /*
            RemoveUnlistedNodes(waypointMeshData, listedNodeTextAsset);
            UnityEditor.EditorUtility.SetDirty(waypointMeshData); // MUST BE DONE TO SAVE IT BETWEEN EDITOR RELOADS
            */
            /*
            VisualizeWaypoints();

            string ao = "(35,30)", af = "(106,103)";
            string bo = "(36,30)", bf = "(238,99)";
            WaypointData ao_wd = GetWaypointData(ao), af_wd = GetWaypointData(af);
            WaypointData bo_wd = GetWaypointData(bo), bf_wd = GetWaypointData(bf);

            Debug.Log(GetRouteAsString(GetRoute(ao_wd, af_wd)));
            Debug.Log(GetRouteAsString(GetRoute(bo_wd, bf_wd)));
            */

            /*
            if (testTransform != null)
            {
                Vector2 center = new Vector2(testTransform.position.x, testTransform.position.z);
                List<string> foundIDs = FindWaypointIDsWithinCircle(center, testRadius);
                Debug.Log("FoundIDs: " + string.Join(";", foundIDs));
            }
            */

            /*
            Vector3 locA = testTransformA.position;
            Vector3 locB = testTransformB.position;

            List<string> clusterA = FindWaypointIDsWithinCircle(new Vector2(locA.x, locA.z), testRadius);
            List<string> clusterB = FindWaypointIDsWithinCircle(new Vector2(locB.x, locB.z), testRadius);

            StartCoroutine(
                GetWaypointPairByFunction(clusterA, clusterB, WaypointClusterFunction.MINIMUM_NONZERO_VISIBILITY,
                testAgentA, testAgentB, "")
                );
            */

            /* // Test individual (use with test2)
            WaypointData wpA = _waypointMeshData.waypointLookupTable[testTransformA.name];
            WaypointData wpB = _waypointMeshData.waypointLookupTable[testTransformB.name];

            testAgentA.GetComponent<AgentMovement>().TeleportToWaypoint(wpA);
            testAgentB.GetComponent<AgentMovement>().TeleportToWaypoint(wpB);

            WaypointVisibilityController wvcA = testAgentA.GetComponent<WaypointVisibilityController>();
            WaypointVisibilityController wvcB = testAgentB.GetComponent<WaypointVisibilityController>();

            if (!isStandingA)
                wvcA.SetProne(testAgentB.transform);
            else
                wvcA.SetStanding();
            if (!isStandingB)
                wvcB.SetProne(testAgentA.transform);
            else
                wvcB.SetStanding();

            // Wait for prone movement to finish
            */


            //int[] vis = VisibilityController.GetArrayOfBodyPointsVisible(
            //    testAgentA.transform,
            //    testAgentB.transform,
            //    isStandingA,
            //    isStandingB,
            //    true);
            //Debug.Log("BodyPoints " + testTransformA.name + "->" + testTransformB + "\t" + string.Join("\t", vis));


            /*
            Debug.Log("Agent for " + testTransformA + ": " + VisibilityController.GetEncapsulatingAgentForSubObject(testTransformA.gameObject));
            */
            
            string dir = "C:\\test\\"+testSaveLoc+"\\";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            System.IO.File.WriteAllText(dir + "\\columnheader.txt",
                "Source\tTarget\tHH\tHT\tHL\tTH\tTT\tTL"
                );

            StartCoroutine(
                RecordClusterToClusterVisibilityData(
                    clusterCentersParent, dir,
                    testAgentA, testAgentB,
                    testRadius,
                    testIsStandingA, testIsStandingB,
                    testNumClusters) // Cannot be 1
            );
            
            //VisualizeWaypoints();
            test = false;
        }

        if (test2)
        {
            int[] vis = VisibilityController.GetArrayOfBodyPointsVisible(
                testAgentA.transform,
                testAgentB.transform,
                testIsStandingA,
                testIsStandingB,
                true);
            Debug.Log("BodyPoints " + testTransformA.name + "->" + testTransformB + "\t" + string.Join("\t", vis));

            test2 = false;
        }
#endif

        if (drawDebugLines)
        {
            foreach(KeyValuePair<string,WaypointData> kvp in waypointMeshData.waypointLookupTable)
            {
                kvp.Value.DrawDebugLines();
            }
            drawDebugLines = false;
        }
    }

    protected virtual void OnStartedProne(ScoutAgent agent)
    {

    }

    public virtual void OnFinishedProne(ScoutAgent agent)
    {
        int[] vis = VisibilityController.GetArrayOfBodyPointsVisible(
                testAgentA.transform,
                testAgentB.transform,
                testIsStandingA,
                testIsStandingB,
                true);
        /*Debug.Log("BodyPoints " + testTransformA.name + "->" + testTransformB + "\t" + string.Join("\t", vis));*/

    }



    private void Start()
    {
        waypointMeshData.SetupDictionary();
        onWaypointsSetup?.Invoke();
    }

    protected virtual void SetupWaypointsFromFile(TextAsset waypointData)
    {
        Dictionary<string, GameObject> waypointLookupTable = new Dictionary<string, GameObject>();
        Dictionary<string, string[]> waypointNeighborData = new Dictionary<string, string[]>();

        string[] lines = waypointData.text.Split('\n');
        // Create all waypoints with right name and position
        foreach (string line in lines)
        {
            string[] parts = line.Split('\t');
            string thisWaypointID = parts[0];
            Vector3 loc = StringUtility.StringToVector3(parts[1]);
            GameObject waypoint = Instantiate(waypointPrefab, waypointParent);
            waypoint.name = thisWaypointID;
            waypoint.transform.position = loc;

            // Add them to a dictionary where the key is the name and the value is the waypoint object
            waypointLookupTable.Add(thisWaypointID, waypoint);
            waypointNeighborData.Add(thisWaypointID, ArrayUtility.RangeSubset(parts, 2, NumWaypointConnections));
        }

        // Re-read the text file and set up the neighbors, using the Dictionary
        foreach (KeyValuePair<string, GameObject> kvp in waypointLookupTable)
        {
            string[] neighborData = waypointNeighborData[kvp.Key];
            for (int i = 0; i < neighborData.Length; ++i)
            {
                if (neighborData[i] != "null")
                {
                    kvp.Value.GetComponent<Waypoint>().SetNeighbor(
                        i + 1,
                        waypointLookupTable[neighborData[i]].GetComponent<Waypoint>()
                        );
                }
            }
        }
    }

    protected virtual void SetupWaypointsFromFile_DataDriven(TextAsset waypointData)
    {
        string[] lines = waypointData.text.Split('\n');
        // Create all waypoints with right name and position
        foreach (string line in lines)
        {
            WaypointData wd = new WaypointData();

            string[] parts = line.Split('\t');
            wd.waypointID = parts[0];
            wd.location = StringUtility.StringToVector3(parts[1]);
            wd.neighborIDs = new string[NumWaypointConnections+1];
            wd.neighborIDs[0] = wd.waypointID;
            for (int i=1; i<=NumWaypointConnections; ++i)
            {
                if (parts[i+1] != "null")
                {
                    wd.neighborIDs[i] = parts[i + 1];
                }
            }

            waypointDataList.Add(wd);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="spacing"></param>
    /// <param name="wmd"></param>
    /// <param name="maxX"></param>
    /// <param name="maxY"></param>
    protected virtual string CalculateOffsetsOfWaypoints(float spacing, WaypointMeshData wmd, bool onlyIfMisaligned = false)
    {
        string t = "\t";
        Vector3 initValue = wmd.waypointData[0].location;
        string output = "";
        foreach (WaypointData wd in wmd.waypointData)
        {
            int[] coord = StringUtility.StringToIntPair(wd.waypointID);
            float offsetX = wd.location.x - (initValue.x + coord[0] * spacing);
            float offsetZ = wd.location.z - (initValue.z + coord[1] * spacing);
            if (!onlyIfMisaligned || 
                (onlyIfMisaligned && 
                    (!string.IsNullOrEmpty(offsetX.ToString("#.###")) || !string.IsNullOrEmpty(offsetZ.ToString("#.###")))
                    )
                )
            {
                Vector3 theoreticalLoc = new Vector3(initValue.x + coord[0] * spacing, wd.location.y, initValue.z + coord[1] * spacing);
                string line = wd.waypointID + t + offsetX.ToString("#.###") + t + offsetZ.ToString("#.###") + t + theoreticalLoc + "\n";
                output += line;
            }
        }
        return output;
    }

    protected virtual void FixOffsetsOfWaypoints(float spacing, WaypointMeshData wmd, float offsetToFix)
    {
        string t = "\t";
        Vector3 initValue = wmd.waypointData[0].location;
        bool test = false;
        for (int i=0; i< (test ? 5 : wmd.waypointData.Count); ++i)
        {
            WaypointData wd = wmd.waypointData[i];
            int[] coord = StringUtility.StringToIntPair(wd.waypointID);
            float offsetX = wd.location.x - (initValue.x + coord[0] * spacing);
            float offsetZ = wd.location.z - (initValue.z + coord[1] * spacing);
            if (test) Debug.Log("i=" + i + "; offsetX=" + offsetX + "; offsetZ=" + offsetZ);
            float delta = 0.001f;
            if (MathUtility.AreEqual(offsetX, offsetToFix, delta) || 
                MathUtility.AreEqual(offsetZ, offsetToFix, delta)
                )
            {
                Vector3 theoreticalLoc = new Vector3(initValue.x + coord[0] * spacing, wd.location.y, initValue.z + coord[1] * spacing);
                wd.location = theoreticalLoc;
                if (test) Debug.Log("i=" + i + "; new loc=" + theoreticalLoc);
                wmd.waypointData[i] = wd;
            }
        }
    }

    protected virtual void RemoveMisalignedNodes(WaypointMeshData wmd, TextAsset misalignedNodeTextAsset)
    {
        List<string> misalignedNodes = new List<string>();
        string[] lines = misalignedNodeTextAsset.text.Split('\n');
        foreach(string line in lines)
        {
            misalignedNodes.Add(line.Split('\t')[0]);
        }

        List<WaypointData> wdcopy = new List<WaypointData>(wmd.waypointData);
        for (int i = wdcopy.Count-1; i >= 0; --i)
        {
            WaypointData wd = wdcopy[i];
            if (misalignedNodes.Contains(wd.waypointID))
            {
                wmd.waypointData.RemoveAt(i);
            }
            else
            {
                for (int j=0; j<wd.neighborIDs.Length; ++j)
                {
                    if (misalignedNodes.Contains(wd.neighborIDs[j]))
                    {
                        wd.neighborIDs[j] = string.Empty;
                    }
                }
            }
        }
    }

    protected virtual void RemoveUnlistedNodes(WaypointMeshData wmd, TextAsset listedNodeTextAsset)
    {
        List<string> listedNodes = new List<string>();
        string[] lines = listedNodeTextAsset.text.Split('\n');
        foreach (string line in lines)
        {
            listedNodes.Add(line.Split('\t')[0]);
        }

        List<WaypointData> wdcopy = new List<WaypointData>(wmd.waypointData);
        for (int i = wdcopy.Count - 1; i >= 0; --i)
        {
            WaypointData wd = wdcopy[i];
            if (!listedNodes.Contains(wd.waypointID))
            {
                wmd.waypointData.RemoveAt(i);
            }
        }
    }


    #endregion

    #region WAYPOINT_RETRIEVAL

    public static WaypointData GetWaypointData(string waypointID)
    {
        if (_waypointMeshData == null)
        {
            Debug.Log("_waypointMeshData is null");
            return null;
        }

        if (_waypointMeshData.waypointLookupTable == null)
        {
            Debug.Log("_waypointMeshData.waypointLookupTable is null");
            return null;
        }

        if (_waypointMeshData.waypointLookupTable.ContainsKey(waypointID))
            return _waypointMeshData.waypointLookupTable[waypointID];
        else
            return null;
    }

    public static WaypointData GetNeighborInDirection(WaypointData waypointA, int direction)
    {
        if (waypointA != null)
        {
            if (direction >= 0 && direction <= NumWaypointConnections)
            {
                return GetWaypointData(waypointA.neighborIDs[direction]);
            }
        }
        return null;
    }

    #endregion

    public static float GetProbability(GameObject origin, GameObject target)
    {
        float targetHeight = 1.4f;
        Vector3 tgtPos = target.GetComponent<ScoutAgent>().currentWaypoint.location + new Vector3(0f, targetHeight, 0f);
        Vector3 agentPosition = origin.GetComponent<ScoutAgent>().currentWaypoint.location + new Vector3(0f, targetHeight, 0f);
        if (VisibilityController.IsObjectOccluded(agentPosition, tgtPos) ||
            VisibilityController.IsObjectOccluded(tgtPos, agentPosition))
            return 0f;

        return ProbabilityCalculation(origin, target);
    }

    public static float ProbabilityCalculation(GameObject atkr, GameObject opp)
    {
        Vector3 atkrPos = atkr.GetComponent<ScoutAgent>().currentWaypoint.location;
        Vector3 oppPos = opp.GetComponent<ScoutAgent>().currentWaypoint.location;
        float distanceToEnemy = (atkrPos - oppPos).magnitude;

        float flatDistance = Mathf.Sqrt(Mathf.Pow(atkrPos.x - oppPos.x, 2f) + Mathf.Pow(atkrPos.z - oppPos.z, 2f));
        float vertDistance = (atkrPos.y - oppPos.y); // no absolute value since we want it signed
        float angleToEnemy_deg = -Mathf.Atan2(vertDistance, flatDistance) * Mathf.Rad2Deg;

        float range = 50f;
        float jFactor = 0.633f;
        float hFactor = 1f / (Mathf.Pow(range, jFactor) * Mathf.Log(range, 10f));
        // 0.002f is more realistic for an m4 if the range goes to realistic distances
        // This equation gives 0.25 for range 15 and 0.007 for range 90)
        // Infinite range means always a perfect hit, no misses

        // Need Transforms and postures
        float pointVisibilityIndex = VisibilityController.GetPercentageOfBodyPointsVisible(
            atkr.transform, opp.transform,
            atkr.GetComponent<ScoutAgent>().IsStanding(),
            opp.GetComponent<ScoutAgent>().IsStanding(),
            true);

        float accuracy = pointVisibilityIndex * 1f;

        float exp = Mathf.Exp(hFactor * (range - distanceToEnemy));
        float denom = 10f * (hFactor) * (1 + (1f / 90f) * angleToEnemy_deg * accuracy) * ((1 - accuracy) / accuracy) * distanceToEnemy;
        float maxProb = exp / (denom + exp);

        if (float.IsNaN(maxProb))
            maxProb = 0f;

        return maxProb;
    }

    public static WaypointData[] GetRoute(WaypointData start, WaypointData end)
    {
        if (start == null || end == null)
            return new WaypointData[0];

        // See if we have any routes to the endpoint
        if (establishedRoutes.ContainsKey(end))
        {
            // See if the start point is in any of these routes
            RouteObject routeContainingCurrentWP = null;
            foreach (RouteObject routeObj in establishedRoutes[end])
            {
                if (routeObj.ContainsWaypoint(start))
                {
                    routeContainingCurrentWP = routeObj;
                }
            }

            if (routeContainingCurrentWP != null)
            {
                return routeContainingCurrentWP.GetPartialFromShortestPath(start).ToArray();
            }
            else // The endpoint is established, but the start is not in any established routes
            {
                Dijkstra dijkstra = new Dijkstra(start, end, _waypointMeshData);
                List<string> shortestPath_ids = dijkstra.shortestPath;
                List<WaypointData> shortestPath = new List<WaypointData>();
                foreach (string wv in shortestPath_ids)
                {
                    shortestPath.Add(_waypointMeshData.waypointLookupTable[wv]);
                }
                RouteObject newRoute = new RouteObject(dijkstra, shortestPath);
                establishedRoutes[end].Add(newRoute);
                return shortestPath.ToArray();
            }
        }
        else
        {
            Dijkstra dijkstra = new Dijkstra(start, end, _waypointMeshData);
            List<string> shortestPath_ids = dijkstra.shortestPath;
            List<WaypointData> shortestPath = new List<WaypointData>();
            foreach (string wv in shortestPath_ids)
            {
                shortestPath.Add(_waypointMeshData.waypointLookupTable[wv]);
            }
            RouteObject newRoute = new RouteObject(dijkstra, shortestPath);
            establishedRoutes.Add(end, new List<RouteObject>() { newRoute });
            //Debug.Log("GetRoute: " + start.waypointID + "->" + end.waypointID + ": " + GetRouteAsString(shortestPath.ToArray()));
            return shortestPath.ToArray();
        }
    }

    public static string GetRouteAsString(WaypointData[] route, string delimiter = ";")
    {
        string output = "";
        foreach(WaypointData wd in route)
        {
            output += wd.waypointID + delimiter;
        }
        return output;
    }

    public static Direction GetDirectionFromWaypoint(ScoutAgent agent, WaypointData waypoint)
    {
        for (int i = 0; i <= NumWaypointConnections; ++i)
        {
            if (agent.currentWaypoint.neighborIDs[i].Trim() == waypoint.waypointID.Trim())
            {
                return new Direction(i);
            }
        }
        return new Direction(0);
    }

    public static List<WaypointData> GetWaypointListFromString(string waypointList, string delimiter = ";")
    {
        string[] waypointIDs = waypointList.Split(new string[] { delimiter }, System.StringSplitOptions.RemoveEmptyEntries);
        List<WaypointData> waypointObjectList = new List<WaypointData>();
        foreach (string waypointID in waypointIDs)
        {
            if (_waypointMeshData.waypointLookupTable.ContainsKey(waypointID))
            {
                WaypointData wm = _waypointMeshData.waypointLookupTable[waypointID];
                if (wm != null)
                {
                    waypointObjectList.Add(wm);
                }
            }
            else
            {
                Debug.LogError(waypointID + " not in lookup table!");
            }
        }
        return waypointObjectList;
    }

    /*// Go through each point's neighbors recursively and add them to a master list
    public static List<WaypointData> CreateListOfConnectedPoints(WaypointData start)
    {
        if (!inputList.Contains(start))
        {
            count++;
            if (count >= countLimit)
            {
                Debug.Log("Ended on " + start.waypointID);
                return inputList;
            }
            inputList.Add(start); // MEMORY OVERFLOW when doing all points
            for (int i=1; i<start.neighborIDs.Length; ++i)
            {
                string id = start.neighborIDs[i];
                if (string.IsNullOrEmpty(id)) continue;

                WaypointData wd = _waypointMeshData.waypointLookupTable[id];
                if (!inputList.Contains(wd))
                {
                    inputList.AddRange(CreateListOfConnectedPoints(wd));
                }
            }
        }
        return inputList;
    }*/

    // Go through each point and see if can connect back to (0,0)
    public static void CreateListOfConnectedPoints(WaypointData start)
    {
        foreach(KeyValuePair<string, WaypointData> kvp in _waypointMeshData.waypointLookupTable)
        {
            Dijkstra dijkstra = new Dijkstra(start, kvp.Value, _waypointMeshData, false);
            if (dijkstra.GetDistance() >= 0)
            {
                inputList.Add(kvp.Value);
            }
        }
    }

    private void VisualizeWaypoints()
    {
        foreach (KeyValuePair<string, WaypointData> kvp in _waypointMeshData.waypointLookupTable)
        {
            GameObject wpgo = Instantiate(waypointPrefab, transform);
            wpgo.name = kvp.Key;
            wpgo.transform.position = kvp.Value.location;
        }
    }

#region AUXILIARY_FUNCTIONS

    public List<string> FindWaypointIDsWithinCircle(Vector2 center, float radius)
    {
        List<string> foundWaypoints = new List<string>();
        foreach (KeyValuePair<string, WaypointData> kvp in _waypointMeshData.waypointLookupTable)
        {
            Vector3 loc = kvp.Value.location;
            Vector2 loc2 = new Vector3(loc.x, loc.z);
            if (Vector2.Distance(center, loc2) <= radius)
            {
                foundWaypoints.Add(kvp.Key);
            }
        }
        return foundWaypoints;
    }

    public enum WaypointClusterFunction
    {
        MINIMUM_NONZERO_VISIBILITY,
        AVERAGE_VISIBILITY
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clusterA"></param>
    /// <param name="clusterB"></param>
    /// <param name="clusterFunction"></param>
    /// <param name="agentA"></param>
    /// <param name="agentB"></param>
    /// <param name="isStandingA"></param>
    /// <param name="isStandingB"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public IEnumerator GetWaypointPairByFunction(List<string> clusterA, List<string> clusterB, WaypointClusterFunction clusterFunction,
        GameObject agentA, GameObject agentB,
        bool isStandingA, bool isStandingB,
        string savePath)
    {
        // Get a list of all possible pairs, using one point from clusterA and one from clusterB
        List<string[]> pairs = new List<string[]>();
        foreach(string idA in clusterA)
        {
            foreach(string idB in clusterB)
            {
                pairs.Add(new string[2] { idA, idB });
            }
        }

        // Do not use any colliders for the entire agent (might be a defunct requirement)
        agentA.GetComponent<Collider>().enabled = false;
        agentB.GetComponent<Collider>().enabled = false;

        // Go pair-by-pair
        string output = "";
        foreach (string[] pair in pairs)
        {
            // Create WaypointData from waypoint IDs
            WaypointData wpA = _waypointMeshData.waypointLookupTable[pair[0]];
            WaypointData wpB = _waypointMeshData.waypointLookupTable[pair[1]];

            // Move agents to waypoints instantly
            agentA.GetComponent<AgentMovement>().TeleportToWaypoint(wpA);
            agentB.GetComponent<AgentMovement>().TeleportToWaypoint(wpB);

            // Get waypointVisibilityControllers for each agent so we can test visibility with raycasts
            WaypointVisibilityController wvcA = agentA.GetComponent<WaypointVisibilityController>();
            WaypointVisibilityController wvcB = agentB.GetComponent<WaypointVisibilityController>();

            //yield return new WaitForFixedUpdate();

            // Check all four possibilities: stand/prone vs. stand/prone
            if (isStandingA && isStandingB) // stand / stand
            {
                wvcA.SetStanding();
                wvcB.SetStanding();
                yield return new WaitForFixedUpdate();
            }
            else if (isStandingA && !isStandingB) // stand / prone
            {
                wvcA.SetStanding();
                wvcB.SetStanding(); // Reset position
                yield return new WaitForFixedUpdate(); // wait
                yield return wvcB.SetProneAsync(agentA.transform); // Asynchronously have agent fall into prone position
            }
            else if (!isStandingA && isStandingB) // prone / stand
            {
                wvcB.SetStanding();
                wvcA.SetStanding();
                yield return new WaitForFixedUpdate();
                yield return wvcA.SetProneAsync(agentB.transform);
            }
            else // prone / prone
            {
                wvcA.SetStanding();
                wvcB.SetStanding();
                yield return new WaitForFixedUpdate();
                yield return wvcA.SetProneAsync(agentB.transform);
                yield return wvcB.SetProneAsync(agentA.transform);
            }
            yield return new WaitForFixedUpdate();

            // Check Head->{Head,Torso,Legs} and Torso->{Head,Torso,Legs}
            int[] vis = VisibilityController.GetArrayOfBodyPointsVisible(
                agentA.transform, agentB.transform, isStandingA, isStandingB, true
                );
            string line = pair[0] + "\t" + pair[1] + "\t" + string.Join("\t", vis) + "\n";
            Debug.Log(line); // line data
            output += line; // Apply to final output
            
        }
        Debug.Log(output); // Write file data
        yield return null;
        // Write all output
        if (!string.IsNullOrEmpty(savePath))
            System.IO.File.WriteAllText(savePath, output);
    }

    /// <summary>
    /// Create clusters and then go point-by-point
    /// </summary>
    /// <param name="clusterCentersParent"></param>
    /// <param name="saveFolderPath"></param>
    /// <param name="agentA"></param>
    /// <param name="agentB"></param>
    /// <param name="radius"></param>
    /// <param name="isAgentAStanding"></param>
    /// <param name="isAgentBStanding"></param>
    /// <param name="maxClusters"></param>
    /// <returns></returns>
    public IEnumerator RecordClusterToClusterVisibilityData(
        Transform clusterCentersParent, string saveFolderPath,
        GameObject agentA, GameObject agentB, float radius,
        bool isAgentAStanding, bool isAgentBStanding,
        int maxClusters = -1
        )
    {
        List<Transform> clustersToCheck = new List<Transform>();
        for (int i = 0; i < (maxClusters <= 0 ? clusterCentersParent.childCount : maxClusters); ++i)
        {
            clustersToCheck.Add(clusterCentersParent.GetChild(i));
        }

        return RecordClusterToClusterVisibilityData(clustersToCheck, saveFolderPath, agentA, agentB, radius, isAgentAStanding, isAgentBStanding);
    }

    public IEnumerator RecordClusterToClusterVisibilityData(
        List<Transform> clustersToCheck, string saveFolderPath,
        GameObject agentA, GameObject agentB, float radius,
        bool isAgentAStanding, bool isAgentBStanding)
    {
        // Iterate through clusters -- use maxClusters if it is greater than 0, otherwise do all
        for (int i=0; i < clustersToCheck.Count; ++i)
        {
            Transform clusterCenterA = clustersToCheck[i];
            for (int j = 0; j < clustersToCheck.Count; ++j)
            {
                // Now we are looking at a cluster for the source agent and one for the target agent
                if (j != i) // Ignore the case where agent and target are in the same cluster
                {
                    /*
                    yield return null;

                    Transform clusterCenterB = clustersToCheck[j];

                    Vector3 locA = clusterCenterA.position;
                    Vector3 locB = clusterCenterB.position;

                    // Get all the waypoint IDs for each cluster
                    List<string> clusterA = FindWaypointIDsWithinCircle(new Vector2(locA.x, locA.z), radius);
                    List<string> clusterB = FindWaypointIDsWithinCircle(new Vector2(locB.x, locB.z), radius);

                    // Now iterate between pairs of points in each cluster
                    // each pair is a point from cluster A and cluster B
                    yield return GetWaypointPairByFunction(clusterA, clusterB, WaypointClusterFunction.MINIMUM_NONZERO_VISIBILITY, // ignore the function for now
                        agentA, agentB,
                        isAgentAStanding, isAgentBStanding,
                        saveFolderPath + clusterCenterA.name + "-" + clusterCenterB.name + ".txt");

                    //System.IO.File.WriteAllText(saveFolderPath + clusterCenterA.name + "-" + clusterCenterB.name + ".txt", pairsVis);
                    */
                    yield return RecordClusterPair(clusterCenterA, clustersToCheck[j],
                        saveFolderPath, agentA, agentB, radius,
                        isAgentAStanding, isAgentBStanding
                        );
                }
            }
        }
    }

    public IEnumerator RecordClusterToClusterVisibilityData(
        TextAsset clusterPairListFile, Transform clusterParent, string saveFolderPath,
        GameObject agentA, GameObject agentB, float radius,
        bool isAgentAStanding, bool isAgentBStanding)
    {
        string[] lines = clusterPairListFile.text.Split('\n');
        List<Transform[]> linePairList = new List<Transform[]>();
        foreach(string line in lines)
        {
            if (!string.IsNullOrEmpty(line.Trim()))
            {
                string[] lineParts = line.Trim().Split('\t');
                if (lineParts.Length >= 2)
                {
                    Transform childA = GetChildWithName(clusterParent, lineParts[0]);
                    Transform childB = GetChildWithName(clusterParent, lineParts[1]);
                    if (childA != null && childB != null)
                    {
                        linePairList.Add(new Transform[] { childA, childB });
                    }
                }
            }
        }

        foreach(Transform[] pair in linePairList)
        {
            yield return RecordClusterPair(pair[0], pair[1],
                saveFolderPath, agentA, agentB, radius, isAgentAStanding, isAgentBStanding);
        }
    }

    protected virtual Transform GetChildWithName(Transform parent, string name)
    {
        foreach(Transform child in parent)
        {
            if (child.gameObject.name == name)
                return child;
        }
        return null;
    }


    public IEnumerator RecordClusterPair(Transform clusterCenterA, Transform clusterCenterB, string saveFolderPath,
        GameObject agentA, GameObject agentB, float radius,
        bool isAgentAStanding, bool isAgentBStanding)
    {
        yield return null;

        Vector3 locA = clusterCenterA.position;
        Vector3 locB = clusterCenterB.position;

        // Get all the waypoint IDs for each cluster
        List<string> clusterA = FindWaypointIDsWithinCircle(new Vector2(locA.x, locA.z), radius);
        List<string> clusterB = FindWaypointIDsWithinCircle(new Vector2(locB.x, locB.z), radius);

        // Now iterate between pairs of points in each cluster
        // each pair is a point from cluster A and cluster B
        yield return GetWaypointPairByFunction(clusterA, clusterB, WaypointClusterFunction.MINIMUM_NONZERO_VISIBILITY, // ignore the function for now
            agentA, agentB,
            isAgentAStanding, isAgentBStanding,
            saveFolderPath + clusterCenterA.name + "-" + clusterCenterB.name + ".txt");

        //System.IO.File.WriteAllText(saveFolderPath + clusterCenterA.name + "-" + clusterCenterB.name + ".txt", pairsVis);

    }

    #endregion
}
