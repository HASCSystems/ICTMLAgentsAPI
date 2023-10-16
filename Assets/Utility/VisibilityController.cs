using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Checker for the visibility of an object via raycasts.
/// </summary>
public class VisibilityController
{
    private static readonly bool IS_DEBUG = false;
    public static readonly float agentHeight_standing = 1.5f, agentHeight_crawl = 0.2f;
    public static float numVisibilityChecks = 6f;

    public static bool IsObjectOccluded(Vector3 positionA, Vector3 positionB, bool addOnStandingHeight = false, string lbl = "")
    {
        RaycastHit[] hits;
        Vector3 posA = positionA + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 posB = positionB + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 dir = posB - posA;
        //firstHit = default;
        //Debug.DrawRay(posA, dir, Color.magenta, 0.01f);
        //Debug.DrawRay(positionA, positionB-positionA, Color.blue, 5f);
        hits = Physics.RaycastAll(posA, dir.normalized, dir.magnitude);
        if (IS_DEBUG) Debug.Log("A) IsObjectOccluded. #hits=" + hits.Length + "; lbl=" + lbl);
        if (hits.Length > 0) //if no object was found there is no minimum
        {
            if (IS_DEBUG) Debug.Log("B) IsObjectOccluded. first hit=" + hits[0].transform.name + "; " + hits[0].transform.parent.parent.name + "; hits[0].transform.position == posA ?" + (hits[0].transform.position == posA) + "; " + hits[0].transform.position + "; " + posA + "; lbl=" + lbl);
            if (!(hits.Length == 1 && hits[0].transform.position == posA)) //if we found only 1 and that is the source object there is also no minimum. This can be written in a simplified version but this is more understandable i think.
            {
                float min = hits[0].distance; //lets assume that the minimum is at the 0th place
                int minIndex = 0; //store the index of the minimum because thats hoow we can find our object
                for (int i = 1; i < hits.Length; ++i)// iterate from the 1st element to the last.(Note that we ignore the 0th element)
                {
                    //Debug.Log("hit: " + hits[i].transform.gameObject.name + "; lbl=" + lbl);
                    //Debug.Log("hit grandparent: " + hits[i].transform.parent.parent.gameObject.name + "; lbl=" + lbl);
                    if (hits[i].transform.position != posA && hits[i].distance < min) //if we found smaller distance and its not the player we got a new minimum
                    {
                        min = hits[i].distance; //refresh the minimum distance value
                        minIndex = i; //refresh the distance
                    }
                }
                return true;
            }
        }
        return false;
    }

    public static bool IsObjectOccluded(GameObject gameObjectA, GameObject gameObjectB, bool addOnStandingHeight = false, string lbl = "")
    {
        Vector3 positionA = gameObjectA.transform.position;
        Vector3 positionB = gameObjectB.transform.position;

        RaycastHit[] hits;
        Vector3 posA = positionA + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 posB = positionB + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 dir = posB - posA;
        //firstHit = default;
        hits = Physics.RaycastAll(posA, dir.normalized, dir.magnitude);
        //Debug.Log("A) IsObjectOccluded. #hits=" + hits.Length);
        if (hits.Length > 0) //if no object was found there is no minimum
        {
            //Debug.Log("B) IsObjectOccluded. first hit=" + hits[0].transform.name);// + "; " + hits[0].transform.parent.parent.name + "; hits[0].transform.position == posA ?" + (hits[0].transform.position == posA) + "; " + hits[0].transform.position + "; " + posA);
            if (
                !(
                    (
                        (hits.Length == 1) &&
                        (
                            (hits[0].transform.position == posA) ||
                            (hits[0].transform.gameObject == gameObjectB.transform.parent.gameObject) ||
                            (hits[0].transform.gameObject == gameObjectB.transform.parent.parent.gameObject) ||
                            ((hits[0].transform.parent != null) && (hits[0].transform.parent.gameObject == gameObjectB.transform.parent.parent.gameObject))
                        )
                    )
                    ||
                    (
                        (hits.Length == 2) &&
                        (
                            (hits[0].transform.position == posA) ||
                            (hits[0].transform.gameObject == gameObjectB.transform.parent.gameObject) ||
                            (hits[0].transform.gameObject == gameObjectB.transform.parent.parent.gameObject) ||
                            ((hits[0].transform.parent != null) && (hits[0].transform.parent.gameObject == gameObjectB.transform.parent.parent.gameObject))
                        ) &&
                        (
                            (hits[1].transform.position == posA) ||
                            (hits[1].transform.gameObject == gameObjectB.transform.parent.gameObject) ||
                            (hits[1].transform.gameObject == gameObjectB.transform.parent.parent.gameObject) ||
                            ((hits[1].transform.parent != null) && (hits[1].transform.parent.gameObject == gameObjectB.transform.parent.parent.gameObject))
                        )
                    )
                 )
               ) //if we found only 1 and that is the source object there is also no minimum. This can be written in a simplified version but this is more understandable i think.
            {
                float min = hits[0].distance; //lets assume that the minimum is at the 0th place
                int minIndex = 0; //store the index of the minimum because thats hoow we can find our object
                for (int i = 1; i < hits.Length; ++i)// iterate from the 1st element to the last.(Note that we ignore the 0th element)
                {
                    //Debug.Log("hit: " + hits[i].transform.gameObject.name);
                    //Debug.Log("hit grandparent: " + hits[i].transform.parent.parent.gameObject.name);
                    if (hits[i].transform.position != posA && hits[i].distance < min) //if we found smaller distance and its not the player we got a new minimum
                    {
                        min = hits[i].distance; //refresh the minimum distance value
                        minIndex = i; //refresh the distance
                    }
                }
                return true;
            }
        }
        return false;
    }

