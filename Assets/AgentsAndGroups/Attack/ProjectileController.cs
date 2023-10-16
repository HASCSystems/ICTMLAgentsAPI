using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : RangedAttack
{
    public delegate void ProjectileShotDelegate(ScoutAgent shooter, ScoutAgent target, ProjectileBullet pb);
    public static event ProjectileShotDelegate ProjectileShot = null;

    public bool AllowKeyboardInput = true; //this mode ignores player input
    public bool initialized; //has this robot been initialized
    public KeyCode shootKey = KeyCode.J;

    //SHOOTING RATE
    [Header("SHOOTING RATE")]
    public float shootingRate = .02f; //can shoot every shootingRate seconds. ex: .5 can shoot every .5 seconds

    public float coolDownTimer;
    public bool coolDownWait;

    //PROJECTILES
    [Header("PROJECTILE")] public GameObject projectilePrefab;
    public int numberOfProjectilesToPool = 25;
    public Transform projectileOrigin; //the transform the projectile will originate from
    public List<Rigidbody> projectilePoolList = new List<Rigidbody>(); //projectiles to shoot
    private int projectileIndex = 0;

    //FORCES
    [Header("FORCES")] public float forceToUse;

    [Header("MUZZLE FLASH")] public bool UseMuzzleFlash;
    public GameObject MuzzleFlashObject;

    [Header("SOUND")] public bool PlaySound;
    public ForceMode forceMode;
    private AudioSource m_AudioSource;

    [Header("SCREEN SHAKE")] public bool UseScreenShake;

    [Header("TRANSFORM SHAKE")] public bool ShakeTransform;
    public float ShakeDuration = .1f;
    public float ShakeAmount = .1f;
    private Vector3 startPos;
    private bool m_TransformIsShaking;

    [Header("ACCURACY")]
    public Transform transformToAimAt;
    [Tooltip("If closer than this amount, then auto-hit")]
    public float pointBlankDistance = 2f;
    public Vector2 jitterMax_deg = new Vector2(1.5f, 1.5f);

    [Header("DAMAGE")]
    public int bulletDamageOverride = -1;

    //public CinemachineImpulseSource impulseSource;

    // Start is called before the first frame update
    void Start()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    protected virtual void Initialize()
    {
        //impulseSource = GetComponent<CinemachineImpulseSource>();
        projectilePoolList.Clear(); //clear list in case it's not empty
        for (var i = 0; i < numberOfProjectilesToPool; i++)
        {
            GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Rigidbody p = obj.GetComponent<Rigidbody>();
            projectilePoolList.Add(p);
            p.transform.position = projectileOrigin.position;
            p.gameObject.SetActive(false);
        }

        if (MuzzleFlashObject)
        {
            MuzzleFlashObject.SetActive(false);
        }

        m_AudioSource = GetComponent<AudioSource>();

        initialized = true;
    }

    public override void OnReset()
    {
        DisableAllProjectiles();
    }

    public void DisableAllProjectiles()
    {
        foreach (Rigidbody rb in projectilePoolList)
        {
            rb.gameObject.SetActive(false);
        }
    }

    protected virtual void FixedUpdate()
    {
        coolDownWait = coolDownTimer > shootingRate ? false : true;
        coolDownTimer += Time.fixedDeltaTime;

#if UNITY_EDITOR
        if (Input.GetKeyDown(shootKey))
        {
            if (projectilePoolList.Count <= 0)
            {
                Initialize();
            }

            Shoot(projectilePoolList[projectileIndex].gameObject.GetComponent<ProjectileBullet>(),
                  transform.parent.GetComponent<ScoutAgent>());
        }
#endif
    }

    public override void Attack(AgentTarget targetOpponent, string shooterPos, string targetPos)
    {
        Shoot(targetOpponent, shooterPos, targetPos);
    }

    public virtual void Shoot(AgentTarget targetOpponent, string shooterPos, string targetPos)
    {
        StanceController sc = targetOpponent.agent.GetComponent<WaypointVisibilityController>().stanceController;
        transformToAimAt = sc.GetRandomPart(targetOpponent.targetPoints);

        if (projectilePoolList.Count <= 0)
        {
            Initialize();
        }

        ProjectileBullet pb = projectilePoolList[projectileIndex].gameObject.GetComponent<ProjectileBullet>();
        pb.shooterPos = shooterPos;
        pb.targetPos = targetPos;

        if (ProjectileShot != null)
        {
            ProjectileShot(transform.parent.GetComponent<ScoutAgent>(), targetOpponent.agent, pb);
        }
        Shoot(pb,
              transform.parent.GetComponent<ScoutAgent>(),
              VisibilityController.GetPercentageOfBodyPointsVisible(targetOpponent.targetPoints)
              );
    }


    public virtual void Shoot(ScoutAgent targetOpponent)
    {
        transformToAimAt = targetOpponent.transform;

        if (projectilePoolList.Count <= 0)
        {
            Initialize();
        }

        ProjectileBullet pb = projectilePoolList[projectileIndex].gameObject.GetComponent<ProjectileBullet>();
        if (ProjectileShot != null)
        {
            ProjectileShot(transform.parent.GetComponent<ScoutAgent>(), targetOpponent, pb);
        }
        Shoot(pb,
              transform.parent.GetComponent<ScoutAgent>());

    }

    //ignoreTeam. 0 ignores team 0, 1 ignores team 1, -1 ignores no teams
    public virtual void Shoot(ProjectileBullet db, ScoutAgent thrower, int ignoreTeam = -1)
    {
        if (coolDownWait || !gameObject.activeSelf)
        {
            return;
        }

        if (transformToAimAt != null)
        {
            transform.parent.LookAt(transformToAimAt);
            transform.localEulerAngles = Vector3.zero;
            // Point blank scenario handling
            if (Vector3.Distance(transform.parent.position, transformToAimAt.position) <= pointBlankDistance)
            {
                float d = bulletDamageOverride >= 0f ? bulletDamageOverride : ((ProjectileBullet)db).damage;
                transformToAimAt.GetComponent<ScoutAgent>()?.Health.SubtractHealth(Mathf.RoundToInt(d), thrower);
                return;
            }
            ScoutAgent thisAgent = thrower?.gameObject?.GetComponent<ScoutAgent>();
            float pointVisibilityIndex = VisibilityController.GetPercentageOfBodyPointsVisible(
                thisAgent?.transform, transformToAimAt,
                thisAgent?.IsStanding() ?? true,
                transformToAimAt.GetComponent<ScoutAgent>().IsStanding(),
                true);
            //Debug.Log("PointVisiblityIndex=" + pointVisibilityIndex + "; thrower=" + thrower.name);
            // Rescales 1/6 -> 1 to 1 -> 0
            float jitterCoefficient = Mathf.Min(1f, 1f -
                                        (pointVisibilityIndex - (1f / VisibilityController.numVisibilityChecks))
                                            * (VisibilityController.numVisibilityChecks / (VisibilityController.numVisibilityChecks - 1f)));
            //pointVisibilityIndex; // if visibility is high, lowest jitter. Low visibility => high jitter
            //Debug.Log("jitterCoefficient=" + jitterCoefficient);
            transform.localEulerAngles += new Vector3(
                                                jitterCoefficient * Random.Range(-jitterMax_deg.x, jitterMax_deg.x),
                                                jitterCoefficient * Random.Range(-jitterMax_deg.y, jitterMax_deg.y),
                                                0f
                                                );
        }

        coolDownTimer = 0; //reset timer
        db.shotBy = thrower;
        if (db.GetType().IsAssignableFrom(typeof(ProjectileBullet)))
        {
            ((ProjectileBullet)db).thrower_sa = thrower?.gameObject?.GetComponent<ScoutAgent>();
            if (bulletDamageOverride >= 0f)
            {
                ((ProjectileBullet)db).damage = bulletDamageOverride;
            }
        }
        FireProjectile(db.rb);

        projectileIndex = (projectileIndex + 1) % projectilePoolList.Count;
    }

    public virtual void Shoot(ProjectileBullet db, ScoutAgent thrower, float pointVisibilityPct, int ignoreTeam = -1)
    {
        if (coolDownWait || !gameObject.activeSelf)
        {
            return;
        }

        if (transformToAimAt != null)
        {
            transform.parent.LookAt(transformToAimAt);
            transform.localEulerAngles = Vector3.zero;
            // Point blank scenario handling
            if (Vector3.Distance(transform.parent.position, transformToAimAt.position) <= pointBlankDistance)
            {
                float d = bulletDamageOverride >= 0f ? bulletDamageOverride : ((ProjectileBullet)db).damage;
                transformToAimAt.GetComponent<ScoutAgent>()?.Health.SubtractHealth(Mathf.RoundToInt(d), thrower);
                return;
            }
            ScoutAgent thisAgent = thrower?.gameObject?.GetComponent<ScoutAgent>();
            
            // Rescales 1/6 -> 1 to 1 -> 0
            float jitterCoefficient = Mathf.Min(1f, 1f -
                                        (pointVisibilityPct - (1f / VisibilityController.numVisibilityChecks))
                                            * (VisibilityController.numVisibilityChecks / (VisibilityController.numVisibilityChecks - 1f)));
            //pointVisibilityIndex; // if visibility is high, lowest jitter. Low visibility => high jitter
            //Debug.Log("jitterCoefficient=" + jitterCoefficient);
            transform.localEulerAngles += new Vector3(
                                                jitterCoefficient * Random.Range(-jitterMax_deg.x, jitterMax_deg.x),
                                                jitterCoefficient * Random.Range(-jitterMax_deg.y, jitterMax_deg.y),
                                                0f
                                                );
        }

        coolDownTimer = 0; //reset timer
        db.shotBy = thrower;
        if (db.GetType().IsAssignableFrom(typeof(ProjectileBullet)))
        {
            ((ProjectileBullet)db).thrower_sa = thrower?.gameObject?.GetComponent<ScoutAgent>();
            if (bulletDamageOverride >= 0f)
            {
                ((ProjectileBullet)db).damage = bulletDamageOverride;
            }
        }
        FireProjectile(db.rb);

        projectileIndex = (projectileIndex + 1) % projectilePoolList.Count;
    }

    public virtual void Drop(ProjectileBullet db)
    {
        var rb = db.rb;
        rb.transform.position = projectileOrigin.position;
        rb.transform.rotation = projectileOrigin.rotation;
        rb.gameObject.SetActive(true);
    }

    public void FireProjectile(Rigidbody rb)
    {
        rb.transform.position = projectileOrigin.position;
        rb.transform.rotation = projectileOrigin.rotation;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.gameObject.SetActive(true);
        rb.AddForce(projectileOrigin.forward * forceToUse, forceMode);
        /*if (UseScreenShake && impulseSource)
        {
            impulseSource.GenerateImpulse();
        }*/

        if (ShakeTransform && !m_TransformIsShaking)
        {
            StartCoroutine(I_ShakeTransform());
        }

        if (PlaySound)
        {
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
        }
    }

    protected virtual IEnumerator I_ShakeTransform()
    {
        m_TransformIsShaking = true;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        if (UseMuzzleFlash && MuzzleFlashObject)
        {
            MuzzleFlashObject.transform.localScale = Random.Range(.5f, 1.5f) * Vector3.one;
            MuzzleFlashObject.SetActive(true);
        }

        float timer = 0;
        startPos = transform.localPosition;
        while (timer < ShakeDuration)
        {
            var pos = startPos + (Random.insideUnitSphere * ShakeAmount);
            transform.localPosition = pos;
            timer += Time.fixedDeltaTime;
            yield return wait;
        }

        transform.localPosition = startPos;
        if (UseMuzzleFlash && MuzzleFlashObject)
        {
            MuzzleFlashObject.SetActive(false);
        }

        m_TransformIsShaking = false;
    }
}
