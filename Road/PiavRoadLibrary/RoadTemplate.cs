using System;
using UnityEngine;

#pragma warning disable 649

[Serializable]
internal class RoadTemplate
{
    /// <summary>A road template is a description of a full road type, built with individual lane templates and directions.
    /// </summary>

    #region Variables

    [SerializeField]
    internal string _name;

    [SerializeField, Tooltip("Expressed in meters per second")]
    internal int _speedLimit;

    [SerializeField]
    internal string[] _laneTemplateNames;

    [SerializeField, Tooltip("False : Normal direction.\nTrue : Reverse direction")]
    internal bool[] _roadDirections;

    [SerializeField]
    internal LaneType[] _laneTypes;

    #endregion
}

internal enum LaneType
{
    Pedestrian,
    Vehicle
}