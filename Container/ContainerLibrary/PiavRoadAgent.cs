using System;
using System.Collections.Generic;
using UnityEngine;

public class PiavRoadAgent : MonoBehaviour
{
    #region Variables

    [Tooltip("Top-down dimensions <width,height>")]
    public Vector2 Dimensions;

    private double _curRt = double.NaN;
    private Segment _curSgm;
    private double  _curDirSp = double.NaN;
    private double  _curAbsSp = double.NaN;
    private Vector3? _curMdir;
    private Vector3? _curFdir;
    private Vector3? _curPos;

    public Vector3? CurrentPosition
    {
        get { return _curPos; }
        internal set { _curPos = value; }
    }
    public Vector3? CurrentFacingDirection
    {
        get { return _curFdir; }
        internal set { _curFdir = value; }
    }
    public Vector3? CurrentMovingDirection
    {
        get { return _curMdir; }
        internal set { _curMdir = value; }
    }
    public double CurrentAbsoluteSpeed
    {
        get { return _curAbsSp; }
        internal set { _curAbsSp = value; }
    }
    public double CurrentDirectionalSpeed
    {
        get { return _curDirSp; }
        internal set { _curDirSp = value; }
    }
    public Segment CurrentSegment
    {
        get { return _curSgm; }
        internal set { _curSgm = value; }
    }
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