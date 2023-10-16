using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to create brand new WaypointMeshData objects and objects from subsets of WaypointMeshData.
/// </summary>
public class WaypointMeshEditor : MonoBehaviour
{
    public WaypointMeshController wmc;
    public GameObject waypointPrefab;

    [Header("Create Trimmed Down Submesh from Larger Mesh")]
    public GameObject southWestOrigin;
    public GameObject northEastTerminus;
    public WaypointMeshData emptyWMD;
    public Transform newSubmeshParent;
    public bool doTrimDown = false;

    [Header("Check if points are inside the terrain")]
    public Transform terrainPointToCheck;
    public string pathForSunkenPoints;
    public bool doTerrainPointCheck = false;

    [Header("Fix points below terrain")]
    public WaypointMeshData wmdToFix;
    public TextAsset fixData;
    [Tooltip("Add this to points so they are a little bit above terrain")]
    public float extraOffset = 0.2f;

    protected virtual void Update()
    {
        if (doTrimDown)
        {
            CreateTrimmedDownSubmesh(southWestOrigin, northEastTerminus, emptyWMD);
            doTrimDown = false;
        }

        if (doTerrainPointCheck)
        {
            //Debug.Log("Y-distance to terrain: " + CheckYDistanceToTerrain(terrainPointToCheck));
            //CheckAllSunkenPoints(terrainPointToCheck, pathForSunkenPoints);
            //FixTerrainPoints(wmdToFix, fixData, extraOffset); // Don't call more than once
            FixNeighbor0Problem(wmdToFix);
            doTerrainPointCheck = false;
        }
    }

    /// <summary>
    /// Makes prefabs and new scriptable object
    /// </summary>
    /// <param name="swOrigin"></param>
    /// <param name="neTerminus"></param>
    public virtual void CreateTrimmedDownSubmesh(GameObject swOrigin, GameObject neTerminus, WaypointMeshData newEmptyWMD)
    {
        string swID = swOrigin.name;
        string neID = neTerminus.name;
        CreateTrimmedDownSubmesh(swID, neID, newEmptyWMD);
    }

