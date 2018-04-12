using System;
using System.Collections.Generic;
using UnityEngine;

public class PiavRoadAgent : MonoBehaviour
{
    #region Variables

    [Tooltip("Top-down dimensions <width,height>")]
    public Vector2 Dimensions;

    private Vector3 _curPos;

    public Vector3 CurrentPosition
    {
        get { return _curPos; }
        internal set { _curPos = value; }
    }

    private Vector3 _curFdir;

    public Vector3 CurrentFacingDirection
    {
        get { return _curFdir; }
        internal set { _curFdir = value; }
    }

    private Vector3 _curMdir;

    public Vector3 CurrentMovingDirection
    {
        get { return _curMdir; }
        internal set { _curMdir = value; }
    }

    private double _curAbsSp;

    public double CurrentAbsoluteSpeed
    {
        get { return _curAbsSp; }
        internal set { _curAbsSp = value; }
    }

    private double _curDirSp;

    public double CurrentDirectionalSpeed
    {
        get { return _curDirSp; }
        internal set { _curDirSp = value; }
    }

    private Segment _curSgm;

    public Segment CurrentSegment
    {
        get { return _curSgm; }
        internal set { _curSgm = value; }
    }

    private double _curRt;

    public double CurrentRoot
    {
        get { return _curRt; }
        internal set { _curRt = value; }
    }

    // ****************************

    [HideInInspector, Range(-1, 1)] public double DesiredThrottle;
    [HideInInspector, Range(-1, 1)] public double DesiredSteer;
    [HideInInspector, Range(0, 3)] public int DesiredHeadlightState;
    [HideInInspector] public bool DesiredBrakelightState;
    [HideInInspector, Tooltip("<left,right>")] public Tuple<bool, bool> DesiredTurnSignalState;
    [HideInInspector] public bool DesiredHornState;
    [HideInInspector] public bool DesiredSirenState;
    [HideInInspector] public List<Tuple<Segment,double,double>> DesiredIncomingPath = new List<Tuple<Segment, double, double>>();

    #endregion
}