using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to manipulate connections and locations of waypoints and save to WaypointMeshData ScriptableObject.
/// </summary>
[ExecuteInEditMode]
public class WaypointMeshManipulator : EditorWindow
{
    WaypointMeshData waypointMeshData;
    //WaypointData newWaypoint;
    GameObject waypointA, waypointB;
    string errorMessage;
    static GUIStyle errorStyle = new GUIStyle();

    GameObject newWaypointObject, newN, newNE, newE, newSE, newS, newSW, newW, newNW;
    bool newIsDecisionPoint;

    [MenuItem("Window/Waypoint Connection Editor")]
    static void Init()
    {
        WaypointMeshManipulator window = (WaypointMeshManipulator)EditorWindow.GetWindow(typeof(WaypointMeshManipulator));
        window.Show();

        errorStyle.normal.textColor = Color.red;
    }

    private void OnGUI()
    {
        if (errorStyle.normal.textColor != Color.red)
            errorStyle.normal.textColor = Color.red;

        EditorGUILayout.LabelField("Make a copy of this before including here, if it's not empty");
        waypointMeshData = EditorGUILayout.ObjectField("Waypoint Mesh Data to Edit", waypointMeshData, typeof(WaypointMeshData), true) as WaypointMeshData;
        //newWaypoint = EditorGUILayout.ObjectField("New Waypoint To Add", newWaypoint, typeof(WaypointData), true) as WaypointData;
        newWaypointObject = EditorGUILayout.ObjectField("New Waypoint - pos, name", newWaypointObject, typeof(GameObject), true) as GameObject;
        newN = EditorGUILayout.ObjectField("New Waypoint - N neighbor", newN, typeof(GameObject), true) as GameObject;
        newNE = EditorGUILayout.ObjectField("New Waypoint - NE neighbor", newNE, typeof(GameObject), true) as GameObject;
        newE = EditorGUILayout.ObjectField("New Waypoint - E neighbor", newE, typeof(GameObject), true) as GameObject;
        newSE = EditorGUILayout.ObjectField("New Waypoint - SE neighbor", newSE, typeof(GameObject), true) as GameObject;
        newS = EditorGUILayout.ObjectField("New Waypoint - S neighbor", newS, typeof(GameObject), true) as GameObject;
        newSW = EditorGUILayout.ObjectField("New Waypoint - SW neighbor", newSW, typeof(GameObject), true) as GameObject;
        newW = EditorGUILayout.ObjectField("New Waypoint - W neighbor", newW, typeof(GameObject), true) as GameObject;
        newNW = EditorGUILayout.ObjectField("New Waypoint - NW neighbor", newNW, typeof(GameObject), true) as GameObject;
        newIsDecisionPoint = EditorGUILayout.Toggle("New Waypoint - isDecisionPoint", newIsDecisionPoint);

        if (GUILayout.Button("Setup Waypoints"))
        {
            if (waypointMeshData != null)
                waypointMeshData.SetupDictionary();
            else
                errorMessage = "WaypointMeshData is null";
        }

        if (GUILayout.Button("Clean Waypoints"))
        {
            if (waypointMeshData != null)
                waypointMeshData.CleanWaypointData();
            else
                errorMessage = "WaypointMeshData is null";
        }

        if (GUILayout.Button("Draw Debug Lines"))
        {
            DrawDebugLines();
        }

        if (GUILayout.Button("Clear Waypoint Selections"))
        {
            waypointA = null;
            waypointB = null;

            newWaypointObject = null;
            newN  = null;
            newNE = null;
            newE  = null;
            newSE = null;
            newS  = null;
            newSW = null;
            newW  = null;
            newNW = null;
            newIsDecisionPoint = false;
        }
        GUILayout.Label("Waypoint A: " + (waypointA == null ? "<none>" : waypointA.name));
        if (GUILayout.Button("Select Waypoint A"))
        {
            GameObject go = GetSelectedObject();
            if (go != null)
                waypointA = go;
        }
        GUILayout.Label("Waypoint B: " + (waypointB == null ? "<none>" : waypointB.name));
        if (GUILayout.Button("Select Waypoint B"))
        {
            GameObject go = GetSelectedObject();
            if (go != null)
                waypointB = go;
        }

        if (GUILayout.Button("Add New Waypoint to mesh data"))
        {
            WaypointData newWaypoint = CreateWaypointData(
                newWaypointObject,
                newN,newNE,newE,newSE,newS,newSW,newW,newNW,
                newIsDecisionPoint
                );
            // Check if already in waypoint mesh data
            if (!waypointMeshData.waypointLookupTable.ContainsKey(newWaypoint.waypointID))
            {
                waypointMeshData.AddWaypoint(newWaypoint);
            }
            else
            {
                errorMessage = "Waypoint A (" + waypointA.name + ") already in waypointMeshData";
            }
        }

        if (GUILayout.Button("Set new position for Waypoint A"))
        {
            waypointMeshData.SetupDictionary();
            if (waypointA != null)
            {
                if (waypointMeshData.waypointLookupTable.ContainsKey(waypointA.name.Trim()))
                {
                    waypointMeshData.SetNewLocationOfWaypoint(waypointA.name.Trim(), waypointA.transform.localPosition);
                }
                else
                {
                    errorMessage = "Waypoint A (" + waypointA.name + ") not in waypointMeshData. #items=" + waypointMeshData.waypointLookupTable.Count;
                }
            }
            else
            {
                errorMessage = "Waypoint A is not set.";
            }
        }

        GUILayout.Space(20f);
        GUILayout.Label(string.IsNullOrEmpty(errorMessage) ? "" : "Error: " + errorMessage, errorStyle);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            if (GUILayout.Button("Clear Error message"))
            {
                errorMessage = string.Empty;
            }
        }

