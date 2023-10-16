using UnityEngine;

public class TransformCopier : MonoBehaviour
{
    public Transform sourceTransform;

    // Update is called once per frame
    void Update()
    {
        if (sourceTransform != null)
        {
            transform.localPosition = sourceTransform.localPosition;
            transform.localEulerAngles = sourceTransform.localEulerAngles;
            transform.localScale = sourceTransform.localScale;
        }
    }
}
