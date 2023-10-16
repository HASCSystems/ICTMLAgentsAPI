using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtility : MonoBehaviour
{
    public static bool AreEqual(float A, float B, float delta)
    {
        return Mathf.Approximately(A, B) || (Mathf.Abs(A - B) <= delta);
    }
}
