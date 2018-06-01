
using System.Collections.Generic;
using UnityEngine;

internal class BehaviorFollowPath : AiBehavior
{

    internal override void Update(PiavRoadAgent agent)
    {
        if (agent.DesiredIncomingPath.Count > 1)
        {
            for (int i = 1; i < agent.DesiredIncomingPath.Count; i++)
            {
                foreach (var possibleSegment in agent.CurrentSegments)
                {
                    if (possibleSegment.Item1.Id == agent.DesiredIncomingPath[i].Item1.Id && possibleSegment.Item2 > 0 && possibleSegment.Item2 < 1)
                    {
                        agent.DesiredIncomingPath.RemoveRange(0,i);
                        break;
                    }
                }
            }

            var gizmolist = new List<Vector3>();

            foreach (var tuple in agent.DesiredIncomingPath)
                gizmolist.Add(RoadUtilities.Calculate(tuple.Item1.Polynomial, (tuple.Item2 + tuple.Item3) / 2));
            SetProjectionGizmo(gizmolist, Color.red);
        }
    }

    /// <inheritdoc />
    public BehaviorFollowPath(PiavCoreAiInput input)
        : base(input)
    { }
}