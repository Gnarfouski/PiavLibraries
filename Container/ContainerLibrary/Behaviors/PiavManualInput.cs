using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiavManualInput : PiavInput
{
    private void Update()
    {
        _parentAgent.DesiredThrottle = Input.GetAxis("Vertical");
        _parentAgent.DesiredSteer = Input.GetAxis("Horizontal");
    }
}
