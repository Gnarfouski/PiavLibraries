using UnityEngine;

public class ParkingSpace
{

    public double Root;
    public bool IsOccupied;
    public Vector3 Position;
    public Vector3 Direction;
    public Segment ParentSegment;
    public ParkingType MyType;
}

public enum ParkingType
{
    Vertical, Horizontal
}
