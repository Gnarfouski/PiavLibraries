using System;

internal abstract class BehaviorSpeedManagement : AiBehavior
{
    protected BehaviorSpeedManagement(PiavCoreAiInput input)
        : base(input)
    {
        _parentAi._profile._forcedSpeedLimitValues.Add(new Tuple<double, Type>(double.PositiveInfinity, GetType()));
    }

    internal abstract override void Update(PiavRoadAgent agent);
}
