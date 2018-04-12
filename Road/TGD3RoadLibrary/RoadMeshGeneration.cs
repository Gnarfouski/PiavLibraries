using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 219

internal static class RoadMeshGeneration
{
    /// <summary>Class used to create evenly spaced points from divider ppolynoms, then generate the 3D mesh.
    /// </summary>

    internal static Mesh GenerateMesh(QuadraticPolynomial[,] polynomials, double[] textureSizes)
    {

        var evenlySpacedPoints = LoadEvenlySpacedPoints(polynomials, 3);
        evenlySpacedPoints = OptimizePoints(evenlySpacedPoints);

        var meshes    = new Mesh[polynomials.GetLength(0) - 1];

        var vertices  = new List<Vector3>[meshes.Length];
        var uv        = new List<Vector2>[meshes.Length];
        var colors    = new List<Color32>[meshes.Length];
        var normals   = new List<Vector3>[meshes.Length];
        var triangles = new List<int>[meshes.Length];

        for (var i = 0; i < meshes.Length; ++i)
        {
            meshes[i]    = new Mesh();
            vertices[i]  = new List<Vector3>();
            uv[i]        = new List<Vector2>();
            colors[i]    = new List<Color32>();
            normals[i]   = new List<Vector3>();
            triangles[i] = new List<int>();
        }

        // For each way, scan the two adjacent dividers and create uv, color and normal for each vertice

        for (var i = 0; i < polynomials.GetLength(0) - 1; i++)
        {
            double totalDistance = 0;
            for (int j = 0; j < polynomials.GetLength(1); j++) totalDistance += polynomials[i, j].ArcLength(0, 1);
            double numberTiles = totalDistance / textureSizes[i];

            var divider1Count = 1;
            var pos1Count     = 0;
            var divider2Count = 1;
            var pos2Count     = 1;

            // add the right and left vertices at position 0

            vertices[i].Add(evenlySpacedPoints[i][0].Item2);
            uv[i].Add(i >= (polynomials.GetLength(0) - 1) / 2 ? new Vector2(1, 0) : new Vector2(0, 0));
            colors[i].Add(Color.white);
            normals[i].Add(Vector3.up);

            vertices[i].Add(evenlySpacedPoints[i + 1][0].Item2);
            uv[i].Add(i >= (polynomials.GetLength(0) - 1) / 2 ? new Vector2(0, 0) : new Vector2(1, 0));
            colors[i].Add(Color.white);
            normals[i].Add(Vector3.up);

            // add the next positionned vertice and its uv value - uv is inverted for the second half of the street

            if (evenlySpacedPoints[i][divider1Count].Item1 <= evenlySpacedPoints[i + 1][divider2Count].Item1)
            {
                vertices[i].Add(evenlySpacedPoints[i][divider1Count].Item2);

                uv[i]
                   .Add(
                        i >= (polynomials.GetLength(0) - 1) / 2 ?
                            new Vector2(1, (float)(evenlySpacedPoints[i][divider1Count].Item1 * numberTiles)) :
                            new Vector2(0, (float)(evenlySpacedPoints[i][divider1Count].Item1 * numberTiles)));

                divider1Count++;
                pos1Count = vertices[i].Count - 1;
            }
            else
            {
                vertices[i].Add(evenlySpacedPoints[i + 1][divider2Count].Item2);

                uv[i]
                   .Add(
                        i >= (polynomials.GetLength(0) - 1) / 2 ?
                            new Vector2(0, (float)(evenlySpacedPoints[i + 1][divider2Count].Item1 * numberTiles)) :
                            new Vector2(1, (float)(evenlySpacedPoints[i + 1][divider2Count].Item1 * numberTiles)));

                divider2Count++;
                pos2Count = vertices[i].Count - 1;
            }

            colors[i].Add(Color.white);
            normals[i].Add(Vector3.up);

            // create triangle with the first three vertices

            if (IsUpwardsTriangle(vertices[i][0], vertices[i][1], vertices[i][2]))
            {
                triangles[i].Add(0);
                triangles[i].Add(1);
                triangles[i].Add(2);
            }
            else
            {
                triangles[i].Add(0);
                triangles[i].Add(2);
                triangles[i].Add(1);
            }

            // while one of the dividers has unused vertices

            while (!(divider1Count == evenlySpacedPoints[i].Count && divider2Count == evenlySpacedPoints[i + 1].Count))
            {
                // get the last used vertice on each divider
                var tempVertsIndexes = new List<int>();
                tempVertsIndexes.Add(pos1Count);
                tempVertsIndexes.Add(pos2Count);

                // get the vertice with the smallest global score

                var val1 = divider1Count < evenlySpacedPoints[i].Count ? evenlySpacedPoints[i][divider1Count].Item1 : float.PositiveInfinity;
                var val2 = divider2Count < evenlySpacedPoints[i + 1].Count ? evenlySpacedPoints[i + 1][divider2Count].Item1 : float.PositiveInfinity;

                if (val1 <= val2)
                {
                    vertices[i].Add(evenlySpacedPoints[i][divider1Count].Item2);

                    uv[i]
                       .Add(
                            i >= (polynomials.GetLength(0) - 1) / 2 ?
                                new Vector2(1, (float)(evenlySpacedPoints[i][divider1Count].Item1 * numberTiles)) :
                                new Vector2(0, (float)(evenlySpacedPoints[i][divider1Count].Item1 * numberTiles)));

                    divider1Count++;
                    pos1Count = vertices[i].Count - 1;
                }
                else
                {
                    vertices[i].Add(evenlySpacedPoints[i + 1][divider2Count].Item2);

                    uv[i]
                       .Add(
                            i >= (polynomials.GetLength(0) - 1) / 2 ?
                                new Vector2(0, (float)(evenlySpacedPoints[i + 1][divider2Count].Item1 * numberTiles)) :
                                new Vector2(1, (float)(evenlySpacedPoints[i + 1][divider2Count].Item1 * numberTiles)));

                    divider2Count++;
                    pos2Count = vertices[i].Count - 1;
                }

                colors[i].Add(Color.white);
                normals[i].Add(Vector3.up);

                tempVertsIndexes.Add(vertices[i].Count - 1);

                if (IsUpwardsTriangle(vertices[i][tempVertsIndexes[0]], vertices[i][tempVertsIndexes[1]], vertices[i][tempVertsIndexes[2]]))
                {
                    triangles[i].Add(tempVertsIndexes[0]);
                    triangles[i].Add(tempVertsIndexes[1]);
                    triangles[i].Add(tempVertsIndexes[2]);
                }
                else
                {
                    triangles[i].Add(tempVertsIndexes[0]);
                    triangles[i].Add(tempVertsIndexes[2]);
                    triangles[i].Add(tempVertsIndexes[1]);
                }
            }
        }

        var combinedVerts = vertices[0].Count;

        for (var i = 1; i < polynomials.GetLength(0) - 1; ++i)
        {
            vertices[0].AddRange(vertices[i]);
            uv[0].AddRange(uv[i]);
            normals[0].AddRange(normals[i]);

            for (var j = 0; j < triangles[i].Count; j++) triangles[i][j] += combinedVerts;
            combinedVerts                                                += vertices[i].Count;

            //Debug.Log("combined verts " + combinedVerts);
        }

        var mesh = new Mesh();

        mesh.vertices = vertices[0].ToArray();
        mesh.uv       = uv[0].ToArray();
        mesh.normals  = normals[0].ToArray();

        mesh.subMeshCount = polynomials.GetLength(0) - 1;
        for (var i = 0; i < polynomials.GetLength(0) - 1; ++i) mesh.SetTriangles(triangles[i].ToArray(), i);

        return mesh;

        //*/
    }

