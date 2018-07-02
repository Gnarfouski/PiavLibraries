using System;

internal abstract class BehaviorSpeedManagement : AiBehavior
{
    protected BehaviorSpeedManagement(PiavCoreAiInput input)
        : base(input)
    {
        _parentAi._profile._forcedSpeedLimitValues.Add(new PiavRoadContainer.SpeedLimitation{_value = double.PositiveInfinity, _originBehaviorType = GetType()});
    }

    internal abstract override void Update(PiavRoadAgent agent);
}
