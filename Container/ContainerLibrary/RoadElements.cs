using System.Collections.Generic;

public class Road
{
    public long Id;
    public Lane[] ChildLanes;
}

public class Lane
{
    public long Id;
    public Road ParentRoad;
    public Segment[] ChildSegments;

    public bool Direction;
    public LaneType MyType;
    public double SpeedLimit;
    public double Width;
}

public class Segment
{
    public long Id;
    public Lane ParentLane;

    public double[,] Polynomial;
    public double[] ArcLength;
    public double[] InvArcLength;

    public List<Contact> Contacts = new List<Contact>();
    public List<Contact> IncomingContacts = new List<Contact>();
    public List<StoppingPoint> Stops = new List<StoppingPoint>();
    public List<ParkingSpace> ParkingSpaces = new List<ParkingSpace>();
}

public enum LaneType
{
    Pedestrian, Vehicle
}