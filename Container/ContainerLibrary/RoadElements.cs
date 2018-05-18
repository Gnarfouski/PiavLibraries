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
    public int SelfIndex;
    public List<DualVertex> DualVertices = new List<DualVertex>();

    public double[,] Polynomial;
    public double[] ArcLength;
    public double[] InvArcLength;

    public List<PointContact> Contacts = new List<PointContact>();
    public List<PointContact> IncomingContacts = new List<PointContact>();
    public List<LaneContact> LaneContacts = new List<LaneContact>();
    public List<StoppingPoint> Stops = new List<StoppingPoint>();
    public List<ParkingSpace> ParkingSpaces = new List<ParkingSpace>();
}

public enum LaneType
{
    Pedestrian, Vehicle
}