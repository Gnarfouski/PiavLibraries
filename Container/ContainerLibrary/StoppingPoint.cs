public class StoppingPoint
{
    public PiavRoadAgent Origin;
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