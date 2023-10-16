using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View for agent data so they are easy to track during runtime
/// </summary>
public class MarkerView : MonoBehaviour
{
    public Transform markerTransform;
    public Text markerText;
    private string divider = ".";

    protected virtual void Update()
    {
        ScoutAgent sa = GetComponent<ScoutAgent>();
        markerText.text =
            transform.GetSiblingIndex() + divider +
            sa.Health.GetHealth() + divider +
            sa.lastMoveAction + divider +
            "E" + sa.CompletedEpisodes + "S" + sa.StepCount + divider +
            sa.lastWaypoint.waypointID;
        markerTransform.localEulerAngles = -transform.localEulerAngles;
    }
}
