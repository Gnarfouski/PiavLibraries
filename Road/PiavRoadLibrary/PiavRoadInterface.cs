﻿using System;
using UnityEngine;

[ExecuteInEditMode]
internal class PiavRoadInterface : MonoBehaviour
{
    private void Awake()
    {
        var roadConfig = FindObjectOfType(typeof(PiavRoadConfig)) as PiavRoadConfig;

        if (null != roadConfig)
            if (null != roadConfig._roadTemplates && roadConfig._roadTemplates.Length > 0)
            {
                _savedTemplateIndex = 0;
                LoadRoadMesh();
                RegenerateMesh();
            }
        else
            {
                Debug.LogError("No Road template found in RoadConfig");
            }
        else
            Debug.LogError("RoadConfig not found in scene");
    }

    /// <summary> Keep the Gameobject transform at zero to not have to transform local points to global
    /// </summary>
    private void Update()
    {
        if (transform.position != Vector3.zero) transform.position = Vector3.zero;
        if (transform.rotation.eulerAngles != Vector3.zero) transform.rotation = Quaternion.Euler(Vector3.zero);
    }

    /// <summary> Create and assign a new 3D mesh from the points
    /// </summary>
    internal void RegenerateMesh()
    {
        RegenerateDividers();
        RegenerateLanes();

        var newMesh = RoadMeshGeneration.GenerateMesh(_dividerPolynoms, _laneMaterialSizes);

        var meshFilter                          = GetComponent(typeof(MeshFilter)) as MeshFilter;
        if (null == meshFilter) meshFilter      = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
        if (null != meshFilter) meshFilter.mesh = newMesh;

        var meshCollider                                  = GetComponent(typeof(MeshCollider)) as MeshCollider;
        if (null == meshCollider) meshCollider            = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        if (null != meshCollider) meshCollider.sharedMesh = newMesh;

        var meshRenderer                       = GetComponent(typeof(MeshRenderer)) as MeshRenderer;
        if (null == meshRenderer) meshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        if (null != meshRenderer)
        {
            meshRenderer.receiveShadows = true;
            meshRenderer.sharedMaterials = _laneMaterials;
        }
    }

