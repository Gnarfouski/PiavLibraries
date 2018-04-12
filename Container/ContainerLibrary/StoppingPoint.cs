public class StoppingPoint
{
    public long OriginId;
    public Segment ParentSegment;
    public double Root;
    public StoppingPointStateMachine Sm;
    public StoppingType Type;
}

public enum StoppingType
{
    StopSign,
    TrafficLight,
    Vehicle
}