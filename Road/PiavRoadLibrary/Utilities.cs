using System;
using System.Collections.Generic;

internal static class Utilities
{
    /// <summary>
    /// Creates a collection of evenly spaced points, ordered by a common distance value to the end of the piecewise polynomial.
    /// </summary>
    /// <param name="polynomials">QuadraticPolynomial[numberLanes + 1,numberAnchors - 1]</param>
    /// <returns>ValuedPoint[numberLanes + 1][numberPoints]</returns>
    public static ValuedPoint[][] GetEvenlySpacedPoints(QuadraticPolynomial[,] polynomials)
    {
        const double vertexSpacing = 3.0;

        var evenlySpacedPoints = new ValuedPoint[polynomials.GetLength(0)][];

        for (int i = 0; i < polynomials.GetLength(0); i++)
        {
            var localList = new List<ValuedPoint> { new ValuedPoint { Value = 0, Distance = 0, Point = polynomials[i, 0].Calculate(0) } };

            int    polyIndexCheck       = 0;
            double accumulatedRoot      = 0;
            double currentPolyLength    = 0;
            double currentSpacingLength = 0;
            double currentTotalLength = 0;
            double currentRoot          = 0;

            while (polyIndexCheck < polynomials.GetLength(1))
            {
                double currentArcLength = polynomials[i, polyIndexCheck].ArcLength(0, 1);

                while (currentPolyLength < currentArcLength)
                {
                    currentRoot          += 0.01;
                    double addedDistance = polynomials[i, polyIndexCheck].ArcLength(currentRoot - 0.01f, currentRoot);
                    currentSpacingLength += addedDistance;
                    currentPolyLength    += addedDistance;
                    currentTotalLength += addedDistance;
                    accumulatedRoot      += 0.01;

                    if (currentSpacingLength > vertexSpacing)
                    {
                        localList.Add(new ValuedPoint { Value = accumulatedRoot / polynomials.GetLength(1), Distance =  currentTotalLength, Point = polynomials[i, polyIndexCheck].Calculate(currentRoot) });
                        currentSpacingLength                   = 0;
                    }
                }
                currentPolyLength = 0;
                currentRoot       = 0;
                polyIndexCheck++;
            }

            localList.Add(new ValuedPoint { Value = 1, Distance = currentTotalLength, Point = polynomials[i, polynomials.GetLength(1) - 1].Calculate(1) });

            evenlySpacedPoints[i] = localList.ToArray();
        }

        return evenlySpacedPoints;
    }

    /// <summary>
    /// Deletes unnecessary points to reduce the vertex count without impacting the resulting mesh's visual aspect.
    /// </summary>
    /// <param name="evenlySpacedPoints"></param>
    /// <returns></returns>
    public static ValuedPoint[][] OptimizePoints(ValuedPoint[][] evenlySpacedPoints)
    {
        for (int j = 0; j < evenlySpacedPoints.Length; j++)
        {
            var replacement = new List<ValuedPoint> { evenlySpacedPoints[j][0] };

            for (int i = 1; i < evenlySpacedPoints[j].Length - 1; i++)
            {
                var diff = evenlySpacedPoints[j][i].Point.Sub(evenlySpacedPoints[j][i - 1].Point).AngleTo(evenlySpacedPoints[j][i + 1].Point.Sub(evenlySpacedPoints[j][i].Point));

                if (diff > 0.1) replacement.Add(evenlySpacedPoints[j][i]);
            }
            replacement.Add(evenlySpacedPoints[j][evenlySpacedPoints[j].Length - 1]);
            evenlySpacedPoints[j] = replacement.ToArray();
        }

        return evenlySpacedPoints;
    }

    /// <summary> Use LU Decomposition to approximate arc length and inverse arc length functions for faster computation. They will be used in the XML navigation file. </summary>
    /// <returns>double[nbLanes,nbAnchors-1,2,3] with 2 being (arcLength,invArcLength) and 3 being(x^3,x^2,x)</returns>
    public static double[,,,] ArcLengthApproximations(QuadraticPolynomial[,] lanePolynoms)
    {
        var arcLengthApproximations = new double[lanePolynoms.GetLength(0), lanePolynoms.GetLength(1), 2, 3];
        var steps                   = new double[11];
        var values                  = new double[11];

        for (int j = 0; j < lanePolynoms.GetLength(0); j++)
        {
            for (int k = 0; k < lanePolynoms.GetLength(1); k++)
            {
                for (var i = 0; i < 11; i++)
                {
                    steps[i]  = i / 10d;
                    values[i] = lanePolynoms[j, k].ArcLength(0.0, steps[i]);
                }

                if (lanePolynoms[j, k]._coeffs[0, 0] < 0.0001 && lanePolynoms[j, k]._coeffs[0, 1] < 0.0001 && lanePolynoms[j, k]._coeffs[0, 2] < 0.0001)
                {
                    arcLengthApproximations[j, k, 0, 0] = 0;
                    arcLengthApproximations[j, k, 1, 0] = 0;
                    arcLengthApproximations[j, k, 2, 0] = lanePolynoms[j, k].ArcLength(0.0, 1.0);

                    arcLengthApproximations[j, k, 0, 1] = 0;
                    arcLengthApproximations[j, k, 1, 1] = 0;
                    arcLengthApproximations[j, k, 2, 1] = 1 / arcLengthApproximations[j, k, 0, 2];
                }
                else
                {
                    var aLen                            = new PolyFit(steps, values, 3);
                    arcLengthApproximations[j, k, 0, 0] = aLen.Coeff[3];
                    arcLengthApproximations[j, k, 1, 0] = aLen.Coeff[2];
                    arcLengthApproximations[j, k, 2, 0] = aLen.Coeff[1];

                    var invaLen                         = new PolyFit(values, steps, 3);
                    arcLengthApproximations[j, k, 0, 1] = invaLen.Coeff[3];
                    arcLengthApproximations[j, k, 1, 1] = invaLen.Coeff[2];
                    arcLengthApproximations[j, k, 2, 1] = invaLen.Coeff[1];
                }
            }
        }

        return arcLengthApproximations;
    }


