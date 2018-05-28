using System;
using System.Collections.Generic;

internal class AgentProfile
{

    #region Variables

    //public float ForcedSlmTime;
    internal List<Tuple<double,Type>> _forcedSpeedLimitValues = new List<Tuple<double, System.Type>>();

    /*
    public int PerpetratorId = -1;
    public int        SearchTargetId = -1;

    public float SpeedLimitRespectMultiplier = 1;
    */

    /*
    internal bool _joinTarget;
    internal bool _needTarget;
    internal bool _collideWithTarget;
    internal bool _hadCollision;
    internal bool _exitParkingSpace;
    internal int _needParkingSpace = -1;
    internal int _requiredParkingType = -1;
    internal bool _hornSignal;
    internal bool _siren;

    private float _pSetTime;
    public bool StopGeneratingRoadStoppingPoint;

    internal int _parkingTargetIndex = -1;
        */

    internal bool _needTargetDestination;
    internal Tuple<Segment,double> _targetDestination;

    /*
    internal bool _isParked;
    internal bool _makeHeadLightSignal;
    internal const bool UseDirectionalVelocity = true;
    public bool HoldOvertake;
    */

    #endregion

    internal double GetMinimalSpeedValue()
    {
        double minSpeed = double.PositiveInfinity;

        foreach (var m in _forcedSpeedLimitValues)
            if (m.Item1 < minSpeed) minSpeed = m.Item1;

        return double.IsPositiveInfinity(minSpeed)? double.NaN:minSpeed;
    }

    /*
    internal void SetPerpetrator(int g)
    {
        if (g != -1)
        {
            PerpetratorId = g;
            _pSetTime = Time.time;
        }
    }
    */
}