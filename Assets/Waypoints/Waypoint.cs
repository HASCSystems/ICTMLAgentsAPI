using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField]
    protected Waypoint[] neighbors = new Waypoint[WaypointMeshController.NumWaypointConnections + 1]; // neighbor[0] will be self.
    public GameObject[] displayObjects;

    // Already done
    /*protected virtual void Awake()
    {
        SetNeighbor(0, this); // direction 0 is self, to correspond with movement one-hot
    }*/

    public virtual Waypoint GetNeighbor(int direction)
    {
        if (direction < 0 || direction >= neighbors.Length)
            return null;
        return neighbors[direction];
    }

    public virtual void SetNeighbor(int direction, Waypoint newNeighbor)
    {
        if (direction < 0 || direction >= neighbors.Length)
            return;
        neighbors[direction] = newNeighbor;
    }

    public virtual void SetMarkers(bool isOn)
    {
        foreach(GameObject displayObject in displayObjects)
        {
            displayObject.SetActive(isOn);
        }
    }

    public virtual void ShowMarkers(Color? markerColor = null)
    {
        foreach (GameObject displayObject in displayObjects)
        {
            displayObject.SetActive(true);
            if (markerColor != null)
            {
                Renderer r = displayObject.GetComponent<Renderer>();
                if (r != null)
                    r.material.color = markerColor.Value;
            }
        }
    }

    public static int GetOppositeDirection(int currentDir)
    {
        if (currentDir == 0) return 0;
        return ((currentDir-1) + WaypointMeshController.NumWaypointConnections / 2) % WaypointMeshController.NumWaypointConnections + 1;
    }

    public virtual Waypoint GetNearestWaypointToAngle(float theta)
    {
        Waypoint nearestWaypoint = null;
        float nearestAngle = (float)Math.PI; //180 degrees away is max angle distance
        int bestIndex = -1; //only for debugging

        for (int i = 1; i <= WaypointMeshController.NumWaypointConnections; i++)
        {
            Waypoint neighbor = neighbors[i];
            if (neighbor == null)
            {
                // Debug.Log("neighbor " + i + " was null.");
                continue;
            }
            float deltaX = neighbor.transform.position.x - transform.position.x;
            float deltaZ = neighbor.transform.position.z - transform.position.z;

            //TODO: fix if this can return more than pi ...it's -pi to pi, so the range is 2pi
            float theta2 = Mathf.Atan2(deltaZ, deltaX); //angle from x axis
            float deltaTheta = GetDeltaTheta(theta2, theta);
            Debug.DrawRay(transform.position, ((float)Math.PI - deltaTheta) * (neighbor.transform.position - transform.position), Color.white);

            if (deltaTheta < nearestAngle)
            {
                nearestAngle = deltaTheta;
                nearestWaypoint = neighbor;
                // Debug.Log("neighbor " + i + " is now the best, as it was better than " + bestIndex);
                bestIndex = i;
            }
            else
            {
                // Debug.Log("neighbor " + i + " is worse than " + bestIndex + " because " + deltaTheta + " is larger than " + nearestAngle);
            }
        }
        return nearestWaypoint;
    }

    protected virtual float GetDeltaTheta(float theta1, float theta2)
    {
        float deltaTheta = theta2 - theta1;
        if (deltaTheta > Math.PI)
        {
            deltaTheta = (float)(2 * Math.PI - deltaTheta);
        }
        else if (deltaTheta < -Math.PI)
        {
            deltaTheta = (float)(-2 * Math.PI + deltaTheta);
        }
        // Debug.Log("Angles: " + theta2 + " - " + theta1 + " = " + deltaTheta);
        return Math.Abs(deltaTheta);
    }

    public virtual float[] GetAnglesOfNeighbors()
    { 
        //in order of neighbors
        float[] angles = new float[WaypointMeshController.NumWaypointConnections];

        for (int i = 0; i <= WaypointMeshController.NumWaypointConnections; i++)
        {
            if (i == 0 || neighbors[i] == null) angles[i] = -1;
            else
            {
                angles[i] = Mathf.Atan2(neighbors[i].transform.position.z - transform.position.z,
                    neighbors[i].transform.position.x - transform.position.x);
            }
        }
        return angles;
    }

    /// <summary>
    /// Gives a neighbor in a direction. 
    /// </summary>
    /// <param name="neighbor"></param>
    /// <returns>-1 if it is not a neighbor. 0 indicates self.</returns>
    public virtual int GetDirectionOfNeighbor(Waypoint neighbor)
    {
        for (int i = 0; i <= WaypointMeshController.NumWaypointConnections; ++i)
        {
            if (neighbors[i] == neighbor)
            {
                return i;
            }
        }
        return -1;
    }
}
