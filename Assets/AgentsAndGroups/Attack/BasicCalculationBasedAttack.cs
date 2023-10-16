using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCalculationBasedAttack : RangedAttack
{
    public ScoutAgent agent;

    public bool AllowKeyboardInput = true; //this mode ignores player input
    public bool initialized; //has this robot been initialized
    public KeyCode shootKey = KeyCode.J;

    [Header("SOUND")] public bool PlaySound;
    public ForceMode forceMode;
    private AudioSource m_AudioSource;

    //SHOOTING RATE
    [Header("SHOOTING RATE")]
    public float shootingRate = .02f; //can shoot every shootingRate seconds. ex: .5 can shoot every .5 seconds
    public float coolDownTimer;
    public bool coolDownWait;

    [Header("ACCURACY")]
    public Transform transformToAimAt;

    [Header("DAMAGE")]
    public float damage = 10f;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    protected virtual void Initialize()
    {
        m_AudioSource = GetComponent<AudioSource>();

        initialized = true;
    }

    protected virtual void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    protected virtual void FixedUpdate()
    {
        coolDownWait = coolDownTimer > shootingRate ? false : true;
        coolDownTimer += Time.fixedDeltaTime;

#if UNITY_EDITOR
        if (Input.GetKeyDown(shootKey))
        {
            AgentTarget agtTgt = new AgentTarget();
            agtTgt.agent = agent.GetTargetAgent();
            agtTgt.targetPoints = new int[6];
            Attack(
                agtTgt,
                agent.currentWaypoint.waypointID,
                agent.GetTargetAgent().currentWaypoint.waypointID);
        }
#endif
    }


    public override void Attack(AgentTarget targetOpponent, string shooterPos, string targetPos)
    {
        if (coolDownWait || !gameObject.activeSelf)
        {
            return;
        }

        StanceController sc = targetOpponent.agent.GetComponent<WaypointVisibilityController>().stanceController;
        transformToAimAt = sc.GetRandomPart(targetOpponent.targetPoints);

        float chanceOfHit = AttackCalculation(targetOpponent.targetPoints);

        if (Random.value < chanceOfHit)
        {
            // target hit
            targetOpponent.agent.Health.SubtractHealth(Mathf.RoundToInt(damage), agent);
        }
        else
        {
            // target miss
        }

        // Sound
        if (PlaySound && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
        }
    }

    public virtual float AttackCalculation(int[] targetPoints)
    {
        return VisibilityController.GetPercentageOfBodyPointsVisible(targetPoints);
    }

    /// <summary>
    /// When the episode is reset, call this command
    /// </summary>
    public override void OnReset()
    {
        // Override this for attacks
    }
}
