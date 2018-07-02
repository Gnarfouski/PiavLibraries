using System;
using System.Collections.Generic;
using UnityEngine;

public class PiavRoadAgent : MonoBehaviour
{
    #region Variables

    [Tooltip("Agent dimensions <width,height>")]
    public Vector2 Dimensions;
    [Tooltip("Length of fixed items attached to the agent, such as a baby carrier or a trailer <pushed,towed>")]
    public Vector2 AdditionalLength;

    [Tooltip("Segment, root, distance, heuristic")]
    private List<PiavRoadContainer.ValuedSegment> _curSgm;
    private double  _curAbsSp = double.NaN;
    //private Vector3 _curMdir;
    private Vector3 _curFdir;
    private Vector3 _curPos;
    private PiavRoadContainer.StoppingDataPackage _curStops;

    internal PiavRoadContainer.StoppingDataPackage CurrentStops
    {
        get { return _curStops; }
        set { _curStops = value; }
    }

    internal Vector3 CurrentPosition
    {
        get { return _curPos; }
        set { _curPos = value; }
    }

    internal Vector3 CurrentFacingDirection
    {
        get { return _curFdir; }
        set { _curFdir = value; }
    }
    /*
    internal Vector3 CurrentMovingDirection
    {
        get { return _curMdir; }
        set { _curMdir = value; }
    }
    */
    internal double CurrentAbsoluteSpeed
    {
        get { return _curAbsSp; }
        set { _curAbsSp = value; }
    }

    internal List<PiavRoadContainer.ValuedSegment> CurrentSegments
    {
        get { return _curSgm; }
        set { _curSgm = value; }
    }

    // ****************************

    [HideInInspector, Range(-1, 1)] public double DesiredThrottle;
    [HideInInspector, Range(-1, 1)] public double DesiredSteer;
    [HideInInspector, Range(0, 3)] public int DesiredHeadlightState;
    [HideInInspector] public bool DesiredBrakelightState;
    [HideInInspector] public bool DesiredLeftTurnSignalState;
    [HideInInspector] public bool DesiredRightTurnSignalState;
    [HideInInspector] public bool DesiredHornState;
    [HideInInspector] public bool DesiredSirenState;
    [HideInInspector] internal List<DualVertex.SegmentPart> _desiredIncomingPath = new List<DualVertex.SegmentPart>();

    #endregion


}