using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ProjectileBullet : MonoBehaviour
{
    public delegate void HitByProjectileDelegate(ScoutAgent shooter, ScoutAgent target, ProjectileBullet pb);
    public static event HitByProjectileDelegate HitByProjectile = null;

    public bool inPlay;

    [HideInInspector]
    public Rigidbody rb;

    public Collider ProjectileCollider;

    public int TeamToIgnore;
    public ScoutAgent shotBy;

    protected Material m_ProjectileMat;

    [Header("COLOR FLASHING")] public int FlashFrequency = 3; //The rate the ball should flash based on frames;
    Color m_PrimaryColor;
    public Color FlashColor = Color.white;

    protected Vector3 m_ResetPosition;

    protected TrailRenderer m_TrailRenderer;
    public virtual void SetResetPosition(Vector3 position)
    {
        m_ResetPosition = position;
        m_TrailRenderer.Clear();
    }

    protected virtual void Awake()
    {
        shotBy = null;
        m_ProjectileMat = ProjectileCollider.gameObject.GetComponent<MeshRenderer>().material;
        m_TrailRenderer = GetComponentInChildren<TrailRenderer>();
        m_PrimaryColor = m_ProjectileMat.color;
    }


    protected virtual void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (inPlay)
        {
            if (FlashFrequency > 0 && Time.frameCount % FlashFrequency == 0)
            {
                m_ProjectileMat.color = m_ProjectileMat.color == m_PrimaryColor ? FlashColor : m_PrimaryColor;
            }
        }
    }

    protected virtual void TagBallAs(string tag)
    {
        //gameObject.tag = tag;
        //ProjectileCollider.gameObject.tag = tag;
    }

    [Header("Projectile Attack Properties")]
    public float damage = 10f;
    public ScoutAgent thrower_sa;
    public string shooterPos, targetPos;



    protected virtual void OnCollisionEnter(Collision col)
    {
        GameObject colEncapsulatingAgent = VisibilityController.GetEncapsulatingAgentForSubObject(col.gameObject);

        if (col.gameObject.CompareTag("ground") || (colEncapsulatingAgent == null)) // Missed player!
        {
            shotBy = null;

            string pair = shooterPos + targetPos;
            /*if (ScoutAgent.useShooterTargetMissPair && !ScoutAgent.shooterTargetMissPair.Contains(pair))
            {
                ScoutAgent.shooterTargetMissPair.Add(
                    pair
                    );
            }*/

            /*Debug.Log("ShooterTargetMissPair. Count=" + ScoutAgent.shooterTargetMissPair.Count + "; Added: " + shooterPos + "," + targetPos);*/

            gameObject.SetActive(false);
        }
        else if (colEncapsulatingAgent.CompareTag("blueAgent"))
        {
            if (thrower_sa.gameObject.CompareTag("redAgent"))
            {
                if (HitByProjectile != null)
                {
                    HitByProjectile(thrower_sa, colEncapsulatingAgent.GetComponent<ScoutAgent>(), this);
                }
                colEncapsulatingAgent.GetComponent<ScoutAgent>().Health.SubtractHealth(Mathf.RoundToInt(damage), thrower_sa);
            }
            gameObject.SetActive(false);
        }
        else if (colEncapsulatingAgent.CompareTag("redAgent"))
        {
            if (thrower_sa.gameObject.CompareTag("blueAgent"))
            {
                if (HitByProjectile != null)
                {
                    HitByProjectile(thrower_sa, colEncapsulatingAgent.GetComponent<ScoutAgent>(), this);
                }
                colEncapsulatingAgent.GetComponent<ScoutAgent>().Health.SubtractHealth(Mathf.RoundToInt(damage), thrower_sa);
            }
            gameObject.SetActive(false);
        }
    }
}