    public virtual void CreateTrimmedDownSubmesh(string swOrigin, string neTerminus, WaypointMeshData newEmptyWMD)
    {
        WaypointMeshData wmd = wmc.waypointMeshData;
        if (!(wmd.waypointLookupTable.ContainsKey(swOrigin) &&
            wmd.waypointLookupTable.ContainsKey(neTerminus)))
        {
            return;
        }

        int[] swPair = StringUtility.StringToIntPair(swOrigin);
        int[] nePair = StringUtility.StringToIntPair(neTerminus);

        for (int i=swPair[0]; i<=nePair[0]; ++i)
        {
            for (int j=swPair[1]; j<=nePair[1]; ++j)
            {
                string trialID = StringUtility.IntPairToString(i, j);
                if (wmd.waypointLookupTable.ContainsKey(trialID))
                {
                    WaypointData wd = wmd.waypointLookupTable[trialID];
                    // Prepare waypoint with only neighbors within new bounds
                    WaypointData wdA = new WaypointData();
                    wdA.waypointID = wd.waypointID;
                    wdA.location = wd.location;
                    wdA.neighborIDs = new string[wd.neighborIDs.Length];
                    wdA.neighborIDs[0] = wd.neighborIDs[0];
                    for (int k=1; k<wd.neighborIDs.Length; ++k)
                    {
                        string neighbor = wd.neighborIDs[k];
                        if (!string.IsNullOrEmpty(neighbor) &&
                            StringUtility.IsIDBetweenSWandNEcorners(
                                neighbor,
                                swOrigin,
                                neTerminus
                            ))
                        {
                            wdA.neighborIDs[k] = neighbor;
                        }
                        else
                        {
                            wdA.neighborIDs[k] = string.Empty;
                        }
                    }

                    // New waypoint, wdA is ready
                    newEmptyWMD.waypointData.Add(wdA);
                    // instantiate prefab
                    GameObject wpPrefab = Instantiate(waypointPrefab, newSubmeshParent);
                    wpPrefab.name = trialID;
                }
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(newEmptyWMD);
#endif
    }


    /// <summary>
    /// Send a ray up and/or down to see how far away the terrain is
    /// </summary>
    /// <param name="transformToCheck">The transform to check</param>
    /// <returns>Distance to terrain. If negative, then below terrain</returns>
    public virtual float CheckYDistanceToTerrain(Transform transformToCheck)
    {
        Vector3 pos = transformToCheck.position;
        //return CheckYDistanceToTerrain(pos);
        return CheckYDistanceDownward(pos);
    }

    public virtual float CheckYDistanceToTerrain(Vector3 positionToCheck)
    {
        RaycastHit[] uphits = Physics.RaycastAll(positionToCheck, Vector3.up, 200f);
        RaycastHit[] downhits = Physics.RaycastAll(positionToCheck, Vector3.down, 200f);
        List<RaycastHit> allHits = new List<RaycastHit>(uphits);
        allHits.AddRange(downhits);
        float minDist = Mathf.Infinity;
        List<RaycastHit> closestHit = new List<RaycastHit>();
        foreach(RaycastHit hit in allHits)
        {
            if (hit.transform.CompareTag("ground"))
            {
                float d = Vector3.Distance(hit.point, positionToCheck);
                if (d < minDist)
                {
                    minDist = d;
                    closestHit.Clear();
                    closestHit.Add(hit);
                }
            }
        }

        if (closestHit.Count > 0)
        {
            return positionToCheck.y - closestHit[0].point.y;
        }
        else
        {
            return float.NaN;
        }
    }

    public virtual float CheckYDistanceDownward(Vector3 positionToCheck)
    {
        RaycastHit[] downhits = Physics.RaycastAll(new Vector3(positionToCheck.x, 200f, positionToCheck.z), Vector3.down, 300f);
        List<RaycastHit> allHits = new List<RaycastHit>(downhits);

        float minDist = Mathf.Infinity;
        List<RaycastHit> closestHit = new List<RaycastHit>();
        foreach (RaycastHit hit in allHits)
        {
            if (hit.transform.CompareTag("ground"))
            {
                float d = Vector3.Distance(hit.point, positionToCheck);
                if (d < minDist)
                {
                    minDist = d;
                    closestHit.Clear();
                    closestHit.Add(hit);
                }
            }
        }

        if (closestHit.Count > 0)
        {
            return positionToCheck.y - closestHit[0].point.y;
        }
        else
        {
            return float.NaN;
        }
    }

    public virtual void CheckAllSunkenPoints(Transform pointParent, string pathToSave)
    {
        string output = string.Empty;
        foreach(Transform child in pointParent)
        {
            float d = CheckYDistanceToTerrain(child);
            if (d < 0)
            {
                output += child.name + "\t" + d + "\t" + (child.position - new Vector3(0f, d, 0f)) + "\n";
            }
        }
        Debug.Log(output);
        System.IO.File.WriteAllText(pathToSave, output);
    }

    public virtual void FixTerrainPoints(WaypointMeshData wmdToFix, TextAsset fixData, float extraOffset)
    {
        // Set up a dictionary lookup table
        Dictionary<string, float> fixes = new Dictionary<string, float>();
        string[] lines = fixData.text.Split('\n');
        for (int i=0; i<lines.Length; ++i)
        {
            string[] parts = lines[i].Split('\t');
            float offset = float.Parse(parts[1]);
            fixes.Add(parts[0], offset);
        }
        Debug.Log("Fixed size=" + fixes.Count);

        for (int i=0; i<wmdToFix.waypointData.Count; ++i)
        {
            WaypointData wd = wmdToFix.waypointData[i];
            if (fixes.ContainsKey(wd.waypointID))
            {
                // Need to fix!
                wd.location = new Vector3(
                    wd.location.x,
                    wd.location.y + Mathf.Abs(fixes[wd.waypointID]) + extraOffset,
                    wd.location.z);
                wmdToFix.waypointData[i] = wd;
                Debug.Log("fixed " + wd.waypointID + " -> " + wd.location);
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(wmdToFix); // MUST BE DONE TO SAVE IT 
#endif
        Debug.Log("Done.");
    }

    public virtual void FixNeighbor0Problem(WaypointMeshData wmdToFix)
    {
        for (int i = 0; i < wmdToFix.waypointData.Count; ++i)
        {
            WaypointData wd = wmdToFix.waypointData[i];
            if (wd.waypointID != wd.neighborIDs[0])
            {
                // Offset
                for (int j = wd.neighborIDs.Length-1; j > 0; --j)
                {
                    wd.neighborIDs[j] = wd.neighborIDs[j-1];
                }
                // Set 0th to same
                wd.neighborIDs[0] = wd.waypointID;
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(wmdToFix); // MUST BE DONE TO SAVE IT 
#endif
        Debug.Log("Done.");
    }
}
