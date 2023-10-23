using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Create a grid of waypoints
/// </summary>
public class AutomaticWaypointGenerator : MonoBehaviour
{
    public WaypointMeshController waypointMeshController;

    //public GameObject waypointPrefab;
    public float gridSpacing = 1.25f;
    public float maxHeightDifferential = 2.25f;

    public Transform startingPoint, endingPoint;

    //private NavMesh navMesh;
    private List<Vector3> waypoints = new List<Vector3>();

    private List<List<WaypointData>> waypointGrid = new List<List<WaypointData>>();
    private List<WaypointData> invalidWaypoints = new List<WaypointData>();

    public bool deployWaypoints = false;

    public bool showConnections = true, showWaypointMarkers = false;

    [Header("Serialization")]
    public bool saveWaypointData = false;
    public string waypointSaveLocation = string.Empty;

    public bool test = false;
    public Transform testTransform;
    public void LateUpdate()
    {
        if (test)
        {
            Debug.Log(testTransform.position + " --yDist--> " + WaypointMeshEditor.CheckYDistanceToTerrain(testTransform.position));
            test = false;
        }
    }

    private void Start()
    {
        /*
        if (showConnections || !showWaypointMarkers)
        {
            foreach(Transform wp in transform)
            {
                WaypointView wv = wp.GetComponent<WaypointView>();
                if (!showWaypointMarkers)
                    wv.DisableMarker();
                if (showConnections)
                    wv.DrawDebugConnections();
            }
        }*/
    }

    public void Update()
    {
        if (saveWaypointData)
        {
            //SaveWaypointData(waypointSaveLocation);
            saveWaypointData = false;
        }

        if (deployWaypoints)
        {
            Init();
            deployWaypoints = false;
        }
    }

    /*
    void SaveWaypointData(string location)
    {
        string output = string.Empty;

        int areaNum = 0;
        int index = 0;
        foreach(Transform child in transform)
        {
            output += child.GetComponent<WaypointView>().ToSerializedString() + "\n";
        }
        System.IO.File.WriteAllText(location, output);
    }*/

    void Init()
    {
        //navMesh = GetComponent<NavMesh>();
        GenerateWaypoints();
        //PrintWaypoints();
    }

