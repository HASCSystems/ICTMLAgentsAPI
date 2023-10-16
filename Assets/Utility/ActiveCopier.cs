using UnityEngine;

public class ActiveCopier : MonoBehaviour
{
    public GameObject toCopy;
    public GameObject toApply;

    // Update is called once per frame
    void Update()
    {
        if ((toCopy != null) &&
            (toApply != null))
        {
            toApply.SetActive(toCopy.activeSelf);
        }
    }
}
