using System;
using System.Collections.Generic;
using UnityEngine;

public class PiavCoreAiInput : PiavInput {

    private void Awake()
    {
        _behaviors = new List<AiBehavior>
        {
            new BehaviorLaneFollow(this),
            new BehaviorMaintainSpeed(this),
            new BehaviorRespectRoadSpeedLimit(this),
            new BehaviorReduceSpeedOnSharpTurn(this)
            //new BehaviorFindPath(this),
            //new BehaviorFollowPath(this),
            //new BehaviorSetPathTarget(this)
        };
    }

    private void Update()
    {
        if (_parentAgent == null) _parentAgent = GetComponent<PiavRoadAgent>();

        foreach (var behavior in _behaviors) behavior.Update(_parentAgent);
    }

    internal Tuple<double, Segment, double> ProjectPathLaneFromOrigin(double distance, bool withTraffic)
    {
        if(distance < 0) throw new Exception("Project with positive distances only");

        var mas = _parentAgent.CurrentSegments[0];
        var projection = new Tuple<double, Segment, double>(distance,mas.Item1,mas.Item2);

        if (_parentAgent.DesiredIncomingPath != null && _parentAgent.DesiredIncomingPath.Count > 0)
            foreach (var possibleSegment in _parentAgent.CurrentSegments)
            {
                if (possibleSegment.Item1.Id == _parentAgent.DesiredIncomingPath[0].Item1.Id)
                {
                    projection = ProjectOnPath(distance, withTraffic, possibleSegment.Item2);
                    break;
                }
            }

        if (projection.Item1 > 0.1) projection = ProjectOnLane(projection.Item1, withTraffic, projection.Item2, projection.Item3);
        return projection;
    }

    private Tuple<double, Segment, double> ProjectOnLane(double distance, bool withTraffic, Segment startSegment, double startRoot)
    {
        if(distance < 0) throw  new Exception("ProjectOnLane called with negative distance");

        var currentEvaluatedSegment = startSegment;
        var currentRoot             = startRoot;
        var lastProjection          = new Tuple<double, double>
            (distance * (currentEvaluatedSegment.ParentLane.Direction && !withTraffic || !currentEvaluatedSegment.ParentLane.Direction && withTraffic? 1:-1), currentRoot);
        var limitCount              = 100;

        while (limitCount > 0)
        {
            lastProjection = RoadUtilities.GetProjection(currentEvaluatedSegment.ArcLength, currentEvaluatedSegment.InvArcLength, currentRoot, lastProjection.Item1);


            if (Math.Abs(lastProjection.Item1) <= 0.001) return new Tuple<double, Segment, double>(lastProjection.Item1, currentEvaluatedSegment, lastProjection.Item2);

            Segment next = null;

            foreach (var contact in currentEvaluatedSegment.Contacts)
            {
                if (contact.Target.ParentLane.Id == currentEvaluatedSegment.ParentLane.Id)
                {
                    next = contact.Target;

                    break;
                }
            }

            if (next == null)
                foreach (var contact in currentEvaluatedSegment.Contacts)
                {
                    next = contact.Target;
                    break;
                }

            if (next != null)
            {
                currentEvaluatedSegment = next;
                currentRoot             = currentEvaluatedSegment.ParentLane.Direction ? 1 : 0;
            }
            else
            {
                return new Tuple<double, Segment, double>(lastProjection.Item1, currentEvaluatedSegment, lastProjection.Item2);
            }
            limitCount--;
        }

        return new Tuple<double, Segment, double>(lastProjection.Item1, currentEvaluatedSegment, lastProjection.Item2);
    }

    private Tuple<double, Segment, double> ProjectOnPath(double distance, bool withTraffic, double startRoot)
    {

        var currentEvaluatedSegment = _parentAgent.DesiredIncomingPath[0].Item1;
        var currentRoot             = startRoot;
        var endRoot = currentEvaluatedSegment.ParentLane.Direction && withTraffic || !currentEvaluatedSegment.ParentLane.Direction && !withTraffic ?
                          _parentAgent.DesiredIncomingPath[0].Item2 : _parentAgent.DesiredIncomingPath[0].Item3;

        var lastProjection          = new Tuple<double, double>(distance, currentRoot);

        for (int i = 0; i < _parentAgent.DesiredIncomingPath.Count; i++)
        {

            lastProjection = RoadUtilities.GetProjectionTo(currentEvaluatedSegment.ArcLength, currentEvaluatedSegment.InvArcLength, currentRoot, endRoot, lastProjection.Item1);

            if (Math.Abs(lastProjection.Item1) <= 0.001 || i == _parentAgent.DesiredIncomingPath.Count - 1)
                return new Tuple<double, Segment, double>(lastProjection.Item1, currentEvaluatedSegment, lastProjection.Item2);

            currentEvaluatedSegment = _parentAgent.DesiredIncomingPath[i + 1].Item1;

            if (_parentAgent.DesiredIncomingPath[i + 1].Item1.ParentLane.Direction)
            {
                currentRoot = withTraffic? _parentAgent.DesiredIncomingPath[i + 1].Item3 : _parentAgent.DesiredIncomingPath[i + 1].Item2;
                endRoot = withTraffic ? _parentAgent.DesiredIncomingPath[i + 1].Item2 : _parentAgent.DesiredIncomingPath[i + 1].Item3;
            }
            else
            {
                currentRoot = withTraffic ? _parentAgent.DesiredIncomingPath[i + 1].Item2 : _parentAgent.DesiredIncomingPath[i + 1].Item3;
                endRoot     = withTraffic ? _parentAgent.DesiredIncomingPath[i + 1].Item3 : _parentAgent.DesiredIncomingPath[i + 1].Item2;
            }
        }

        return new Tuple<double, Segment, double>(lastProjection.Item1, currentEvaluatedSegment, lastProjection.Item2);
    }

    internal AgentProfile _profile = new AgentProfile();
    private List<AiBehavior> _behaviors;
}
