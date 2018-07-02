using System;
using System.Collections.Generic;
using UnityEngine;

internal abstract class AiBehavior
{
    protected AiBehavior(PiavCoreAiInput input)
    {
        _parentAi = input;
        _parentContainer = UnityEngine.Object.FindObjectOfType<PiavRoadContainer>();
    }

    protected void SetProjectionGizmo(List<Vector3> target, Color color)
    {
        _parentContainer._gizmosRequests.Add(new PiavRoadContainer.RequestedGizmo { _points = target, _origin = GetType(), _requestedColor = color });
    }

    internal abstract void Update(PiavRoadAgent agent);

    protected PiavRoadContainer _parentContainer;
    protected PiavCoreAiInput _parentAi;
}