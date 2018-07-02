using System;

internal class BehaviorAdaptSpeedToSlowerVehicle : BehaviorSpeedManagement
{
    /// <inheritdoc />
    public BehaviorAdaptSpeedToSlowerVehicle(PiavCoreAiInput input)
        : base(input)
    { }

    /// <inheritdoc />
    internal override void Update(PiavRoadAgent agent)
    {
        double        minimalDistance      = double.PositiveInfinity;
        StoppingPoint closestStoppingPoint = null;

        for (int i = 0; i < agent.CurrentStops._points.Length; i++)
        {
            if (agent.CurrentStops._points[i].Type == StoppingType.Vehicle && agent.CurrentStops._distances[i] < minimalDistance)
            {
                minimalDistance      = agent.CurrentStops._distances[i];
                closestStoppingPoint = agent.CurrentStops._points[i];
            }
        }


        for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
        {
            if (_parentAi._profile._forcedSpeedLimitValues[i]._originBehaviorType == GetType())
                if (closestStoppingPoint != null)
                    if (minimalDistance < closestStoppingPoint.Origin.AdditionalLength.y + closestStoppingPoint.Origin.Dimensions.y / 2.0
                                                                                         + Math.Max(2.0 , closestStoppingPoint.Origin.CurrentAbsoluteSpeed * 1.5))
                        _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation{_value = closestStoppingPoint.Origin.CurrentAbsoluteSpeed * 0.9, _originBehaviorType = GetType()};
                    else
                        _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation{_value = closestStoppingPoint.Origin.CurrentAbsoluteSpeed * 1.1, _originBehaviorType = GetType()};
                else
                    _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation{_value = double.PositiveInfinity, _originBehaviorType = GetType()};
        }
    }
}