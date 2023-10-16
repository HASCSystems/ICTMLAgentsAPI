using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimescaleController : MonoBehaviour
{
    public float currentTimeScale;
    // Update is called once per frame
    public Text timescaleDisplay;
    public bool timeTest = true, fixedTimeTest = true;
    private float lastTime = 0f, lastFixedTime = 0f;

    [Header("Time Trials")]
    public bool runTimeTrial = false;
    private bool isReady = false;
    public float timeTrialIntervalTime = 5f;
    public float finalTimeScale = 40f;
    private float timeTrialTimer = 0f;

    protected virtual void Start()
    {
        Time.timeScale = currentTimeScale;
        isReady = true;
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 2f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 3f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Time.timeScale = 4f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Time.timeScale = 5f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Time.timeScale = 6f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Time.timeScale = 7f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Time.timeScale = 8f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Time.timeScale = 9f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Time.timeScale *= 2f;
        }
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            Time.timeScale += 1f;
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore))
        {
            Time.timeScale -= 1f;
        }
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            Time.timeScale = 1f / Time.timeScale;
        }
        currentTimeScale = Time.timeScale;

        if (timescaleDisplay != null)
        {
            timescaleDisplay.text = "Timescale: " + Time.timeScale + "x";
        }

        if (timeTest)
        {
            Debug.Log("UPDATE Timescale: " + Time.timeScale + "; realtimesincelast delta=" + (Time.realtimeSinceStartup - lastTime) + "; deltaTime=" + Time.deltaTime + "; unscaledDeltaTime=" + Time.unscaledDeltaTime);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (fixedTimeTest)
        {
            Debug.Log("FIXEDUPDATE Timescale: " + Time.timeScale + "; fixedrealtimesincelast delta=" + (Time.realtimeSinceStartup - lastFixedTime) + "; fixeddeltaTime=" + Time.fixedDeltaTime + "; unscaledFixedDeltaTime=" + Time.fixedUnscaledDeltaTime);
        }

        if (runTimeTrial && isReady)
        {
            if (timeTrialTimer >= timeTrialIntervalTime/Time.timeScale)
            {
                Time.timeScale += 1f;
                if (Time.timeScale > finalTimeScale)
                {
                    Application.Quit();
                }
                timeTrialTimer = 0f;
            }
            timeTrialTimer += Time.fixedUnscaledDeltaTime;
        }
    }
}
