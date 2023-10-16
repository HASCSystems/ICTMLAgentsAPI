using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Direction of travel
/// </summary>
public class Direction
{
    public int dir = 0;

    public Direction(int dir)
    {
        this.dir = dir;
    }
}

public class Directions
{
    // Not using enum since we can change it for different connection topologies
    public static readonly Direction NONE   = new Direction(0);
    public static readonly Direction N      = new Direction(1);
    public static readonly Direction NE     = new Direction(2);
    public static readonly Direction E      = new Direction(3);
    public static readonly Direction SE     = new Direction(4);
    public static readonly Direction S      = new Direction(5);
    public static readonly Direction SW     = new Direction(6);
    public static readonly Direction W      = new Direction(7);
    public static readonly Direction NW     = new Direction(8);
}

[Serializable]
public class WaypointData
{
    public Vector3 location;
    [SerializeField]
    public string waypointID;
    [SerializeField]
    public string[] neighborIDs;

    public static Color[] connColors = new Color[]
    {
        Color.clear,
        Color.cyan,
        Color.green,
        Color.red,
        Color.yellow,
        Color.cyan,
        Color.green,
        Color.red,
        Color.yellow
    };

    public virtual bool HasNeighborInDirection(int direction)
    {
        if (neighborIDs == null)
        {
            Debug.Log("neighborIDs is null for " + waypointID + " in direction " + direction);
            return false;
        }

        if (direction > neighborIDs.Length-1 || direction < 0)
        {
            Debug.Log(waypointID + ": " + direction + " is out of bounds for " + neighborIDs.Length);
        }
        return !string.IsNullOrEmpty(neighborIDs[direction]);
    }

    public virtual WaypointData GetNeighborInDirection(int direction)
    {
        return WaypointMeshController.GetNeighborInDirection(this, direction);
    }

    public virtual int GetDirectionOfNeighbor(WaypointData neighbor)
    {
        for (int i=0; i<neighborIDs.Length; ++i)
        {
            if (neighborIDs[i] == neighbor.waypointID)
                return i;
        }
        return -1;
    }

    public virtual bool HasNeighbor(string waypointID)
    {
        foreach(string s in neighborIDs)
        {
            if (s == waypointID)
                return true;
        }
        return false;
    }

    public void DrawDebugLines()
    {
        for (int i=1; i<=WaypointMeshController.NumWaypointConnections; ++i)
        {
            if (HasNeighborInDirection(i))
            {
                Debug.DrawLine(location, GetNeighborInDirection(i).location, connColors[i], 1e6f, false);
            }
        }
    }

    public void CleanData()
    {
        waypointID = waypointID.Trim();
        for (int i = 0; i < neighborIDs.Length; ++i)
        {
            if (!string.IsNullOrEmpty(neighborIDs[i]))
            {
                neighborIDs[i] = neighborIDs[i].Trim();
            }
        }
    }
}

/// <summary>
/// Main data structure for waypoints in scene. Though this can be used to create GameObject/Prefab-based representations
/// of the waypoints, they must be updated here in order to have effect. Use WaypointMeshManipulator editor script to
/// make changes easily.
/// </summary>
[CreateAssetMenu(fileName = "New WaypointMeshData", menuName = "USC ICT/New WaypointMeshData")]
public class WaypointMeshData : ScriptableObject
{
    [SerializeField]
    public List<WaypointData> waypointData = new List<WaypointData>();

    public Dictionary<string, WaypointData> waypointLookupTable = new Dictionary<string, WaypointData>();
    public void SetupDictionary()
    {
        waypointLookupTable.Clear();
        Debug.Log("A) WaypointMeshData count=" + waypointLookupTable.Count);
        string wps = "Waypoints:";
        foreach (WaypointData wd in waypointData)
        {
            waypointLookupTable.Add(wd.waypointID, wd);
            wps += wd.waypointID + ";";
        }
        Debug.Log("B) WaypointMeshData count=" + waypointLookupTable.Count + "; " + wps);
    }

    public void AddWaypoint(WaypointData newWaypoint)
    {
        SetupDictionary();
        if (!waypointLookupTable.ContainsKey(newWaypoint.waypointID))
        {
            waypointData.Add(newWaypoint);
            CheckWaypointConnections();
            SetupDictionary();
        }
    }

    public void CleanWaypointData()
    {
        List<WaypointData> _waypointData = new List<WaypointData>(waypointData);
        foreach (WaypointData wd in _waypointData)
        {
            wd.CleanData();
        }
        waypointData = _waypointData;

        SetupDictionary();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void SetNewLocationOfWaypoint(string waypointID, Vector3 newLocalPosition)
    {
        Debug.Log("Trying to setting new location for " + waypointID + " to " + newLocalPosition);
        List<WaypointData> _waypointData = new List<WaypointData>(waypointData);
        foreach (WaypointData wd in _waypointData)
        {
            if (wd.waypointID.Trim() == waypointID.Trim())
            {
                wd.location = newLocalPosition;
                Debug.Log("Set new waypoint for " + wd.waypointID.Trim() + " to " + wd.location);
            }
        }
        waypointData = _waypointData;

        SetupDictionary();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void CheckWaypointConnections()
    {
        SetupDictionary();
        foreach (KeyValuePair<string, WaypointData> kvp in waypointLookupTable)
        {
            for (int i = 1; i < kvp.Value.neighborIDs.Length; ++i)
            {
                string neighborID = kvp.Value.neighborIDs[i];
                if (!string.IsNullOrEmpty(neighborID))
                {
                    if (waypointLookupTable[neighborID].neighborIDs[Waypoint.GetOppositeDirection(i)] != kvp.Key)
                    {
                        // Fix it!
                        waypointLookupTable[neighborID].neighborIDs[Waypoint.GetOppositeDirection(i)] = kvp.Key;
                    }
                }
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
}
