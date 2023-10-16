using UnityEngine;
using Unity.MLAgents;

public class MLAgentsController : MonoBehaviour
{
    public enum DECISION_MODE
    {
        FIXED_INTERVAL,
        UPON_ARRIVAL,
        CUSTOM
    }
    public DECISION_MODE decisionMode = DECISION_MODE.FIXED_INTERVAL;

    [Tooltip("This only applies to Custom DecisionMode. Only set to false only if you know what you are doing!")]
    public bool UseAutomaticStepping = true; // See: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Academy.html

    public delegate void OnBroadcastDecisionAction();
    public static event OnBroadcastDecisionAction OnBroadcastDecision;

    public float timeBetweenBroadcasts = 1f;
    protected float broadcastTimer = 0f;

    protected static bool m_IsDecisionStep = false;
    public static bool IsDecisionStep
    {
        get { return m_IsDecisionStep; }
        set { m_IsDecisionStep = value; }
    }


    protected virtual void Awake()
    {
        Academy.Instance.AutomaticSteppingEnabled = UseAutomaticStepping;
        /*
        switch (decisionMode)
        {
            case DECISION_MODE.UPON_ARRIVAL:
                Academy.Instance.AutomaticSteppingEnabled = true;
                break;
            case DECISION_MODE.FIXED_INTERVAL:
                Academy.Instance.AutomaticSteppingEnabled = true;
                break;
            case DECISION_MODE.CUSTOM:
            default:
                Academy.Instance.AutomaticSteppingEnabled = UseAutomaticStepping;
                break;
        }*/
    }

    protected virtual void FixedUpdate()
    {
        if (decisionMode == DECISION_MODE.FIXED_INTERVAL)
        {
            broadcastTimer += Time.fixedDeltaTime;
            if (broadcastTimer >= timeBetweenBroadcasts)
            {
                BroadcastDecision();
                broadcastTimer = 0f;
            }
        }
    }

    public virtual void BroadcastDecision()
    {
        OnBroadcastDecision?.Invoke();
    }
}
