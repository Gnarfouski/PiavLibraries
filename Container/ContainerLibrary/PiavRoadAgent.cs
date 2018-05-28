using System;
using System.Collections.Generic;
using UnityEngine;

public class PiavRoadAgent : MonoBehaviour
{
    #region Variables

    [Tooltip("Top-down dimensions <width,height>")]
    public Vector2 Dimensions;

    [Tooltip("Segment, root, distance, currentDot")]
    private List<Tuple<Segment, double,double,double>> _curSgm;
    private double  _curAbsSp = double.NaN;
    //private Vector3 _curMdir;
    private Vector3 _curFdir;
    private Vector3 _curPos;

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

    internal List<Tuple<Segment,double,double,double>> CurrentSegments
    {
        get { return _curSgm; }
        set { _curSgm = value; }
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