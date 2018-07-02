using System;
using System.Collections.Generic;
using UnityEngine;

internal class BehaviorLaneFollow : AiBehavior
{
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
            {
                //Debug.Log(chosenSegment.Item1.Id + " " + chosenSegment.Item2);
                var proj = _parentContainer.ProjectPathLaneFromOrigin(agent, 3.5 + agent.Dimensions.y / 2, true);

                var target = RoadUtilities.Calculate(proj._targetSegment.Polynomial, proj._targetRoot);
                SetProjectionGizmo(new List<Vector3>() { target }, Color.cyan);

                var direction = target - agent.CurrentPosition;
                direction.y   = 0;

                var curDirection = agent.CurrentFacingDirection;
                curDirection.y = 0;

                var angle2              = Vector3.Angle(direction, curDirection);
                var cross               = Vector3.Cross(direction, curDirection);
                if (cross.y > 0) angle2 = -angle2;

                //Debug.Log(angle2);

                var steer          = Mathf.Clamp(angle2 / 35, -1, 1);
                agent.DesiredSteer = steer;
            }
        }
    }

    /// <inheritdoc />
    public BehaviorLaneFollow(PiavCoreAiInput input)
        : base(input)
    { }
}