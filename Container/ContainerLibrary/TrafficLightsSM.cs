using UnityEngine;

internal class TrafficLightsSm : StoppingPointStateMachine
{
    public TrafficLightsSm(float st, float cycle)
    {
        _startTime     = st;
        _fullCycleTime = cycle;
    }

    public override bool GetState()
    {
        return Time.time >= _startTime && (Time.time - _startTime) % _fullCycleTime < _fullCycleTime / 2;
    }

    #region Variables

    private float _fullCycleTime;
    private float _startTime;

    #endregion
}