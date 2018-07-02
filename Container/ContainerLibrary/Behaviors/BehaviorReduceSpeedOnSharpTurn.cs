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
            var chosenSegment = new PiavRoadContainer.ValuedSegment();

            if (agent._desiredIncomingPath != null && agent._desiredIncomingPath.Count > 0)
                foreach (var possibleSegment in agent.CurrentSegments)
                {
                    if (possibleSegment._segment.Id == agent._desiredIncomingPath[0]._parentSegment.Id)
                    {
                        chosenSegment = possibleSegment;

                        break;
                    }
                }
            else
                chosenSegment = agent.CurrentSegments[0];

            if (chosenSegment._segment != null)
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i]._originBehaviorType == GetType())
                    {
                        double totalAngle  = 0;
                        var    projections = new List<Vector3>();

                        for (int j = 1; j <= 3; j++)
                        {
                            var proj = _parentContainer.ProjectPathLaneFromOrigin(agent, (3 + agent.CurrentAbsoluteSpeed) * j * 1.2, true);

                            var pos = RoadUtilities.Calculate(proj._targetSegment.Polynomial, proj._targetRoot);
                            pos.y   = agent.CurrentPosition.y;
                            projections.Add(pos);
                            var dir    = pos - agent.CurrentPosition;
                            totalAngle += Math.Abs(Vector3.Angle(dir, agent.CurrentFacingDirection));
                        }
                        totalAngle /= 3;
                        SetProjectionGizmo(projections, Color.yellow);

                        if (totalAngle < 5)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation { _value = double.PositiveInfinity, _originBehaviorType = GetType() };
                        else if (totalAngle < 35)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation { _value = 5 / ((totalAngle - 4) / 30), _originBehaviorType = GetType() };
                        else if (totalAngle < 125)
                            _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation { _value = 5 - (totalAngle - 35) / 225, _originBehaviorType = GetType() };
                        else
                            _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation { _value = 0.1, _originBehaviorType = GetType() };

                        break;
                    }
                }
            else
                for (int i = 0; i < _parentAi._profile._forcedSpeedLimitValues.Count; i++)
                {
                    if (_parentAi._profile._forcedSpeedLimitValues[i]._originBehaviorType == GetType())
                        _parentAi._profile._forcedSpeedLimitValues[i] = new PiavRoadContainer.SpeedLimitation
                        {
                            _value              = double.PositiveInfinity,
                            _originBehaviorType = GetType()
                        };
                }
        }


    }
}
