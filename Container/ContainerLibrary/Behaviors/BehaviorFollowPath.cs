
using System.Collections.Generic;
using UnityEngine;

internal class BehaviorFollowPath : AiBehavior
{

    internal override void Update(PiavRoadAgent agent)
    {
        if (agent._desiredIncomingPath.Count > 1)
        {
            for (int i = 1; i < agent._desiredIncomingPath.Count; i++)
            {
                foreach (var possibleSegment in agent.CurrentSegments)
                {
                    if (possibleSegment._segment.Id == agent._desiredIncomingPath[i]._parentSegment.Id && possibleSegment._root > 0 && possibleSegment._root < 1)
                    {
                        agent._desiredIncomingPath.RemoveRange(0,i);
                        break;
                    }
                }
            }

            var gizmolist = new List<Vector3>();

            foreach (var tuple in agent._desiredIncomingPath)
                gizmolist.Add(RoadUtilities.Calculate(tuple._parentSegment.Polynomial, (tuple._fromRoot + tuple._toRoot) / 2));
            SetProjectionGizmo(gizmolist, Color.red);
        }
    }

    /// <inheritdoc />
    public BehaviorFollowPath(PiavCoreAiInput input)
        : base(input)
    { }
}