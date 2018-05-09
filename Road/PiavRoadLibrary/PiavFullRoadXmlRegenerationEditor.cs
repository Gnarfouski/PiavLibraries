using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PiavFullRoadXmlRegeneration))]
internal class PiavFullRoadXmlRegenerationEditor : Editor
{
    /// <inheritdoc />
    /// <summary>"Regenerate" Button on the FullRoadXmlRegeneration monobehavior.</summary>

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var c = (PiavFullRoadXmlRegeneration)target;
        if (GUILayout.Button("Regenerate")) c.Regenerate();
    }
}