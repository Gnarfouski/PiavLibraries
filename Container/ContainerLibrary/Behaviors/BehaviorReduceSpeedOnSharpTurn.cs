using System.Collections.Generic;
using UnityEngine;
using System;

internal class BehaviorReduceSpeedOnSharpTurn : BehaviorSpeedManagement
{
    public BehaviorReduceSpeedOnSharpTurn(PiavCoreAiInput input)
        : base(input)
    { }

    internal override void Update(PiavRoadAgent agent)
    {
        if (agent != null && agent.CurrentSegments != null && agent.CurrentSegments.Count > 0)
        {
            Tuple<Segment, double, double, double> chosenSegment = null;

            if (agent.DesiredIncomingPath != null && agent.DesiredIncomingPath.Count > 0)
                foreach (var possibleSegment in agent.CurrentSegments)
                {
                    if (possibleSegment.Item1.Id == agent.DesiredIncomingPath[0].Item1.Id)
                    {
                        chosenSegment = possibleSegment;

                        break;
                    }
                }
            else
                chosenSegment = agent.CurrentSegments[0];

            if (chosenSegment != null)
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i].Item2 == GetType())
                    {
                        double totalAngle  = 0;
                        var    projections = new List<Vector3>();

                        for (int j = 1; j <= 3; j++)
                        {
                            var proj = _parentAi.ProjectPathLaneFromOrigin((3 + agent.CurrentAbsoluteSpeed) * j * 1.2, true);

                            if (proj != null)
                            {
                                var pos = RoadUtilities.Calculate(proj.Item2.Polynomial, proj.Item3);
                                pos.y   = agent.CurrentPosition.y;
                                projections.Add(pos);
                                var dir    = pos - agent.CurrentPosition;
                                totalAngle += Math.Abs(Vector3.Angle(dir, agent.CurrentFacingDirection));
                            }
                        }
                        totalAngle /= 3;
                        SetProjectionGizmo(projections, Color.yellow);

                        if (totalAngle < 5)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(double.PositiveInfinity, GetType());
                        else if (totalAngle < 35)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(5 / ((totalAngle - 4) / 30), GetType());
                        else if (totalAngle < 125)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(5 - (totalAngle - 35) / 225, GetType());
                        else
                            _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(0.1, GetType());

                        break;
                    }
                }
            else
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i].Item2 == GetType())
                        _parentAi._profile._forcedSpeedLimitValues[i] = new Tuple<double, Type>(double.PositiveInfinity, GetType());
                }
        }
    }
}
