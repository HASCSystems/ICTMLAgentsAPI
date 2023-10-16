using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentInput : MonoBehaviour
{
    [SerializeField]
    private int _CurrentInput = 0;
    public int CurrentInput { get { return _CurrentInput; } }
    // Update is called once per frame
    protected virtual void Update()
    {
        _CurrentInput = GetInputFromKeyPress();
    }

    protected virtual int GetInputFromKeyPress()
    {
        if (Input.GetKey(KeyCode.W))
            return 1;
        if (Input.GetKey(KeyCode.E))
            return 2;
        if (Input.GetKey(KeyCode.D))
            return 3;
        if (Input.GetKey(KeyCode.C))
            return 4;
        if (Input.GetKey(KeyCode.X))
            return 5;
        if (Input.GetKey(KeyCode.Z))
            return 6;
        if (Input.GetKey(KeyCode.A))
            return 7;
        if (Input.GetKey(KeyCode.Q))
            return 8;
        else
            return 0;
    }
}
