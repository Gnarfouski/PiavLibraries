using System.Globalization;
using UnityEngine;
using UnityEditor;

public class PiavAgentSummaryWindow : EditorWindow
{
    private PiavRoadContainer _rc;
    private const float NameIndent = 90f;
    private const float V3Indent = 100f;

    [MenuItem("Window/Active Agents")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow(typeof(PiavAgentSummaryWindow));
    }

    private void OnGUI()
    {
        if (_rc == null) _rc = (PiavRoadContainer)FindObjectOfType(typeof(PiavRoadContainer));

        if (_rc._cars != null && _rc._cars.Count != 0)
        {
            titleContent.text    = "Agents";

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(NameIndent));
            GUILayout.Label("Facing", GUILayout.Width(V3Indent));
            GUILayout.Label("Moving", GUILayout.Width(V3Indent));
            GUILayout.Label("Abs. Speed", GUILayout.Width(NameIndent));
            GUILayout.Label("Dir. Speed", GUILayout.Width(NameIndent));
            GUILayout.Label("Segment", GUILayout.Width(NameIndent));
            GUILayout.Label("Root", GUILayout.Width(NameIndent));
            GUILayout.EndHorizontal();

            foreach (var car in _rc._cars)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(car.name, GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentFacingDirection.ToString(), GUILayout.Width(V3Indent));
                GUILayout.Label(car.CurrentMovingDirection.ToString(), GUILayout.Width(V3Indent));
                GUILayout.Label(car.CurrentAbsoluteSpeed.ToString(CultureInfo.InvariantCulture), GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentDirectionalSpeed.ToString(CultureInfo.InvariantCulture), GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentSegment.Id.ToString(), GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentRoot.ToString(CultureInfo.InvariantCulture), GUILayout.Width(NameIndent));
                GUILayout.EndHorizontal();
            }
        }

        Repaint();
    }
}