using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class BehaviorFindPath : AiBehavior
{

    private List<Tuple<Segment, double, double>> GetPath(Segment startSegment, double startRoot, Segment endSegment, double endRoot)
    {
        var startVertex = FindDualVertex(startSegment, startRoot);
        var endVertex = FindDualVertex(endSegment, endRoot);
        //Debug.Log(startVertex.Id + " " + endVertex.Id);

        foreach (var dualVertex in _parentContainer.DualGraph)
        {
            dualVertex.AccumulatedDistance = double.NaN;
            dualVertex.PathPredecessor = null;
        }

        startVertex.AccumulatedDistance = startVertex.Distance;

        var toSearch = new List<DualVertex> { startVertex };

        while (toSearch.Count != 0)
        {
            if (toSearch[0] == endVertex)
                if (startVertex.ParentSegmentPart.Item1.ParentLane.Direction && endRoot <= startRoot || !startVertex.ParentSegmentPart.Item1.ParentLane.Direction && endRoot >= startRoot)
                    return BuildPath(endVertex);

            foreach (var c in toSearch[0].Followers)
            {
                if (double.IsNaN(c.AccumulatedDistance) || c.AccumulatedDistance > toSearch[0].AccumulatedDistance + c.Distance)
                {
                    c.AccumulatedDistance = toSearch[0].AccumulatedDistance + c.Distance;
                    c.PathPredecessor     = toSearch[0];
                    toSearch.Add(c);
                }
            }
            toSearch.RemoveAt(0);
            toSearch.Sort((a, b) => a.AccumulatedDistance.CompareTo(b.AccumulatedDistance));
        }

        return new List<Tuple<Segment, double, double>>();
    }

    private DualVertex FindDualVertex(Segment segment, double root)
    {
        if (root < 0)
        {
            root = 0;
            Debug.LogWarning("FindDualVertex called with root " + root + ". Setting to 0");
        }
        if (root > 1)
        {
            root = 1;
            Debug.LogWarning("FindDualVertex called with root " + root + ". Setting to 1");
        }

        foreach (var sdv in segment.DualVertices) if (root >= sdv.ParentSegmentPart.Item2 && root <= sdv.ParentSegmentPart.Item3) return sdv;

        Debug.LogWarning(segment.Id + " " + root + " " + segment.DualVertices.Count);
        foreach (var sdv in segment.DualVertices) Debug.Log(sdv.ParentSegmentPart.Item2 + " " + sdv.ParentSegmentPart.Item3);
        return null;
    }

    // ReSharper disable once UnusedMember.Local
    private void PrintDualGraph()
    {
        string s = "";

        foreach (var dv in _parentContainer.DualGraph)
            s += "\n" + dv.Id + " " + (dv.PathPredecessor?.Id.ToString() ?? "X") + " " + dv.AccumulatedDistance + " " + dv.ParentSegmentPart.Item1.Id + " " + dv.ParentSegmentPart.Item2 + " " + dv.ParentSegmentPart.Item3;

        Debug.Log(s);
    }

    private List<Tuple<Segment, double, double>> BuildPath(DualVertex check)
    {
        var resPath = new List<Tuple<Segment, double, double>>();

        var last = check;
        resPath.Add(check.ParentSegmentPart);
        if (last.Predecessors == null) return null;

        //*
        while (last.PathPredecessor != null)
        {
            resPath.Add(last.PathPredecessor.ParentSegmentPart);
            last = last.PathPredecessor;
        }

        var newList = new Tuple<Segment, double, double>[resPath.Count];
        for (int i = resPath.Count - 1; i >= 0; i--) newList[resPath.Count - 1 - i] = resPath[i];

        return newList.ToList();
    }

    internal override void Update(PiavRoadAgent agent)
    {
        if (_parentContainer.DualGraph != null && agent.CurrentSegments != null && agent.CurrentSegments.Count != 0)
            if (_parentAi._profile._needTargetDestination)

            {
                var s = agent.CurrentSegments[0];

                agent.DesiredIncomingPath = GetPath(
                                                    s.Item1,
                                                    s.Item2,
                                                    _parentAi._profile._targetDestination.Item1,
                                                    _parentAi._profile._targetDestination.Item2);
                _parentAi._profile._needTargetDestination = false;
            }
    }

    /// <inheritdoc />
    public BehaviorFindPath(PiavCoreAiInput input)
        : base(input)
    { }
}