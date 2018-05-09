using UnityEngine;

internal class PiavStoppingData : MonoBehaviour
{
    [SerializeField] internal int _cycle;
    [SerializeField] internal int _start;
    [SerializeField] internal PiavRoadInterface _target;
    [SerializeField] internal StoppingPointType _type;
}

internal enum StoppingPointType
{
    StopSign,
    TrafficLight,
}