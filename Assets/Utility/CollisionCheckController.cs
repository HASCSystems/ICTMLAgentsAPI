using UnityEngine;

public class CollisionCheckController : MonoBehaviour
{
    public static bool IS_DEBUG = false;
    private bool m_isCollidingWithTerrain = false;

    public bool IsCollidingWithTerrain
    {
        get
        {
            return m_isCollidingWithTerrain;
        }
    }

    public virtual void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("ground"))
        {
            m_isCollidingWithTerrain = true;
            if (IS_DEBUG) Debug.Log("Colliding with " + other.gameObject.name);
            return;
        }
    }

    public virtual void ResetCollisionStatus()
    {
        m_isCollidingWithTerrain = false;
    }
}
