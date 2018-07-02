
internal class BehaviorSetPathTarget : AiBehavior
{
    internal override void Update(PiavRoadAgent agent)
    {
        if (agent._desiredIncomingPath == null || agent._desiredIncomingPath.Count <= 1)
        {
            _parentAi._profile._targetDestination                                                                                          = _parentContainer.FindRandomSegmentAndRoot();
            while (_parentAi._profile._targetDestination._segment.ParentLane.MyType != LaneType.Vehicle) _parentAi._profile._targetDestination = _parentContainer.FindRandomSegmentAndRoot();
            _parentAi._profile._needTargetDestination                                                                                      = true;
        }

    }
    /// <inheritdoc />
    public BehaviorSetPathTarget(PiavCoreAiInput input)
        : base(input)
    { }
}
