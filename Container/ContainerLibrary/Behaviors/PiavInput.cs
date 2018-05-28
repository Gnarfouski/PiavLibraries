using UnityEngine;

public class PiavInput : MonoBehaviour
{
    protected PiavRoadAgent _parentAgent;

    private void Awake()
    {
        _parentAgent = GetComponent<PiavRoadAgent>();
        if (_parentAgent == null) Debug.LogError("Could not initialize PiavInput, RoadAgent not found");
    }
}