    public static bool IsObjectOccludedOrNotFound(
        GameObject gameObjectA,
        GameObject gameObjectB,
        bool addOnStandingHeight = false,
        string lbl = "",
        bool showDebugLines = false)
    {
#if !UNITY_EDITOR
        showDebugLines = false;
#endif

        Vector3 positionA = gameObjectA.transform.position;
        Vector3 positionB = gameObjectB.transform.position;

        RaycastHit[] hits;
        Vector3 posA = positionA + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 posB = positionB + (addOnStandingHeight ? new Vector3(0f, agentHeight_standing, 0f) : Vector3.zero);
        Vector3 dir = (posB - posA)*1f;
        //firstHit = default;
        Vector3 startPos = posA;// + dir.normalized * 2f;
        hits = Physics.RaycastAll(startPos, dir.normalized, dir.magnitude);
        // Sort by distance
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        if (IS_DEBUG) Debug.Log("* IsAgentHit: " + gameObjectA + ">" + GetEncapsulatingAgentForSubObject(gameObjectA) + "->" + gameObjectB + ">" + GetEncapsulatingAgentForSubObject(gameObjectB) + ": " + IsAgentHit(gameObjectA, gameObjectB, hits)
            );

        if (IS_DEBUG) Debug.Log("A) IsObjectOccluded. #hits=" + hits.Length + "; gameObjectA=" + gameObjectA + "/Agent=" + GetEncapsulatingAgentForSubObject(gameObjectA) + "; gameObjectB=" + gameObjectB + "/Agent=" + GetEncapsulatingAgentForSubObject(gameObjectB));
        if (hits.Length > 0) //if no object was found there is no minimum
        {
            string hitsAndGPs = "";
            for (int i = 0; i < hits.Length; ++i)
            {
                hitsAndGPs += "\n" + i + ". " + hits[i].transform.name + "|Agent=" + GetEncapsulatingAgentForSubObject(hits[i].transform.gameObject) + "|d="+ Vector3.Distance(positionA, hits[i].point) + "|" + hits[i].distance + " /";
                if (IS_DEBUG)
                {
                    // Show a colored marker where a hit took place.
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.transform.position = hits[i].point;
                    marker.transform.localScale = 0.125f * Vector3.one;
                    GameObject.Destroy(marker.GetComponent<Collider>());
                    marker.name = gameObjectA.name + "->" + gameObjectB.name + "_marker";
                    marker.GetComponent<Renderer>().material.color =
                        (i == 0 ? Color.red : (i == 1 ? Color.green : (i == 2 ? Color.blue : Color.magenta)));
                }
            }
            if (IS_DEBUG) Debug.Log(
                "B) IsObjectOccluded. first hit=" + hits[0].transform.name + 
                "; gameObjectA=" + gameObjectA + "/Agent=" + GetEncapsulatingAgentForSubObject(gameObjectA) + 
                "; gameObjectB=" + gameObjectB + "/Agent=" + GetEncapsulatingAgentForSubObject(gameObjectB) + 
                "; hitsAndGPs=" + hitsAndGPs);
            if (IsAgentHit(gameObjectA, gameObjectB, hits))
            {
                if (IS_DEBUG) Debug.Log("C) IsObjectOccluded. NO; gameObjectA=" + gameObjectA + "; gameObjectB=" + gameObjectB);
                if (showDebugLines)
                {
                    //Debug.Log("Seen: dmg= 5?");
                    //Debug.DrawLine(posA, posB, Color.blue, 1f);
                    Debug.DrawRay(startPos, dir, Color.blue, 2f);
                }
                return false;
            }
        }
        if (IS_DEBUG) Debug.Log("D) IsObjectOccluded. YES; gameObjectA=" + gameObjectA + "; gameObjectB=" + gameObjectB);
        if (showDebugLines)
        {
            //Debug.Log("Seen: dmg= 0?");
            //Debug.DrawLine(posA, posB, Color.magenta, 5f);
            Debug.DrawRay(startPos, dir, Color.magenta, 2f);
        }
        return true;
    }

