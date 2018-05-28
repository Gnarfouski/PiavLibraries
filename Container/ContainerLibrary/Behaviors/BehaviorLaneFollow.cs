using System;
using System.Collections.Generic;
using UnityEngine;

internal class BehaviorLaneFollow : AiBehavior
{
    internal override void Update(PiavRoadAgent agent)
    {
        if (agent != null && agent.CurrentSegments != null && agent.CurrentSegments.Count > 0)
        {
            Tuple<Segment, double,double,double> chosenSegment = null;
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
            {
                //Debug.Log(chosenSegment.Item1.Id + " " + chosenSegment.Item2);
                var proj = _parentAi.ProjectPathLaneFromOrigin(3.5 + agent.Dimensions.y / 2, true);

                var target = RoadUtilities.Calculate(proj.Item2.Polynomial, proj.Item3);
                SetProjectionGizmo(new List<Vector3>() { target }, Color.cyan);

                var direction = target - agent.CurrentPosition;
                direction.y   = 0;

                var angle2              = Vector3.Angle(direction, agent.CurrentFacingDirection);
                var cross               = Vector3.Cross(direction, agent.CurrentFacingDirection);
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