    /// <summary> Generate (number lanes + 1) piecewise polynoms surrounding the lanes. They will be used for the 3D mesh' vertices placing
    /// </summary>
    private void RegenerateDividers()
    {
        _dividerPolynoms = new QuadraticPolynomial[_laneWidths.Length + 1, _anchorPoints.Length - 1];
        var centralPPolynom = PathCentralPPolynom(_anchorPoints);

        // Compute Cross Vectors

        var centralCrossValues = new Vector3[_anchorPoints.Length];
        centralCrossValues[0] = Vector3.Cross(centralPPolynom[0].CalculateFirstDerivative(0), Vector3.up);
        centralCrossValues[0].y = 0;
        centralCrossValues[0].Normalize();

        for (var i = 0; i < centralPPolynom.Length; i++)
        {
            centralCrossValues[i + 1] = Vector3.Cross(centralPPolynom[i].CalculateFirstDerivative(1), Vector3.up);
            centralCrossValues[i + 1].y = 0;
            centralCrossValues[i + 1].Normalize();
        }

        // Compute Cross Divider Distances

        double totalRoadWidth = 0;
        foreach (var f in _laneWidths) totalRoadWidth += f;

        var dividerDistances = new double[_laneWidths.Length + 1];
        dividerDistances[0] = totalRoadWidth / 2;

        for (var i = 1; i < dividerDistances.Length; i++) dividerDistances[i] = dividerDistances[i - 1] - _laneWidths[i - 1];

        // Compute Divider-wise Points

        var dividerPoints = new Vector3[_laneWidths.Length + 1, _anchorPoints.Length];

        for (var i = 0; i <= _laneWidths.Length; i++)
        {
            for (var j = 0; j < _anchorPoints.Length; j++) dividerPoints[i, j] = _anchorPoints[j] + centralCrossValues[j] * (float)dividerDistances[i];
        }

        // Compute Divider polynoms - We cannot path the divider polys the same way we did with the central one (see online docs). We use relative point distance to scale the central polynoms

        for (var i = 0; i <= _laneWidths.Length; i++)
        {
            for (var j = 0; j < _anchorPoints.Length - 1; j++)
            {
                var dist = _anchorPoints[j + 1] - _anchorPoints[j];
                var ddist = dividerPoints[i, j + 1] - dividerPoints[i, j];

                var ponderation = new Vector3
                {
                    x = Mathf.Abs(dist.x) < 0.001f ? 0 : ddist.x / dist.x,
                    y = Mathf.Abs(dist.y) < 0.001f ? 0 : ddist.y / dist.y,
                    z = Mathf.Abs(dist.z) < 0.001f ? 0 : ddist.z / dist.z
                };

                if (j == 0)
                {
                    _dividerPolynoms[i, j] = new QuadraticPolynomial
                    {
                        _coeffs =
                        {
                            [0, 0] = centralPPolynom[j]._coeffs[0, 0],
                            [0, 1] = centralPPolynom[j]._coeffs[0, 1],
                            [0, 2] = centralPPolynom[j]._coeffs[0, 2],
                            [1, 0] = centralPPolynom[j]._coeffs[1, 0],
                            [1, 1] = centralPPolynom[j]._coeffs[1, 1],
                            [1, 2] = centralPPolynom[j]._coeffs[1, 2],
                            [2, 0] = dividerPoints[i, j].x,
                            [2, 1] = dividerPoints[i, j].y,
                            [2, 2] = dividerPoints[i, j].z
                        }
                    };

                }
                else
                {
                    var aix = centralPPolynom[j - 1]._coeffs[0, 0];
                    var aiy = centralPPolynom[j - 1]._coeffs[0, 1];
                    var aiz = centralPPolynom[j - 1]._coeffs[0, 2];

                    var bix = centralPPolynom[j - 1]._coeffs[1, 0];
                    var biy = centralPPolynom[j - 1]._coeffs[1, 1];
                    var biz = centralPPolynom[j - 1]._coeffs[1, 2];

                    _dividerPolynoms[i, j] = new QuadraticPolynomial
                    {
                        _coeffs =
                        {
                            [0, 0] = dividerPoints[i, j + 1].x - dividerPoints[i, j].x - ponderation.x * (2 * aix + bix),
                            [0, 1] = dividerPoints[i, j + 1].y - dividerPoints[i, j].y - ponderation.y * (2 * aiy + biy),
                            [0, 2] = dividerPoints[i, j + 1].z - dividerPoints[i, j].z - ponderation.z * (2 * aiz + biz),
                            [1, 0] = ponderation.x * (2 * aix + bix),
                            [1, 1] = ponderation.y * (2 * aiy + biy),
                            [1, 2] = ponderation.z * (2 * aiz + biz),
                            [2, 0] = dividerPoints[i, j].x,
                            [2, 1] = dividerPoints[i, j].y,
                            [2, 2] = dividerPoints[i, j].z
                        }
                    };
                }
            }
        }
    }

    /// <summary> Generate one piecewise polynom per lane. They will be used to write the navigation xml file.
    /// </summary>
    private void RegenerateLanes()
    {
        _lanePolynoms = new QuadraticPolynomial[_dividerPolynoms.GetLength(0) - 1, _dividerPolynoms.GetLength(1)];
        for (var i = 0; i < _dividerPolynoms.GetLength(0) - 1; i++)
        {
            for (var j = 0; j < _dividerPolynoms.GetLength(1); j++) _lanePolynoms[i, j] = (_dividerPolynoms[i, j] + _dividerPolynoms[i + 1, j]) / 2;
        }
    }