    /// <summary>
    /// Returns an array of body parts visible
    /// </summary>
    /// <param name="source">Source transform</param>
    /// <param name="target">Target transform</param>
    /// <param name="sourceIsStanding">Is Source Standing?</param>
    /// <param name="targetIsStanding">Is Target Standing?</param>
    /// <param name="showDebugLines">Show Debug Lines?</param>
    /// <returns>An array of six ints that are 1 if hit and 0 otherwise. They are in the order of HH, HT, HL, TH, TT, TL where H=Head, T=Torso, L=Legs</returns>
    public static int[] GetArrayOfBodyPointsVisible(Transform source, Transform target, bool sourceIsStanding, bool targetIsStanding, bool showDebugLines = false)
    {
        WaypointVisibilityController wvc_source = source.GetComponentInChildren<WaypointVisibilityController>();
        if (wvc_source == null) wvc_source = source.parent.GetComponent<WaypointVisibilityController>();
        WaypointVisibilityController wvc_target = target.GetComponentInChildren<WaypointVisibilityController>();
        if (wvc_target == null) wvc_target = target.parent.GetComponent<WaypointVisibilityController>();

        wvc_source.SetCrouchingFlag(!sourceIsStanding);
        wvc_target.SetCrouchingFlag(!targetIsStanding);

        /*
        if (sourceIsStanding)
        {
            wvc_source.SetStanding();
        }
        else
        {
            //wvc_source.SetProne(target);
        }

        if (targetIsStanding)
        {
            wvc_target.SetStanding();
        }
        else
        {
            //wvc_target.SetProne(source);
        }*/

        if (IS_DEBUG) Debug.Log("---HH---");
        bool isOccluded_HH = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.HEAD, showDebugLines);
        if (IS_DEBUG) Debug.Log("---HT---");
        bool isOccluded_HT = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.TORSO, showDebugLines);
        if (IS_DEBUG) Debug.Log("---HL---");
        bool isOccluded_HL = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.LEGS, showDebugLines);
        if (IS_DEBUG) Debug.Log("---TH---");
        bool isOccluded_TH = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.HEAD, showDebugLines);
        if (IS_DEBUG) Debug.Log("---TT---");
        bool isOccluded_TT = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.TORSO, showDebugLines);
        if (IS_DEBUG) Debug.Log("---TL---");
        bool isOccluded_TL = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.LEGS, showDebugLines);


        return
            new int[] {
            (isOccluded_HH ? 0 : 1),
            (isOccluded_HT ? 0 : 1),
            (isOccluded_HL ? 0 : 1),
            (isOccluded_TH ? 0 : 1),
            (isOccluded_TT ? 0 : 1),
            (isOccluded_TL ? 0 : 1)
            };
    }

    public static IEnumerator _GetArrayOfBodyPointsVisible(System.Action<int[]> callback, Transform source, Transform target, bool sourceIsStanding, bool targetIsStanding, bool showDebugLines = false)
    {
        WaypointVisibilityController wvc_source = source.GetComponentInChildren<WaypointVisibilityController>();
        if (wvc_source == null) wvc_source = source.parent.GetComponent<WaypointVisibilityController>();
        WaypointVisibilityController wvc_target = target.GetComponentInChildren<WaypointVisibilityController>();
        if (wvc_target == null) wvc_target = target.parent.GetComponent<WaypointVisibilityController>();

        if (sourceIsStanding)
        {
            wvc_source.SetStanding();
        }
        else
        {
            wvc_source.SetProne(target);
        }

        if (targetIsStanding)
        {
            wvc_target.SetStanding();
        }
        else
        {
            wvc_target.SetProne(source);
        }

        bool isOccluded_HH = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.HEAD, showDebugLines);
        bool isOccluded_HT = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.TORSO, showDebugLines);
        bool isOccluded_HL = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.HEAD, wvc_target, WaypointVisibilityController.BODYPART.LEGS, showDebugLines);
        bool isOccluded_TH = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.HEAD, showDebugLines);
        bool isOccluded_TT = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.TORSO, showDebugLines);
        bool isOccluded_TL = CheckBodyPartOcclusion(wvc_source, WaypointVisibilityController.BODYPART.TORSO, wvc_target, WaypointVisibilityController.BODYPART.LEGS, showDebugLines);


        if (callback != null)
        {
            callback(
            new int[] {
                (isOccluded_HH ? 0 : 1),
                (isOccluded_HT ? 0 : 1),
                (isOccluded_HL ? 0 : 1),
                (isOccluded_TH ? 0 : 1),
                (isOccluded_TT ? 0 : 1),
                (isOccluded_TL ? 0 : 1)
            });
        }

        yield return new WaitForFixedUpdate();
    }

    public static float GetPercentageOfBodyPointsVisible(Transform source, Transform target, bool sourceIsStanding, bool targetIsStanding, bool showDebugLines = false)
    {
        int[] hits = GetArrayOfBodyPointsVisible(source, target, sourceIsStanding, targetIsStanding, showDebugLines);
        return GetPercentageOfBodyPointsVisible(hits);
    }

    public static float GetPercentageOfBodyPointsVisible(int[] hits)
    {
        int sum = 0;
        foreach (int hit in hits)
        {
            sum += hit;
        }
        return (float)sum / (float)numVisibilityChecks;
    }

    public static bool CheckBodyPartOcclusion(WaypointVisibilityController wvc_source, WaypointVisibilityController.BODYPART sourceBodyPart,
                                          WaypointVisibilityController wvc_target, WaypointVisibilityController.BODYPART targetBodyPart,
                                          bool showDebugLines = false)
    {
        // 2023-04-19_RK: Use && instead of || ?
        bool isOccluded = IsObjectOccludedOrNotFound(wvc_source.GetBodyParts()[(int)sourceBodyPart].gameObject,
                                                wvc_target.GetBodyParts()[(int)targetBodyPart].gameObject, false, "", showDebugLines)
                             ||
                             IsObjectOccludedOrNotFound(wvc_target.GetBodyParts()[(int)targetBodyPart].gameObject,
                                                wvc_source.GetBodyParts()[(int)sourceBodyPart].gameObject, false, "", showDebugLines);

        if (showDebugLines)
        {
            Debug.DrawRay(wvc_source.GetBodyParts()[(int)sourceBodyPart].position,
                (wvc_target.GetBodyParts()[(int)targetBodyPart].position - wvc_source.GetBodyParts()[(int)sourceBodyPart].position), isOccluded ? Color.magenta : Color.blue, 5f);
        }
        return isOccluded;
    }

    public static bool DoesAnyParentHaveTag(Transform t, string tag)
    {
        if (t.CompareTag(tag))
        {
            return true;
        }
        else if (t == t.root)
        {
            return false;
        }
        else
        {
            return DoesAnyParentHaveTag(t.parent, tag);
        }
    }

    /// <summary>
    /// Determine the agent, as defined by object with ScoutAgent component, that contains this agent's sub object
    /// </summary>
    /// <param name="subObject">the object to check</param>
    /// <returns>The agent that encapsulates this subObject or null if it does not belong to an agent</returns>
    public static GameObject GetEncapsulatingAgentForSubObject(GameObject subObject)
    {
        // We've gone too far
        if (subObject.transform == subObject.transform.root)
        {
            return null;
        }
        // The parent is an agent - return it
        else if (subObject.transform.parent.GetComponent<ScoutAgent>() != null)
        {
            return subObject.transform.parent.gameObject;
        }
        // This the agent - return it
        else if (subObject.GetComponent<ScoutAgent>() != null)
        {
            return subObject;
        }
        else // Keep searching
        {
            return GetEncapsulatingAgentForSubObject(subObject.transform.parent.gameObject);
        }
    }

    /// <summary>
    /// Check if Agent-subObject to Agent-subObject line-of-sight goes directly from agent to agent.
    /// It can hit subObjects of the source agent prior to hitting subObjects of targetAgent
    /// </summary>
    /// <param name="gameObjectA">Source agent subObject</param>
    /// <param name="gameObjectB">Target agent subObject</param>
    /// <param name="distanceSortedHits">list of RaycastHit's pre-sorted by distance, with closest at index 0</param>
    /// <returns>true if there's a chain of source agent's objects and then immediately any object belonging to the target agent</returns>
    public static bool IsAgentHit(GameObject gameObjectA, GameObject gameObjectB, RaycastHit[] distanceSortedHits)
    {
        // Parameter check
        if (distanceSortedHits.Length <= 0 || gameObjectA == null || gameObjectB == null)
            return false;

        GameObject sourceAgent = GetEncapsulatingAgentForSubObject(gameObjectA);
        GameObject targetAgent = GetEncapsulatingAgentForSubObject(gameObjectB);

        // Agent check
        if ((sourceAgent == null) || (targetAgent == null))
            return false;

        for (int i=0; i<distanceSortedHits.Length; ++i)
        {
            GameObject hitAgent = GetEncapsulatingAgentForSubObject(distanceSortedHits[i].transform.gameObject);

            if (hitAgent == sourceAgent)
            {
                // This is acceptable -- the raycast hit one of agent's subObjects
                continue;
            }
            else if (hitAgent == targetAgent)
            {
                // Successful hit!
                return true;
            }
            else // we hit something else
            {
                return false;
            }
        }
        // failsafe
        return false;
    }
}
