using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class BehaviorFindPath : AiBehavior
{

    private List<DualVertex.SegmentPart> GetPath(Segment startSegment, double startRoot, Segment endSegment, double endRoot)
    {
        var startVertex = FindDualVertex(startSegment, startRoot);
        var endVertex = FindDualVertex(endSegment, endRoot);
        //Debug.Log(startVertex.Id + " " + endVertex.Id);

        foreach (var dualVertex in _parentContainer._dualGraph)
        {
            dualVertex.AccumulatedDistance = double.NaN;
            dualVertex.PathPredecessor = null;
        }

        startVertex.AccumulatedDistance = startVertex.Distance;

        var toSearch = new List<DualVertex> { startVertex };

        while (toSearch.Count != 0)
        {
            if (toSearch[0] == endVertex)
                if (startVertex._parentSegmentPart._parentSegment.ParentLane.Direction && endRoot <= startRoot || !startVertex._parentSegmentPart._parentSegment.ParentLane.Direction && endRoot >= startRoot)
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

        return new List<DualVertex.SegmentPart>();
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

        foreach (var sdv in segment.DualVertices) if (root >= sdv._parentSegmentPart._fromRoot && root <= sdv._parentSegmentPart._toRoot) return sdv;

        Debug.LogWarning(segment.Id + " " + root + " " + segment.DualVertices.Count);
        foreach (var sdv in segment.DualVertices) Debug.Log(sdv._parentSegmentPart._fromRoot + " " + sdv._parentSegmentPart._toRoot);
        return null;
    }

    // ReSharper disable once UnusedMember.Local
    private void PrintDualGraph()
    {
        string s = "";

        foreach (var dv in _parentContainer._dualGraph)
            s += "\n" + dv.Id + " " + (dv.PathPredecessor?.Id.ToString() ?? "X") + " " + dv.AccumulatedDistance + " " + dv._parentSegmentPart._parentSegment.Id + " " + dv._parentSegmentPart._fromRoot + " " + dv._parentSegmentPart._toRoot;

        Debug.Log(s);
    }

    private List<DualVertex.SegmentPart> BuildPath(DualVertex check)
    {
        var resPath = new List<DualVertex.SegmentPart>();

        var last = check;
        resPath.Add(check._parentSegmentPart);
        if (last.Predecessors == null) return null;

        //*
        while (last.PathPredecessor != null)
        {
            resPath.Add(last.PathPredecessor._parentSegmentPart);
            last = last.PathPredecessor;
        }

        var newList = new DualVertex.SegmentPart[resPath.Count];
        for (int i = resPath.Count - 1; i >= 0; i--) newList[resPath.Count - 1 - i] = resPath[i];

        return newList.ToList();
    }

    internal override void Update(PiavRoadAgent agent)
    {
        if (_parentContainer._dualGraph != null && agent.CurrentSegments != null && agent.CurrentSegments.Count != 0)
            if (_parentAi._profile._needTargetDestination)

            {
                var s = agent.CurrentSegments[0];

                agent._desiredIncomingPath = GetPath(
                                                    s._segment,
                                                    s._root,
                                                    _parentAi._profile._targetDestination._segment,
                                                    _parentAi._profile._targetDestination._root);
                _parentAi._profile._needTargetDestination = false;
            }
    }

    /// <inheritdoc />
    public BehaviorFindPath(PiavCoreAiInput input)
        : base(input)
    { }
}