    /// <summary> Generate one piecewise polynom from 3D points. It will be the base for divider ppolynoms
    /// </summary>
    private QuadraticPolynomial[] PathCentralPPolynom(Vector3[] points)
    {
        var res = new QuadraticPolynomial[points.Length - 1];

        res[0] = new QuadraticPolynomial
        {
            _coeffs =
            {
                [1, 0] = points[1].x - points[0].x,
                [1, 1] = points[1].y - points[0].y,
                [1, 2] = points[1].z - points[0].z,
                [2, 0] = points[0].x,
                [2, 0] = points[0].y,
                [2, 0] = points[0].z
            }
        };

        for (var i = 1; i < points.Length - 1; i++)
        {
            res[i] = new QuadraticPolynomial
            {
                _coeffs =
                {
                    [0, 0] = points[i + 1].x - points[i].x - 2 * res[i - 1]._coeffs[0, 0] - res[i - 1]._coeffs[1, 0],
                    [0, 1] = points[i + 1].y - points[i].y - 2 * res[i - 1]._coeffs[0, 1] - res[i - 1]._coeffs[1, 1],
                    [0, 2] = points[i + 1].z - points[i].z - 2 * res[i - 1]._coeffs[0, 2] - res[i - 1]._coeffs[1, 2],

                    [1, 0] = 2 * res[i - 1]._coeffs[0, 0] + res[i - 1]._coeffs[1, 0],
                    [1, 1] = 2 * res[i - 1]._coeffs[0, 1] + res[i - 1]._coeffs[1, 1],
                    [1, 2] = 2 * res[i - 1]._coeffs[0, 2] + res[i - 1]._coeffs[1, 2],

                    [2, 0] = points[i].x,
                    [2, 1] = points[i].y,
                    [2, 2] = points[i].z
                }
            };
        }

        return res;
    }

    /// <summary> Use quadratic regression to approximate arc length and inverse arc length functions for faster computation. They will be used in the XML navigation file.
    /// </summary>
    internal void LoadRegressedFunctions()
    {
        _arcLengthRegressionValues = new double[_lanePolynoms.GetLength(0),_lanePolynoms.GetLength(1),2,3];
        var steps  = new double[11];
        var values = new double[11];

        double maxError = 0.0;
        double minError = double.PositiveInfinity;
        double avgError = 0.0;
        int errorCount = 0;

        for (int j = 0; j < _lanePolynoms.GetLength(0); j++)
        {
            for (int k = 0; k < _lanePolynoms.GetLength(1); k++)
            {
                for (var i = 0; i < 11; i++)
                {
                    steps[i]  = i / 10d;
                    values[i] = _lanePolynoms[j,k].ArcLength(0.0, steps[i]);
                }

                if (_lanePolynoms[j, k]._coeffs[0, 0] < 0.0001 && _lanePolynoms[j, k]._coeffs[0, 1] < 0.0001 && _lanePolynoms[j, k]._coeffs[0, 2] < 0.0001)
                {
                    _arcLengthRegressionValues[j, k, 0, 0] = 0;
                    _arcLengthRegressionValues[j, k, 0, 1] = 0;
                    _arcLengthRegressionValues[j, k, 0, 2] = _lanePolynoms[j, k].ArcLength(0.0, 1.0);

                    _arcLengthRegressionValues[j, k, 1, 0] = 0;
                    _arcLengthRegressionValues[j, k, 1, 1] = 0;
                    _arcLengthRegressionValues[j, k, 1, 2] = 1 / _arcLengthRegressionValues[j, k, 0, 2];
                }
                else
                {
                    var arc = AdHocGradientDescent.PolyfitRegression(steps, values);
                    //var arc                                = AdHocGradientDescent.CubicRegressionBoldDriver(steps, values, 500000, 0.1, 10,10,20);
                    _arcLengthRegressionValues[j, k, 0, 0] = arc.Item1; // * _lanePolynoms[j, k].ArcLength(0.0, 1.0);
                    _arcLengthRegressionValues[j, k, 0, 1] = arc.Item2; // * _lanePolynoms[j, k].ArcLength(0.0, 1.0);
                    _arcLengthRegressionValues[j, k, 0, 2] = arc.Item3;

                    var invArc = AdHocGradientDescent.PolyfitRegression(values, steps);
                    //var invArc                             = AdHocGradientDescent.CubicRegressionBoldDriver(values, steps, 500000, 0.00005,0.1,0.1,1);
                    _arcLengthRegressionValues[j, k, 1, 0] = invArc.Item1;
                    _arcLengthRegressionValues[j, k, 1, 1] = invArc.Item2;
                    _arcLengthRegressionValues[j, k, 1, 2] = invArc.Item3;
                }

                var error = RunRegressionErrorTest(j, k);
                if (error > maxError) maxError = error;
                if (error < minError) minError = error;
                avgError += error;
                errorCount++;
            }
        }

        avgError /= errorCount;
        Debug.Log("Avg : " + avgError + " | Min : " + minError + " | Max : " + maxError);
    }

