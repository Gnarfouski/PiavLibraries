using System;

internal class BehaviorRespectRoadSpeedLimit : BehaviorSpeedManagement
{
    public BehaviorRespectRoadSpeedLimit(PiavCoreAiInput input)
        : base(input) {}

    /// <inheritdoc />
    internal override void Update(PiavRoadAgent agent)
    {
        if (agent != null)
            if (agent.CurrentSegments.Count > 0)
            {
                var mas = agent.CurrentSegments[0];

                if (mas._segment != null)
                    for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                    {
                        if (_parentAi._profile._forcedSpeedLimitValues[i]._originBehaviorType == GetType())
                            _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation{_value = mas._segment.ParentLane.SpeedLimit, _originBehaviorType = GetType()};
                    }
            }
            else
            {
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i]._originBehaviorType == GetType())
                        _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation{_value = 0, _originBehaviorType = GetType()};
                }
            }
    }
}