        GUILayout.Space(20f);
        if (GUILayout.Button("Test"))
        {
            errorMessage = "Error test";
        }

    }

    private WaypointData CreateWaypointData(GameObject nameAndPosGO, 
        GameObject N_GO,
        GameObject NE_GO,
        GameObject E_GO,
        GameObject SE_GO,
        GameObject S_GO,
        GameObject SW_GO,
        GameObject W_GO,
        GameObject NW_GO,
        bool isDecsnPt
        )
    {
        WaypointData wd = new WaypointData();
        wd.waypointID = nameAndPosGO.name;
        wd.location = nameAndPosGO.transform.position;
        wd.neighborIDs = new string[]
        {
            nameAndPosGO.name,
            N_GO == null ? "" : N_GO.name,
            NE_GO == null ? "" : NE_GO.name,
            E_GO == null ? "" : E_GO.name,
            SE_GO == null ? "" : SE_GO.name,
            S_GO == null ? "" : S_GO.name,
            SW_GO == null ? "" : SW_GO.name,
            W_GO == null ? "" : W_GO.name,
            NW_GO == null ? "" : NW_GO.name
        };
        return wd;
    }

    static GameObject GetSelectedObject()
    {
        return Selection.activeGameObject;
    }

    public void DrawDebugLines()
    {
        waypointMeshData.SetupDictionary();
        foreach (KeyValuePair<string, WaypointData> kvp in waypointMeshData.waypointLookupTable)
        {
            for (int i = 1; i < kvp.Value.neighborIDs.Length; ++i)
            {
                if (!string.IsNullOrEmpty(kvp.Value.neighborIDs[i]))
                {
                    Debug.DrawLine(
                        kvp.Value.location,
                        waypointMeshData.waypointLookupTable[kvp.Value.neighborIDs[i]].location, WaypointData.connColors[i], 1e6f
                        );
                }
            }
        }
    }

    public void OnDrawGizmos()
    {
        DrawDebugLines();
    }
}
