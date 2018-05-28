
internal class BehaviorSetPathTarget : AiBehavior
{
    internal override void Update(PiavRoadAgent agent)
    {
        if (agent.DesiredIncomingPath == null || agent.DesiredIncomingPath.Count <= 1)
        {
            _parentAi._profile._targetDestination                                                                                          = _parentContainer.FindRandomSegmentAndRoot();
            while (_parentAi._profile._targetDestination.Item1.ParentLane.MyType != LaneType.Vehicle) _parentAi._profile._targetDestination = _parentContainer.FindRandomSegmentAndRoot();
            _parentAi._profile._needTargetDestination                                                                                      = true;
        }

    }
    /// <inheritdoc />
    public BehaviorSetPathTarget(PiavCoreAiInput input)
        : base(input)
    { }
}
