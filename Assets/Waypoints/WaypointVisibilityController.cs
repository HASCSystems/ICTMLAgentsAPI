using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointVisibilityController : MonoBehaviour
{
    public Transform partParent;
    private Transform[] bodyParts = null;
    public StanceController stanceController;

    private bool useCrouch = true;
    private bool isCrouching = false;

    public enum BODYPART
    {
        HEAD = 0,
        TORSO = 1,
        LEGS = 2
    }
    public static Dictionary<BODYPART, string> bodyPartSymbol = new Dictionary<BODYPART, string>()
    {
        {BODYPART.HEAD, "H" },
        {BODYPART.TORSO, "T" },
        {BODYPART.LEGS, "L" }
    };

    protected virtual void Awake()
    {
        GetBodyParts();
    }

    [Header("Testing")]
    public bool testCrouching = false;
    public bool testUpright = false;

    protected virtual void Update()
    {
        if (testCrouching)
        {
            SetCrouching();
            testCrouching = false;
        }
        if (testUpright)
        {
            SetUpright();
            testUpright = false;
        }
    }

    protected virtual Transform GetPartParent()
    {
        if (stanceController == null)
        {
            return partParent;
        }
        else
        {
            return stanceController.GetPartParent(isCrouching);
        }
    }

    public virtual void ActivateBodyParts()
    {
        foreach (Transform bodyPart in bodyParts)
        {
            bodyPart.gameObject.SetActive(true);
        }
    }

    public virtual Transform[] GetBodyParts()
    {
        bodyParts = new Transform[3] { GetPartParent().GetChild(0), GetPartParent().GetChild(1), GetPartParent().GetChild(2) };
        return bodyParts;
    }

    public virtual void SetStanding()
    {
        if (useCrouch)
        {
            SetUpright();
        }
        else
        {
            GetPartParent().localEulerAngles = Vector3.zero;
        }
    }

    public virtual void SetProne(Transform target)
    {
        if (useCrouch)
        {
            /*GetPartParent().LookAt(target);
            GetPartParent().localEulerAngles += new Vector3(0f, 90f, 0f);
            GetPartParent().localEulerAngles = new Vector3(0f, GetPartParent().localEulerAngles.y, GetPartParent().localEulerAngles.z);*/
            GetPartParent().localEulerAngles = Vector3.zero;
            GetPartParent().LookAt(target);
            GetPartParent().localEulerAngles = new Vector3(0f, GetPartParent().localEulerAngles.y, 0f);
            SetCrouching();
        }
        else
        {
            GetPartParent().LookAt(target);
            GetPartParent().localEulerAngles = new Vector3(90f, GetPartParent().localEulerAngles.y, 0f);
        }
    }

    /// <summary>
    /// Set agent to prone asynchronously so it can be called within a coroutine.
    /// </summary>
    /// <param name="target">Agent to look at so can fall toward it and face it</param>
    /// <returns></returns>
    public virtual IEnumerator SetProneAsync(Transform target)
    {
        GetPartParent().parent.localEulerAngles = Vector3.zero;
        GetPartParent().parent.LookAt(target); // Look at target
        GetPartParent().parent.localEulerAngles = new Vector3(0f, GetPartParent().parent.localEulerAngles.y, 0f); // but only keep the y-rotation, set x and z to zero
        if (stanceController != null)
            yield return stanceController.FindTorsoCollisionAngleAsync();
        else
            yield return null;
    }

    public virtual void SetCrouchingFlag(bool isSet)
    {
        isCrouching = isSet;
    }

    protected virtual void SetCrouching()
    {
        isCrouching = true;
        if (stanceController != null)
        {
            //stanceController.UprightModel.gameObject.SetActive(false);
            //stanceController.ProneModel_NoProjectiles.gameObject.SetActive(true);
            //stanceController.ProneModel_Projectiles.gameObject.SetActive(true);
            //GetPartParent().localEulerAngles = Vector3.zero;
            stanceController.FindTorsoCollisionAngle();
        }
        else
        {
            bodyParts[0].localPosition = new Vector3(-0.61f, 1.277f, 0f);
            bodyParts[0].localEulerAngles = new Vector3(0f, 0f, 0f);
            bodyParts[0].localScale = new Vector3(0.3f, 0.3f, 0.3f);
            bodyParts[1].localPosition = new Vector3(-0.244f, 0.961f, 0f);
            bodyParts[1].localEulerAngles = new Vector3(-6.305f, 9.69f, 44.164f);
            bodyParts[1].localScale = new Vector3(0.5f, 0.417f, 0.5f);
            bodyParts[2].localPosition = new Vector3(0f, 0.355f, 0f);
            bodyParts[2].localEulerAngles = new Vector3(0f, 0f, 0f);
            bodyParts[2].localScale = new Vector3(0.367f, 0.417f, 0.367f);
        }
        
    }

    protected virtual void SetUpright()
    {
        isCrouching = false;
        if (GetPartParent().GetComponent<StanceController>() != null)
        {
            //stanceController.UprightModel.gameObject.SetActive(true);
            //stanceController.ProneModel_NoProjectiles.gameObject.SetActive(false);
            //stanceController.ProneModel_Projectiles.gameObject.SetActive(false);
            //GetPartParent().localEulerAngles = Vector3.zero;
        }
        else
        {
            bodyParts[0].localPosition = new Vector3(0f, 1.66f, 0f);
            bodyParts[0].localEulerAngles = new Vector3(0f, 0f, 0f);
            bodyParts[0].localScale = new Vector3(0.3f, 0.3f, 0.3f);
            bodyParts[1].localPosition = new Vector3(0f, 1.122f, 0f);
            bodyParts[1].localEulerAngles = new Vector3(0f, 0f, 0f);
            bodyParts[1].localScale = new Vector3(0.5f, 0.417f, 0.5f);
            bodyParts[2].localPosition = new Vector3(0f, 0.355f, 0f);
            bodyParts[2].localEulerAngles = new Vector3(0f, 0f, 0f);
            bodyParts[2].localScale = new Vector3(0.367f, 0.417f, 0.367f);
        }
    }

    public virtual List<GameObject> GetActiveMannequinParts()
    {
        List<GameObject> foundParts = new List<GameObject>();
        if (stanceController != null)
        {
            foreach(Transform child in stanceController.transform)
            {
                if ((child.gameObject.activeInHierarchy) &&
                    (!child.gameObject.CompareTag("noprojectile"))
                    )
                {
                    foreach(Transform gchild in child)
                    {
                        foundParts.Add(gchild.gameObject);
                    }
                }
            }
        }

        return foundParts;
    }
}
