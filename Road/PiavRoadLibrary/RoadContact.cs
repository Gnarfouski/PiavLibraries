using System;
using UnityEngine;

#pragma warning disable 649

[Serializable]
internal class RoadContact
{
    /// <summary>A road is a connection between two roads. The joining is done automatically at the xml reconstruction. The contact needs to know which side of origin and target are concerned, and who is the target.
    /// </summary>

    #region Variables

    [SerializeField] internal ContactStartState _start;
    [SerializeField] internal ContactEndState _end;
    [SerializeField] internal PiavRoadInterface _target;

    #endregion
}

internal enum ContactStartState
{
    RoadStart,
    RoadEnd,
}

internal enum ContactEndState
{
    RoadStart,
    RoadEnd,
    RoadSide
}