using System.Collections;
using System.Collections.Generic;

public class Dijkstra
{
    public Dictionary<string, WaypointData> PreviousNodes { get; } = new Dictionary<string, WaypointData>();
    public Dictionary<string, int> Distances { get; } = new Dictionary<string, int>();
    public PriorityQueue<string, int> Queue { get; } = new PriorityQueue<string, int>();

    public WaypointData Start { get; set; }
    public WaypointData Target { get; set; }

    public List<string> shortestPath = new List<string>();
    protected WaypointMeshData waypointMeshData;
    private bool saveShortestPath = true;

    public Dijkstra(WaypointData start, WaypointData target, WaypointMeshData waypointMeshData, bool saveShortestPath = true)
    {
        Start = start;
        Target = target;
        this.waypointMeshData = waypointMeshData;
        this.saveShortestPath = saveShortestPath;

        Run();
    }

    public int GetDistance()
    {
        if (!Distances.ContainsKey(Target.waypointID))
        {
            return -1;
        }
        else
            return Distances[Target.waypointID];
    }

    public int GetPartialDistance(WaypointData tgtWV)
    {
        if (Distances.ContainsKey(tgtWV.waypointID))
        {
            return Distances[tgtWV.waypointID];
        }
        else
        {
            Target = tgtWV;
            Run();
            return GetDistance();
        }
    }

    private void Run()
    {
        Queue.Enqueue(Start.waypointID, 0);
        Distances[Start.waypointID] = 0;

        while (Queue.Count != 0)
        {
            var node = Queue.Dequeue();

            if (node == Target.waypointID)
            {
                if (saveShortestPath)
                {
                    // Construct shortest path
                    while (PreviousNodes.ContainsKey(node))
                    {
                        shortestPath.Insert(0, node);
                        node = PreviousNodes[node].waypointID;
                    }
                    shortestPath.Insert(0, node);
                }
                return;
            }

            WaypointData nodeWaypointData = waypointMeshData.waypointLookupTable[node];
            foreach (string neighbor in nodeWaypointData.neighborIDs)
            {
                if (!string.IsNullOrEmpty(neighbor))
                {
                    var newDistance = Distances[node] + 1;// edge.Cost;

                    if (!Distances.ContainsKey(neighbor) || newDistance < Distances[neighbor])
                    {
                        Distances[neighbor] = newDistance;
                        PreviousNodes[neighbor] = nodeWaypointData;
                        Queue.Enqueue(neighbor, newDistance);
                    }
                }
            }
        }
    }
}