    private static List<Tuple<double, Vector3>>[] OptimizePoints(List<Tuple<double, Vector3>>[] evenlySpacedPoints)
    {
        for (int j = 0; j < evenlySpacedPoints.Length; j ++)
        {
            var replacement = new List<Tuple<double, Vector3>>();
            replacement.Add(evenlySpacedPoints[j][0]);
            for (int i = 1; i < evenlySpacedPoints[j].Count - 1; i++)
            {
                var diff = Vector3.Angle(
                                         (evenlySpacedPoints[j][i].Item2 - evenlySpacedPoints[j][i - 1].Item2).normalized,
                                         (evenlySpacedPoints[j][i + 1].Item2 - evenlySpacedPoints[j][i].Item2).normalized);

                if(diff > 0.1) replacement.Add(evenlySpacedPoints[j][i]);
            }
            replacement.Add(evenlySpacedPoints[j][evenlySpacedPoints[j].Count - 1]);
            evenlySpacedPoints[j] = replacement;
        }

        return evenlySpacedPoints;
    }

    private static List<Tuple<double, Vector3>>[] LoadEvenlySpacedPoints(QuadraticPolynomial[,] polynomials, float vertexSpacing)
    {
        var evenlySpacedPoints = new List<Tuple<double, Vector3>>[polynomials.GetLength(0)];

        for (int i = 0; i < polynomials.GetLength(0); i++)
        {
            evenlySpacedPoints[i] = new List<Tuple<double, Vector3>>();

            //Debug.Log(polynomials[i,0]);
            evenlySpacedPoints[i].Add(new Tuple<double, Vector3>(0, polynomials[i, 0].Calculate(0)));

            int   polyIndexCheck       = 0;
            double accumulatedRoot     = 0;
            double currentPolyLength    = 0;
            double currentSpacingLength = 0;
            double currentRoot          = 0;

            while (polyIndexCheck < polynomials.GetLength(1))
            {
                double currentArcLength = polynomials[i, polyIndexCheck].ArcLength(0, 1);
                while (currentPolyLength < currentArcLength)
                {
                    currentRoot          += 0.01;
                    double addedDistance  = polynomials[i, polyIndexCheck].ArcLength(currentRoot - 0.01f, currentRoot);
                    currentSpacingLength += addedDistance;
                    currentPolyLength    += addedDistance;
                    accumulatedRoot     += 0.01;

                    if (currentSpacingLength > vertexSpacing)
                    {
                        evenlySpacedPoints[i].Add(new Tuple<double, Vector3>(accumulatedRoot / polynomials.GetLength(1), polynomials[i, polyIndexCheck].Calculate(currentRoot)));
                        currentSpacingLength = 0;
                    }
                }
                currentPolyLength = 0;
                currentRoot       = 0;
                polyIndexCheck++;
            }

            evenlySpacedPoints[i].Add(new Tuple<double, Vector3>(1, polynomials[i, polynomials.GetLength(1) - 1].Calculate(1)));
        }

        return evenlySpacedPoints;
    }

    private static bool IsUpwardsTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        var center = (p0 + p1 + p2) / 3;
        var normal = Vector3.Cross(p0 - center, p1 - center).normalized;
        var dot    = Vector3.Dot(Vector3.up, normal);

        return dot >= 0;
    }
}