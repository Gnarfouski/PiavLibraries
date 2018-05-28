internal class BehaviorMaintainSpeed : AiBehavior
{
    /// <inheritdoc />
    internal override void Update(PiavRoadAgent agent)
    {
        var speedTarget = _parentAi._profile.GetMinimalSpeedValue();

        if (!double.IsNaN(speedTarget))
        {
            var lowerThreshold = speedTarget * 0.9;
            var upperThreshold = speedTarget * 1.1;

            if (agent.CurrentAbsoluteSpeed < lowerThreshold)
                agent.DesiredThrottle = 1;
            else if (agent.CurrentAbsoluteSpeed > upperThreshold)
                agent.DesiredThrottle = -1;
            else
                agent.DesiredThrottle = 2 * (agent.CurrentAbsoluteSpeed - lowerThreshold) / (upperThreshold - lowerThreshold) - 1;
        }
        else
        {
            agent.DesiredThrottle = 0;
        }
    }

    /// <inheritdoc />
    public BehaviorMaintainSpeed(PiavCoreAiInput input)
        : base(input)
    { }
}