    void GenerateWaypoints()
    {
        Vector3 startPosition = startingPoint.position;
        int gridSizeX = Mathf.Abs(Mathf.FloorToInt((endingPoint.position.x - startingPoint.position.x) / gridSpacing));
        int gridSizeZ = Mathf.Abs(Mathf.FloorToInt((endingPoint.position.z - startingPoint.position.z) / gridSpacing));
        for (int z = 0; z <= gridSizeZ; z++)
        {
            waypointGrid.Add(new List<WaypointData>());
            for (int x = 0; x <= gridSizeX; x++)
            {
                Vector3 point = new Vector3(
                    startPosition.x + x * gridSpacing, 
                    startPosition.y, 
                    startPosition.z + z * gridSpacing);
                
                WaypointData waypoint = new WaypointData();

                NavMeshHit hit;
                if (NavMesh.SamplePosition(point, out hit, 300f, NavMesh.AllAreas))
                {
                    waypoint.waypointID = "(" + x + "," + z + ")";
                    waypoint.location = hit.position;

                    waypoints.Add(hit.position); // waypoints is just for debugging
                }
                else
                {
                    waypoint.waypointID = "(" + x + "," + z + ")_X";
                    invalidWaypoints.Add(waypoint);
                }
                waypointMeshController.waypointMeshData.AddWaypoint(waypoint);
                waypointGrid[z].Add(waypoint);
                if (x-1 >= 0 && (x-1 < waypointGrid[z].Count) && z < waypointGrid.Count)
                {
                    int ii = z;
                    int jj = x - 1;
                    int dir = Directions.W.dir;
                    int oppdir = Directions.E.dir;
                    ConnectWaypoint(waypoint, ii, jj, dir, oppdir);
                }
                if (z-1 >= 0)
                {
                    if ((x - 1 >= 0) && (x -1 < waypointGrid[z-1].Count))
                    {
                        int ii = z - 1;
                        int jj = x - 1;
                        int dir = Directions.SW.dir;
                        int oppdir = Directions.NE.dir;
                        ConnectWaypoint(waypoint, ii, jj, dir, oppdir);
                    }

                    if (x < waypointGrid[z - 1].Count)
                    {
                        int ii = z - 1;
                        int jj = x;
                        int dir = Directions.S.dir;
                        int oppdir = Directions.N.dir;
                        ConnectWaypoint(waypoint, ii, jj, dir, oppdir);
                    }

                    if (x + 1 < waypointGrid[z - 1].Count)
                    {
                        int ii = z - 1;
                        int jj = x + 1;
                        int dir = Directions.SE.dir;
                        int oppdir = Directions.NW.dir;
                        ConnectWaypoint(waypoint, ii, jj, dir, oppdir);
                    }
                }
            }
        }

        
        foreach(WaypointData invalidWP in invalidWaypoints)
        {
            //Destroy(invalidWP);
            waypointMeshController.waypointMeshData.RemoveWaypoint(invalidWP);
        }

        waypointMeshController.waypointMeshData.SetupDictionary();

        Vector3 initLoc = waypointMeshController.waypointMeshData.waypointData[0].location;
        List<WaypointData> wdcopy = new List<WaypointData>(waypointMeshController.waypointMeshData.waypointData);
        List<WaypointData> wd_toRemove = new List<WaypointData>();

        int index = 0;
        foreach (WaypointData wd in wdcopy)
        {
            int[] pair = StringUtility.StringToIntPair(wd.waypointID);
            if ((Mathf.Abs(wd.location.x - (pair[0]*gridSpacing + initLoc.x)) >= 0.01f) ||
                (Mathf.Abs(wd.location.z - (pair[1]*gridSpacing + initLoc.z)) >= 0.01f)
                )
            {
                wd_toRemove.Add(wd);
            }

            index++;
        }

        foreach(WaypointData wd in wd_toRemove)
        {
            waypointMeshController.waypointMeshData.RemoveWaypoint(wd);
        }

        /*
        StartCoroutine(_DrawDebug());
        IEnumerator _DrawDebug()
        {
            yield return new WaitForEndOfFrame();
            foreach (List<GameObject> lgo in waypointGrid)
            {
                foreach (GameObject go in lgo)
                {
                    if (go != null && go.transform.GetChild(0).GetComponent<Renderer>().enabled)
                        go.GetComponent<WaypointView>().DrawDebugConnections();
                }
            }
        }*/

        UnityEditor.EditorUtility.SetDirty(waypointMeshController.waypointMeshData);
    }

    void ConnectWaypoint(WaypointData wd, int ii, int jj, int dir, int oppdir)
    {
        if (Mathf.Abs(wd.location.y - waypointGrid[ii][jj].location.y) <= maxHeightDifferential)
        {
            float pl = PathfindingUtility.CalculatePathLength(wd.location, waypointGrid[ii][jj].location, 5);
            float d = Vector3.Distance(wd.location, waypointGrid[ii][jj].location);
            //Debug.Log(wd.waypointID + ": pl=" + pl + "; d=" + d + "; pl/d=" + (pl/d) + "; wd.location=" + wd.location + "; waypointGrid[" + ii + "][" + jj + "].location=" + waypointGrid[ii][jj].location);
            if (!float.IsNaN(pl) && (pl < 1.25f * d))
            {
                wd.neighborIDs[dir] = waypointGrid[ii][jj].waypointID;
                waypointGrid[ii][jj].neighborIDs[oppdir] = wd.waypointID;

                waypointMeshController.waypointMeshData.waypointLookupTable[wd.waypointID].neighborIDs[dir] = waypointGrid[ii][jj].waypointID;
                waypointMeshController.waypointMeshData.waypointLookupTable[waypointGrid[ii][jj].waypointID].neighborIDs[oppdir] = wd.waypointID;

            }
        }
    }

    void PrintWaypoints()
    {
        string output = "";
        foreach (Vector3 waypoint in waypoints)
        {
            output += string.Format("({0:F1}, {1:F1}, {2:F1})\n", waypoint.x, waypoint.y, waypoint.z);
        }
        Debug.Log(output);
    }

    /*public WaypointView[] GetRandomWaypointViews(int amt = 2)
    {
        WaypointView[] wvs = new WaypointView[amt];
        for (int i=0; i<amt; ++i)
        {
            wvs[i] = transform.GetChild(Random.Range(0, transform.childCount)).GetComponent<WaypointView>();
        }
        return wvs;
    }*/
}