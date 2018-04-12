using System;
using UnityEngine;

#pragma warning disable 649

[Serializable]
internal class LaneTemplate
{
    /// <summary>A lane template is a description of a single lane of traffic or sidewalk.</summary>
    [SerializeField] internal string _name;
    [SerializeField] internal Material _material;
    [SerializeField, Range(1,15), Tooltip("Approximate real-life size of texture (in meters)")]
    internal int _materialSizeReference = 1;
    [SerializeField] internal double _width;
}