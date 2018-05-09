using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;

public class PiavAgentSummaryWindow : EditorWindow
{
    private PiavRoadContainer _rc;
    private const float NameIndent = 90f;
    private const float V3Indent = 100f;
    private const float DigitIndent = 40f;

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
            GUILayout.Label("AS", GUILayout.Width(DigitIndent));
            GUILayout.Label("DS", GUILayout.Width(DigitIndent));
            GUILayout.Label("Segment", GUILayout.Width(NameIndent));
            GUILayout.Label("Root", GUILayout.Width(DigitIndent));
            GUILayout.EndHorizontal();

            foreach (var car in _rc._cars)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(car.name, GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentFacingDirection.HasValue? car.CurrentFacingDirection.ToString():"X", GUILayout.Width(V3Indent));
                GUILayout.Label(car.CurrentMovingDirection.HasValue? car.CurrentMovingDirection.ToString():"X", GUILayout.Width(V3Indent));
                GUILayout.Label(!double.IsNaN(car.CurrentAbsoluteSpeed)? car.CurrentAbsoluteSpeed.ToString("n2",CultureInfo.InvariantCulture):"X", GUILayout.Width(DigitIndent));
                GUILayout.Label(!double.IsNaN(car.CurrentDirectionalSpeed)? car.CurrentDirectionalSpeed.ToString("n2",CultureInfo.InvariantCulture):"X", GUILayout.Width(DigitIndent));
                GUILayout.Label(car.CurrentSegment?.Id.ToString() ?? "X", GUILayout.Width(NameIndent));
                GUILayout.Label(!double.IsNaN(car.CurrentRoot)? car.CurrentRoot.ToString("n2",CultureInfo.InvariantCulture):"X", GUILayout.Width(DigitIndent));
                GUILayout.EndHorizontal();
            }
        }

        Repaint();
    }
}