using System;

public static class PiavRoadInterface
{
    /// <summary>
    /// 
    /// </summary>Generate [nbLanes + 1][n] optimized points for the dividers.
    /// <param name="anchorPoints">double[numberOfPoints,3] where 3 are the dimensions(x,y,z)</param>
    /// <param name="laneWidths">double[numberOfLanes] width of each lane</param>
    /// <returns></returns>
    public static ValuedPoint[][] GetMeshPoints(double[,] anchorPoints, double[] laneWidths)
    {
        var dividerPPolys = Utilities.GenerateDividerPolynoms(anchorPoints, laneWidths);
        return Utilities.OptimizePoints(Utilities.GetEvenlySpacedPoints(dividerPPolys));
    }

    public static PolynomialRoadDescriptor GenerateRoadXml(double[,] anchorPoints, double[] laneWidths, string path)
    {
        var lanePPolys = Utilities.GenerateLanePolynoms(anchorPoints, laneWidths);
        var arcLengthValues = Utilities.ArcLengthApproximations(lanePPolys);

        var curveValues = new double[lanePPolys.GetLength(0), lanePPolys.GetLength(1), 3, 3];

        for (int i = 0; i < lanePPolys.GetLength(0); i++)
        {
            for (int j = 0; j < lanePPolys.GetLength(1); j++)
            {
                for (int k = 0; k <= 2; k++)
                {
                    curveValues[i, j, 0, k] = lanePPolys[i, j]._coeffs[0, k];
                    curveValues[i, j, 1, k] = lanePPolys[i, j]._coeffs[1, k];
                    curveValues[i, j, 2, k] = lanePPolys[i, j]._coeffs[2, k];
                }
            }
        }

        return new PolynomialRoadDescriptor()
        {
            QuadraticCurveValues    = curveValues,
            ArcLengthApproximations = arcLengthValues
        };
    }


}

/// <summary>A descriptor containing the quadratic functions and arc length approximations of all lanes of a single road.</summary>
[Serializable]
public struct PolynomialRoadDescriptor
{
    /// <summary>double[numberLanes + 1,numberAnchors - 1,{x2,x1,x0},{x,y,z}]. An array of second-degree polynomials, forming continuous piecewise curves for each lane.</summary>
    public double[,,,] QuadraticCurveValues { get; internal set; }
    /// <summary>double[numberLanes + 1,numberAnchors - 1,{x3,x2,x1},{arcLen,invArcLen}].
    /// An array of third-degree polynomials approximating the relation between the progress of the root along a quadratic curve and the effective distance (in meters) travelled from root 0.</summary>
    public double[,,,] ArcLengthApproximations { get; internal set; }
}

/// <summary>A 3d point that can be ordered by value.</summary>
[Serializable]
public struct ValuedPoint
{
    /// <summary>Relative position, distance wise between 0 and 1, to the end of the piecewise polynomial.</summary>
    public double   Value { get; internal set; }
    /// <summary>Relative total distance to end of polynomial.</summary>
    public double Distance { get; internal set; }
    /// <summary>Table of length 3 {x,y,z} for a 3d vector.</summary>
    public double[] Point { get; internal set; }
}