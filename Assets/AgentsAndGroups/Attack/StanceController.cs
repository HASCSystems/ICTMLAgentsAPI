using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls whether an agent is upright or prone. Note that prone works best asynchronously where the agent tries in realtime
/// to see how far down they can tip until they make contact with the ground. This can be a slow process.
/// </summary>
public class StanceController : MonoBehaviour
{
    public delegate void HasStartedGoingProneDelegate(ScoutAgent agent);
    public static event HasStartedGoingProneDelegate HasStartedGoingProne = null;

    public delegate void HasFinishedGoingProneDelegate(ScoutAgent agent);
    public static event HasFinishedGoingProneDelegate HasFinishedGoingProne = null;

    public ScoutAgent agent;

    public CollisionCheckController craniumCollisionCheckController, torsoCollisionCheckController;
    public Transform UprightModel, ProneModel_NoProjectiles, ProneModel_Projectiles;

    [Tooltip("Must be in descending order")]
    public float[] eulerAngleDeltas = new float[] { 5f, 1f };

    public bool initiate = false;
    public bool test_enableRB = false, test_isRBenabled = true;
    protected bool hasFoundCollisionAngle = false, isSearchingForCollisionAngle = false;
    protected int searchPhaseIndex = 0;

    protected List<Vector3> startingBodyPartPositions = new List<Vector3>();
    protected List<Vector3> startingBodyPartEulerAngles = new List<Vector3>();

    protected virtual void Awake()
    {
        for (int i=0; i<ProneModel_NoProjectiles.childCount; ++i)
        {
            startingBodyPartPositions.Add(ProneModel_NoProjectiles.GetChild(i).localPosition);
            startingBodyPartEulerAngles.Add(ProneModel_NoProjectiles.GetChild(i).localEulerAngles);
        }
    }

    protected virtual void ResetMannequin()
    {
        for (int i = 0; i < ProneModel_NoProjectiles.childCount; ++i)
        {
            ProneModel_NoProjectiles.GetChild(i).localPosition = startingBodyPartPositions[i];
            ProneModel_NoProjectiles.GetChild(i).localEulerAngles = startingBodyPartEulerAngles[i];
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (initiate)
        {
            FindTorsoCollisionAngle();
            initiate = false;
        }

        if (test_enableRB)
        {
            //EnableRigidbodies(test_isRBenabled);
            test_enableRB = false;
        }
    }

    public Transform GetPartParent(bool isCrouching)
    {
        SetStance(!isCrouching);
        return isCrouching ? ProneModel_NoProjectiles : UprightModel;
    }

    public Transform GetPart(int index)
    {
        Transform partParent = GetActivePartParent();
        if (index < 0 || index >= partParent.childCount)
            return null;
        return partParent.GetChild(index);
    }

    public Transform GetRandomPart(int[] visPoints)
    {
        int total = 0;
        List<int> nonzeroIndices = new List<int>();
        for (int i=0; i<visPoints.Length; ++i)
        {
            if (visPoints[i] != 0)
                nonzeroIndices.Add(i);
            total += visPoints[i];
        }
        if (total <= 0) return null;

        int rIndex = nonzeroIndices[Random.Range(0, nonzeroIndices.Count)];
        return GetPart(rIndex % 3); // Because it's HH,HT,HL,TH,TT,TL = H{H,T,L},T{H,T,L}
    }


    /// <summary>
    /// Useful for aiming and knowing which body parts are active
    /// </summary>
    /// <returns>Parent of active mannequin parts</returns>
    public Transform GetActivePartParent()
    {
        if (UprightModel.gameObject.activeSelf)
        {
            return UprightModel;
        }
        else
        {
            return ProneModel_Projectiles;
        }
    }


    protected virtual void FixedUpdate()
    {
        if (isSearchingForCollisionAngle && !hasFoundCollisionAngle)
        {
            ProneModel_NoProjectiles.eulerAngles += new Vector3(eulerAngleDeltas[searchPhaseIndex], 0f, 0f);
            if (craniumCollisionCheckController.IsCollidingWithTerrain ||
                torsoCollisionCheckController.IsCollidingWithTerrain)
            {
                isSearchingForCollisionAngle = false;
                hasFoundCollisionAngle = true;
                Debug.Log("Collision Angle=" + ProneModel_NoProjectiles.eulerAngles);
                if (HasFinishedGoingProne != null)
                {
                    HasFinishedGoingProne(agent);
                }
            }
        }
    }

    public virtual void SetStance(bool isUpright)
    {
        UprightModel.gameObject.SetActive(isUpright);
        ProneModel_NoProjectiles.gameObject.SetActive(!isUpright);
        ProneModel_Projectiles.gameObject.SetActive(!isUpright);
    }

    public virtual void FindTorsoCollisionAngle()
    {
        searchPhaseIndex = 0;
        SetStance(false);
        craniumCollisionCheckController.ResetCollisionStatus();
        torsoCollisionCheckController.ResetCollisionStatus();
        ResetMannequin();
        ProneModel_NoProjectiles.eulerAngles = new Vector3(0f, ProneModel_NoProjectiles.eulerAngles.y, ProneModel_NoProjectiles.eulerAngles.z);
        hasFoundCollisionAngle = false;
        isSearchingForCollisionAngle = true;
        if (HasStartedGoingProne != null)
        {
            HasStartedGoingProne(agent);
        }
    }

    /// <summary>
    /// Agent falls asynchronously. Must be used in a coroutine.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator FindTorsoCollisionAngleAsync()
    {
        searchPhaseIndex = 0;
        SetStance(false); // Set data so this agent is now flagged as prone
        craniumCollisionCheckController.ResetCollisionStatus();
        torsoCollisionCheckController.ResetCollisionStatus(); // Start them upright
        ResetMannequin();
        //ProneModel_NoProjectiles.localEulerAngles = new Vector3(0f, ProneModel_NoProjectiles.localEulerAngles.y, ProneModel_NoProjectiles.localEulerAngles.z); // Reset x-value on the projectile-transparent mannequin
        ProneModel_NoProjectiles.localEulerAngles = Vector3.zero;
        float timeLimit = 5f; // use a timer in case it gets stuck
        for (int i = 0; i < eulerAngleDeltas.Length; ++i)
        {
            float timer = 0f;
            while (
                !craniumCollisionCheckController.IsCollidingWithTerrain && // If cranium is not colliding
                !torsoCollisionCheckController.IsCollidingWithTerrain && // and if torso is not colliding
                                                                         //(ProneModel_NoProjectiles.eulerAngles.x < 90f) && // And torso doesn't bend more than 90 degrees (controversial)
                (timer < timeLimit)
                )
            {
                ProneModel_NoProjectiles.localEulerAngles += new Vector3(eulerAngleDeltas[i], 0f, 0f);
                //Debug.Log("Prone eulerAngles=" + ProneModel_NoProjectiles.eulerAngles);
                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (i < eulerAngleDeltas.Length-1)
            {
                ProneModel_NoProjectiles.localEulerAngles -= new Vector3(eulerAngleDeltas[i], 0f, 0f); // Send it back one "try"
            }
        }
    }
}
