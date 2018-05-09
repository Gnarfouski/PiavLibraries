using UnityEngine;

internal class PiavParkingSpaceData : MonoBehaviour
{
    [SerializeField] internal ParkingSpaceType _spaceType;
    [SerializeField] internal PiavRoadInterface _roadInterface;
}

internal enum ParkingSpaceType
{
    Vertical, Horizontal
}