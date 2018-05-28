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

                if (mas != null)
                    for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                    {
                        if (_parentAi._profile._forcedSpeedLimitValues[i].Item2 == GetType())
                            _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(mas.Item1.ParentLane.SpeedLimit, GetType());
                    }
            }
            else
            {
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i].Item2 == GetType())
                        _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(0, GetType());
                }
            }
    }
}
