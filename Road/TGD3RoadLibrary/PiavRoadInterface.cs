using System;
using UnityEngine;

[ExecuteInEditMode]
internal class PiavRoadInterface : MonoBehaviour
{
    /// <summary>The Road Interface stores data for both the generation of the 3D mesh used in the scene, and the XML guidance file used by the agents. It allows you to :
    /// - place the 3D points guiding the road
    /// - choose the granularity of the 3D mesh
    /// - lock roads together with road contacts to form a navigable network for agents
    /// </summary>

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
        _dividerPolynoms = new QuadraticPolynomial[_laneWidths.Length + 1, AnchorPoints.Length - 1];
        var centralPPolynom = PathCentralPPolynom(AnchorPoints);

        // Compute Cross Vectors

        var centralCrossValues = new Vector3[AnchorPoints.Length];
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

        var dividerPoints = new Vector3[_laneWidths.Length + 1, AnchorPoints.Length];

        for (var i = 0; i <= _laneWidths.Length; i++)
        {
            for (var j = 0; j < AnchorPoints.Length; j++) dividerPoints[i, j] = AnchorPoints[j] + centralCrossValues[j] * (float)dividerDistances[i];
        }

        // Compute Divider polynoms - We cannot path the divider polys the same way we did with the central one (see online docs). We use relative point distance to scale the central polynoms

        for (var i = 0; i <= _laneWidths.Length; i++)
        {
            for (var j = 0; j < AnchorPoints.Length - 1; j++)
            {
                var dist = AnchorPoints[j + 1] - AnchorPoints[j];
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
        _arcLengthRegressionValues = new double[_lanePolynoms.GetLength(0),_lanePolynoms.GetLength(1),2,2];
        var steps  = new double[11];
        var values = new double[11];

        for (int j = 0; j < _lanePolynoms.GetLength(0); j++)
        {
            for (int k = 0; k < _lanePolynoms.GetLength(1); k++)
            {
                for (var i = 0; i < 11; i++)
                {
                    steps[i]  = i / 10d;
                    values[i] = _lanePolynoms[j,k].ArcLength(0.0, steps[i]);
                }

                var arc                          = AdHocGradientDescent.QuadraticRegressionBoldDriver(steps, values, 10000, 0.1);
                _arcLengthRegressionValues[j, k, 0, 0] = arc.Item1;// * _lanePolynoms[j, k].ArcLength(0.0, 1.0);
                _arcLengthRegressionValues[j, k, 0, 1] = arc.Item2;// * _lanePolynoms[j, k].ArcLength(0.0, 1.0);


                var invArc                       = AdHocGradientDescent.QuadraticRegressionBoldDriver(values, steps, 100000, 0.000001);
                _arcLengthRegressionValues[j, k, 1, 0] = invArc.Item1;
                _arcLengthRegressionValues[j, k, 1, 1] = invArc.Item2;
            }
        }
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

    [SerializeField] internal Vector3[] AnchorPoints = { Vector3.zero, Vector3.forward * 5f };
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