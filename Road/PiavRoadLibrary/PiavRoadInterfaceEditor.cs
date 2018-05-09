using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PiavRoadInterface))]
internal class PiavRoadInterfaceEditor : Editor
{
    /// <summary>The Road Interface Editor :
    /// - shows a button in the inspector to select used Road Template
    /// - shows a menu option for creating road meshes
    /// </summary>

    #region GameObject Handle

    // Prevent the Gameobject handle to appear, as it makes difficult selecting point handles in the scene GUI

    private Tool _lastTool = Tool.None;

    private void OnEnable()
    {
        _lastTool      = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        Tools.current = _lastTool;
    }

    #endregion

    // Menu item for roadmesh creation
    [MenuItem("GameObject/Create Other/RoadMesh")]
    private static void CreateRoadMesh()
    {
        var roadConfig = FindObjectOfType(typeof(PiavRoadConfig)) as PiavRoadConfig;

        if (null != roadConfig)
            if (null != roadConfig._roadTemplates && roadConfig._roadTemplates.Length > 0)
            {
                var roadObj = new GameObject("Road")
                {
                    isStatic = true,
                    layer    = LayerMask.NameToLayer("Road")
                };
                var newRoadMesh  = roadObj.AddComponent(typeof(PiavRoadInterface)) as PiavRoadInterface;

                if (newRoadMesh != null)
                {
                    newRoadMesh._savedTemplateIndex = 0;
                    newRoadMesh.LoadRoadMesh();
                    newRoadMesh.RegenerateMesh();
                }
                else
                {
                    throw new NullReferenceException();
                }
                Selection.activeGameObject = roadObj;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No Road template found in RoadConfig", "Ok");
            }
        else
            EditorUtility.DisplayDialog("Error", "RoadConfig not found in scene", "Ok");

    }

    // Scene handles for anchor building
    private void OnSceneGUI()
    {
        var roadMesh = (PiavRoadInterface)target;

        if (null != roadMesh.AnchorPoints && roadMesh.enabled)
        {
            //regenerate if one of the existing points is moved
            for (var i = 0; i < roadMesh.AnchorPoints.Length; i++)
            {
                var oldPoint = roadMesh.AnchorPoints[i];
                var newPoint = Handles.DoPositionHandle(oldPoint, Quaternion.identity);

                if (newPoint != oldPoint)
                {
                    roadMesh.AnchorPoints[i] = newPoint;
                    roadMesh.RegenerateMesh();

                    break;
                }
            }

            //regenerate if the projection for a new point is moved
            var proj = 2 * roadMesh.AnchorPoints[roadMesh.AnchorPoints.Length - 1] - roadMesh.AnchorPoints[roadMesh.AnchorPoints.Length - 2];
            var addedPoint    = Handles.DoPositionHandle(proj, Quaternion.identity);

            if (addedPoint != proj)
            {
                var pointList = new List<Vector3>(roadMesh.AnchorPoints) { proj };
                roadMesh.AnchorPoints = pointList.ToArray();
                roadMesh.RegenerateMesh();
            }
        }
    }

    // button for switching to a different RoadTemplate from the adjacent RoadConfig
    public override void OnInspectorGUI()
    {
        var roadMesh = target as PiavRoadInterface;
        var roadConfig = FindObjectOfType(typeof(PiavRoadConfig)) as PiavRoadConfig;

        if (null == roadConfig)
        {
            EditorGUILayout.HelpBox("RoadConfig not found", MessageType.Error);

            return;
        }

        if (null != roadConfig._roadTemplates && roadConfig._roadTemplates.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply template") && roadMesh != null)
            {
                var popupContents                                                               = new GUIContent[roadConfig._roadTemplates.Length];
                for (var i = 0; i < roadConfig._roadTemplates.Length; ++i) popupContents[i] = new GUIContent(roadConfig._roadTemplates[i]._name);

                EditorUtility.DisplayCustomMenu(
                                                new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0),
                                                popupContents,
                                                -1,
                                                delegate(object obj, string[] contentStrings, int selected)
                                                {
                                                    if (selected >= 0 && selected < roadConfig._roadTemplates.Length)
                                                    {
                                                        roadMesh._savedTemplateIndex = selected;
                                                        roadMesh.LoadRoadMesh();
                                                        roadMesh.RegenerateMesh();
                                                    }
                                                },
                                                null
                                               );
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3f);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck() && roadMesh != null)
            {
                if(roadMesh.AnchorPoints.Length < 2)
                    roadMesh.AnchorPoints = roadMesh.AnchorPoints.Length == 1 ?
                                                new[] { roadMesh.AnchorPoints[0], roadMesh.AnchorPoints[0] + 10 * Vector3.forward + 10 * Vector3.right} :
                                                new[] { Vector3.zero, 10 * Vector3.forward + 10 * Vector3.right};
                roadMesh.LoadRoadMesh();
                roadMesh.RegenerateMesh();
            }
        }
    }
}