using UnityEngine;

internal class PiavRoadConfig : MonoBehaviour
{
    /// <summary> RoadConfig must be present in a scene before generating road meshes. It contains descriptions of individual lane types, and of full roads constructed with said lane types. Both must be filled manually in the Inspector. At least one road template must exist for the road meshes to generate.
    /// </summary>

    [SerializeField] internal LaneTemplate[] _laneTemplates;
    [SerializeField] internal RoadTemplate[] _roadTemplates;
}