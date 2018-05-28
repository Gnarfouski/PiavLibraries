
using System.Collections.Generic;
using UnityEngine;

internal class BehaviorFollowPath : AiBehavior
{

    internal override void Update(PiavRoadAgent agent)
    {
        if (agent.DesiredIncomingPath.Count >= 1)
        {
            foreach (var possibleSegment in agent.CurrentSegments)
            {
                if (possibleSegment.Item1.Id == agent.DesiredIncomingPath[0].Item1.Id &&
                    (possibleSegment.Item1.ParentLane.Direction && possibleSegment.Item2 <= agent.DesiredIncomingPath[0].Item2 ||
                        !possibleSegment.Item1.ParentLane.Direction && possibleSegment.Item2 >= agent.DesiredIncomingPath[0].Item3))
                        agent.DesiredIncomingPath.RemoveAt(0);
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