    private double RunRegressionErrorTest(int j, int k)
    {
        var dif = 0.0;
        for (double d = 0.0; d <= 1.0; d += 0.01)
        {
            var alen = _arcLengthRegressionValues[j, k, 0, 0] * d * d * d + _arcLengthRegressionValues[j, k, 0, 1] * d * d + _arcLengthRegressionValues[j, k, 0, 2] * d;
            var ilen = _arcLengthRegressionValues[j, k, 1, 0] * alen * alen * alen + _arcLengthRegressionValues[j, k, 1, 1] * alen * alen + _arcLengthRegressionValues[j, k, 1, 2] * alen;
            dif += Math.Abs(ilen - d);
        }
        dif /= 101;
        return dif;
    }

    /// <summary> Reload the correct data from RoadConfig with the saved template index.
    /// </summary>
    internal void LoadRoadMesh()
    {
        var roadConfig = FindObjectOfType(typeof(PiavRoadConfig)) as PiavRoadConfig;

        if (roadConfig != null)
        {
            _laneDirections = roadConfig._roadTemplates[_savedTemplateIndex]._roadDirections;
            _laneMaterials  = new Material[_laneDirections.Length];
            _laneWidths     = new double[_laneDirections.Length];
            _laneMaterialSizes = new double[_laneDirections.Length];
            _speedLimit     = roadConfig._roadTemplates[_savedTemplateIndex]._speedLimit;
            _laneTypes = roadConfig._roadTemplates[_savedTemplateIndex]._laneTypes;

            for (int j = 0; j < roadConfig._roadTemplates[_savedTemplateIndex]._laneTemplateNames.Length; j++)
            {
                foreach (var laneTemplate in roadConfig._laneTemplates)
                {
                    if (laneTemplate._name.Equals(roadConfig._roadTemplates[_savedTemplateIndex]._laneTemplateNames[j]))
                    {
                        _laneMaterials[j] = laneTemplate._material;
                        _laneWidths[j]    = laneTemplate._width;
                        _laneMaterialSizes[j] = laneTemplate._materialSizeReference;

                        break;
                    }
                }
            }
        }
        else
        {
            throw new NullReferenceException();
        }
    }

    #region Variables

    public int RoadConfigIndex = 0;

    [SerializeField] internal Vector3[] _anchorPoints = { Vector3.zero, Vector3.forward * 5f };
    [SerializeField] internal RoadContact[] _roadContacts;

    internal int _speedLimit;
    internal bool[] _laneDirections;
    private Material[] _laneMaterials;
    internal double[] _laneWidths;
    private double[] _laneMaterialSizes;
    internal LaneType[] _laneTypes;
    internal int _savedTemplateIndex = 0;

    private QuadraticPolynomial[,] _dividerPolynoms;
    internal QuadraticPolynomial[,] _lanePolynoms;
    internal double[,,,] _arcLengthRegressionValues;

    #endregion
}