    /// <summary> Generate (number lanes + 1) piecewise polynoms surrounding the lanes. They will be used for the 3D mesh' vertices placing </summary>
    /// <param name="anchorPoints">double[numberOfPoints,3] where 3 are the dimensions(x,y,z)</param>
    /// <param name="laneWidths">double[numberOfLanes] width of each lane</param>
    public static QuadraticPolynomial[,] GenerateDividerPolynoms(double[,] anchorPoints, double[] laneWidths)
    {

        double[] dVectorUp =
        {
            0,
            1,
            0
        };

        var polynomials     = new QuadraticPolynomial[laneWidths.Length + 1, anchorPoints.Length - 1];
        var centralPPolynom = PathPPolynom(anchorPoints);

        // Compute Cross Vectors

        var centralCrossValues = new double[anchorPoints.Length][];

        centralCrossValues[0]    = centralPPolynom[0].CalculateFirstDerivative(0).Cross(dVectorUp);
        centralCrossValues[0][1] = 0;
        centralCrossValues[0]    = centralCrossValues[0].Normalize();

        for (var i = 0; i < centralPPolynom.Length; i++)
        {
            centralCrossValues[i + 1]    = centralPPolynom[i].CalculateFirstDerivative(1).Cross(dVectorUp);
            centralCrossValues[i + 1][1] = 0;
            centralCrossValues[i + 1]    = centralCrossValues[i + 1].Normalize();
        }

        // Compute Cross Divider Distances

        double totalRoadWidth                         = 0;
        foreach (var lw in laneWidths) totalRoadWidth += lw;

        var dividerDistances = new double[laneWidths.Length + 1];
        dividerDistances[0]  = totalRoadWidth / 2;

        for (var i = 1; i < dividerDistances.Length; i++) dividerDistances[i] = dividerDistances[i - 1] - laneWidths[i - 1];

        // Compute Divider-wise Points

        var dividerPoints = new double[laneWidths.Length + 1, anchorPoints.Length, 3];

        for (var i = 0; i <= laneWidths.Length; i++)
        {
            for (var j = 0; j < anchorPoints.Length; j++)
            {
                for (int k = 0; k <= 2; k++) dividerPoints[i, j, k] = anchorPoints[j, k] + centralCrossValues[j][k] * dividerDistances[i];
            }
        }

        // Compute Divider polynoms - We cannot path the divider polys the same way we did with the central one (see online docs). We use relative point distance to scale the central polynoms

        for (var i = 0; i <= laneWidths.Length; i++)
        {
            for (var j = 0; j < anchorPoints.Length - 1; j++)
            {
                var dist = new[]
                {
                    anchorPoints[j + 1, 0] - anchorPoints[j, 0],
                    anchorPoints[j + 1, 1] - anchorPoints[j, 1],
                    anchorPoints[j + 1, 2] - anchorPoints[j, 2]
                };

                var ddist = new[]
                {
                    dividerPoints[i, j + 1, 0] - dividerPoints[i, j, 0],
                    dividerPoints[i, j + 1, 1] - dividerPoints[i, j, 1],
                    dividerPoints[i, j + 1, 2] - dividerPoints[i, j, 2]
                };

                var ponderation = new[]
                {
                    Math.Abs(dist[0]) < 0.001 ? 0 : ddist[0] / dist[0],
                    Math.Abs(dist[1]) < 0.001 ? 0 : ddist[1] / dist[1],
                    Math.Abs(dist[2]) < 0.001 ? 0 : ddist[2] / dist[2]
                };

                if (j == 0)
                {
                    polynomials[i, j] = new QuadraticPolynomial
                    {
                        _coeffs =
                        {
                            [0, 0] = centralPPolynom[j]._coeffs[0, 0],
                            [0, 1] = centralPPolynom[j]._coeffs[0, 1],
                            [0, 2] = centralPPolynom[j]._coeffs[0, 2],
                            [1, 0] = centralPPolynom[j]._coeffs[1, 0],
                            [1, 1] = centralPPolynom[j]._coeffs[1, 1],
                            [1, 2] = centralPPolynom[j]._coeffs[1, 2],
                            [2, 0] = dividerPoints[i, j, 0],
                            [2, 1] = dividerPoints[i, j, 1],
                            [2, 2] = dividerPoints[i, j, 2]
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

                    polynomials[i, j] = new QuadraticPolynomial
                    {
                        _coeffs =
                        {
                            [0, 0] = dividerPoints[i, j + 1, 0] - dividerPoints[i, j, 0] - ponderation[0] * (2 * aix + bix),
                            [0, 1] = dividerPoints[i, j + 1, 1] - dividerPoints[i, j, 1] - ponderation[1] * (2 * aiy + biy),
                            [0, 2] = dividerPoints[i, j + 1, 2] - dividerPoints[i, j, 2] - ponderation[2] * (2 * aiz + biz),
                            [1, 0] = ponderation[0] * (2 * aix + bix),
                            [1, 1] = ponderation[1] * (2 * aiy + biy),
                            [1, 2] = ponderation[2] * (2 * aiz + biz),
                            [2, 0] = dividerPoints[i, j, 0],
                            [2, 1] = dividerPoints[i, j, 1],
                            [2, 2] = dividerPoints[i, j, 2]
                        }
                    };
                }
            }
        }

        return polynomials;
    }

    /// <summary> Generate one piecewise polynom per lane. They will be used to write the navigation xml file.
    /// </summary>
    public static QuadraticPolynomial[,] GenerateLanePolynoms(QuadraticPolynomial[,] dividers)
    {
        var lanePolynoms = new QuadraticPolynomial[dividers.GetLength(0) - 1, dividers.GetLength(1)];

        for (var i = 0; i < dividers.GetLength(0) - 1; i++)
        {
            for (var j = 0; j < dividers.GetLength(1); j++) lanePolynoms[i, j] = (dividers[i, j] + dividers[i + 1, j]) / 2;
        }

        return lanePolynoms;
    }

    /// <summary> Generate one piecewise polynom per lane. They will be used to write the navigation xml file.
    /// </summary>
    public static QuadraticPolynomial[,] GenerateLanePolynoms(double[,] anchorPoints, double[] laneWidths)
    {
        var dividers     = GenerateDividerPolynoms(anchorPoints, laneWidths);
        var lanePolynoms = new QuadraticPolynomial[dividers.GetLength(0) - 1, dividers.GetLength(1)];

        for (var i = 0; i < dividers.GetLength(0) - 1; i++)
        {
            for (var j = 0; j < dividers.GetLength(1); j++) lanePolynoms[i, j] = (dividers[i, j] + dividers[i + 1, j]) / 2;
        }

        return lanePolynoms;
    }

    /// <summary> Generate a piecewise polynom from 3D points
    /// </summary>
    private static QuadraticPolynomial[] PathPPolynom(double[,] points)
    {
        var res = new QuadraticPolynomial[points.Length - 1];

        res[0] = new QuadraticPolynomial
        {
            _coeffs =
            {
                [1, 0] = points[1, 0] - points[0, 0],
                [1, 1] = points[1, 1] - points[0, 1],
                [1, 2] = points[1, 2] - points[0, 2],
                [2, 0] = points[0, 0],
                [2, 0] = points[0, 1],
                [2, 0] = points[0, 2]
            }
        };

        for (var i = 1; i < points.Length - 1; i++)
        {
            res[i] = new QuadraticPolynomial
            {
                _coeffs =
                {
                    [0, 0] = points[i + 1, 0] - points[i, 0] - 2 * res[i - 1]._coeffs[0, 0] - res[i - 1]._coeffs[1, 0],
                    [0, 1] = points[i + 1, 1] - points[i, 1] - 2 * res[i - 1]._coeffs[0, 1] - res[i - 1]._coeffs[1, 1],
                    [0, 2] = points[i + 1, 2] - points[i, 2] - 2 * res[i - 1]._coeffs[0, 2] - res[i - 1]._coeffs[1, 2],

                    [1, 0] = 2 * res[i - 1]._coeffs[0, 0] + res[i - 1]._coeffs[1, 0],
                    [1, 1] = 2 * res[i - 1]._coeffs[0, 1] + res[i - 1]._coeffs[1, 1],
                    [1, 2] = 2 * res[i - 1]._coeffs[0, 2] + res[i - 1]._coeffs[1, 2],

                    [2, 0] = points[i, 0],
                    [2, 1] = points[i, 1],
                    [2, 2] = points[i, 2]
                }
            };
        }

        return res;
    }
}