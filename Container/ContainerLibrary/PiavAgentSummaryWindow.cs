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
            GUILayout.Label("AS", GUILayout.Width(DigitIndent));
            GUILayout.Label("Segment", GUILayout.Width(NameIndent));
            GUILayout.Label("Root", GUILayout.Width(DigitIndent));
            GUILayout.Label("Distance", GUILayout.Width(DigitIndent));
            GUILayout.Label("Heuristic", GUILayout.Width(DigitIndent));
            GUILayout.EndHorizontal();

            foreach (var car in _rc._cars)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(car.name, GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentFacingDirection.ToString(), GUILayout.Width(V3Indent));
                GUILayout.Label(!double.IsNaN(car.CurrentAbsoluteSpeed)? car.CurrentAbsoluteSpeed.ToString("n2",CultureInfo.InvariantCulture):"X", GUILayout.Width(DigitIndent));
                GUILayout.Label(car.CurrentSegments.Count != 0 ? car.CurrentSegments[0].Item1.Id.ToString() : "X", GUILayout.Width(NameIndent));
                GUILayout.Label(car.CurrentSegments.Count != 0 ? car.CurrentSegments[0].Item2.ToString("n2",CultureInfo.InvariantCulture) : "X", GUILayout.Width(DigitIndent));
                GUILayout.Label(car.CurrentSegments.Count != 0 ? car.CurrentSegments[0].Item3.ToString("n2", CultureInfo.InvariantCulture) : "X", GUILayout.Width(DigitIndent));
                GUILayout.Label(car.CurrentSegments.Count != 0 ? car.CurrentSegments[0].Item4.ToString("n2", CultureInfo.InvariantCulture) : "X", GUILayout.Width(DigitIndent));
                GUILayout.EndHorizontal();

                if (car.CurrentSegments.Count > 1)
                    for (int i = 1; i < car.CurrentSegments.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", GUILayout.Width(NameIndent + V3Indent + DigitIndent));
                        GUILayout.Label(car.CurrentSegments[i].Item1.Id.ToString(), GUILayout.Width(NameIndent));
                        GUILayout.Label(car.CurrentSegments[i].Item2.ToString("n2", CultureInfo.InvariantCulture), GUILayout.Width(DigitIndent));
                        GUILayout.Label(car.CurrentSegments[i].Item3.ToString("n2", CultureInfo.InvariantCulture), GUILayout.Width(DigitIndent));
                        GUILayout.Label(car.CurrentSegments[i].Item4.ToString("n2", CultureInfo.InvariantCulture), GUILayout.Width(DigitIndent));
                        GUILayout.EndHorizontal();
                    }
            }

        }

        Repaint();
    }
}