using UnityEngine;
using System;
using System.Collections.Generic;

internal static class RoadUtilities
{

    [Serializable]
    internal struct CloseRoot
    {
        internal double _root;
        internal double _distance;
    }

    internal static CloseRoot GetClosestRootWithoutYComponent(double[,] polynomial, Vector3 pos)
    {
        pos.y = 0;

        var a = 4.0 * Math.Pow(polynomial[0, 0], 2) +
                4.0 * Math.Pow(polynomial[0, 2], 2);

        var b = 6.0 * polynomial[0, 0] * polynomial[1, 0] +
                6.0 * polynomial[0, 2] * polynomial[1, 2];

        var c = 2.0 * Math.Pow(polynomial[1, 0], 2) +
                4.0 * polynomial[0, 0] * (polynomial[2, 0] - pos.x) +
                2.0 * Math.Pow(polynomial[1, 2], 2) +
                4.0 * polynomial[0, 2] * (polynomial[2, 2] - pos.z);

        var d = 2.0 * polynomial[1, 0] * (polynomial[2, 0] - pos.x) +
                2.0 * polynomial[1, 2] * (polynomial[2, 2] - pos.z);

        if (Math.Abs(a) > 0.000001)
        {
            b = b / a;
            c = c / a;
            d = d / a;

            var res = GetRealRoots(d, c, b);

            var possibleRoots = new List<double>();

            foreach (var root in new[]
            {
                res._root1,
                res._root2,
                res._root3
            })
            {
                if (!double.IsNaN(root))
                    if (root < 0)
                        possibleRoots.Add(0);
                    else if (root > 1)
                        possibleRoots.Add(1);
                    else
                        possibleRoots.Add(root);
            }

            var minRoot  = -1.0;
            var minValue = double.MaxValue;

            foreach (var t in possibleRoots)
            {
                var local    = Calculate(polynomial, t);
                local.y      = 0;
                var distance = (local - pos).magnitude;

                if (distance < minValue)
                {
                    minRoot  = t;
                    minValue = distance;
                }
            }

            return new CloseRoot { _root = minRoot, _distance = minValue };

        }

        {
            var root = -1 *
                       (2 * polynomial[1, 0] * (polynomial[2, 0] - pos.x) +
                        2 * polynomial[1, 2] * (polynomial[2, 2] - pos.z)) /
                       (2 *
                        (Math.Pow(polynomial[1, 0], 2) +
                         Math.Pow(polynomial[1, 2], 2)));
            if (root < 0) root = 0;
            if (root > 1) root = 1;

            var posR = Calculate(polynomial, root);
            posR.y   = pos.y;

            return new CloseRoot { _root = root, _distance = (posR - pos).magnitude };
        }
    }

    /// <summary>
    /// Returns a tuple (root, distanceToRoot)
    /// </summary>
    internal static CloseRoot GetClosestRoot(double[,] polynomial, Vector3 pos)
    {

        var a = 4.0 * Math.Pow(polynomial[0, 0], 2) +
                4.0 * Math.Pow(polynomial[0, 1], 2) +
                4.0 * Math.Pow(polynomial[0, 2], 2);

        var b = 6.0 * polynomial[0, 0] * polynomial[1, 0] +
                6.0 * polynomial[0, 1] * polynomial[1, 1] +
                6.0 * polynomial[0, 2] * polynomial[1, 2];

        var c = 2.0 * Math.Pow(polynomial[1, 0], 2) +
                4.0 * polynomial[0, 0] * (polynomial[2, 0] - pos.x) +
                2.0 * Math.Pow(polynomial[1, 1], 2) +
                4.0 * polynomial[0, 1] * (polynomial[2, 1] - pos.y) +
                2.0 * Math.Pow(polynomial[1, 2], 2) +
                4.0 * polynomial[0, 2] * (polynomial[2, 2] - pos.z);

        var d = 2.0 * polynomial[1, 0] * (polynomial[2, 0] - pos.x) +
                2.0 * polynomial[1, 1] * (polynomial[2, 1] - pos.y) +
                2.0 * polynomial[1, 2] * (polynomial[2, 2] - pos.z);

        if (Math.Abs(a) > 0.000001)
        {
            b = b / a;
            c = c / a;
            d = d / a;

            var res = GetRealRoots(d, c, b);

            var possibleRoots = new List<double>();

            foreach (var root in new[]
            {
                res._root1,
                res._root2,
                res._root3
            })
            {
                if (!double.IsNaN(root))
                    if (root < 0)
                        possibleRoots.Add(0);
                    else if (root > 1)
                        possibleRoots.Add(1);
                    else
                        possibleRoots.Add(root);
            }

            var minRoot  = -1.0;
            var minValue = double.MaxValue;

            foreach (var t in possibleRoots)
            {
                if ((Calculate(polynomial, t) - pos).magnitude < minValue)
                {
                    minRoot  = t;
                    minValue = (Calculate(polynomial, t) - pos).magnitude;
                }
            }

            return new CloseRoot { _root = minRoot, _distance = minValue };

        }

        {
            var root = -1 *
                       (2 * polynomial[1, 0] * (polynomial[2, 0] - pos.x) +
                        2 * polynomial[1, 1] * (polynomial[2, 1] - pos.y) +
                        2 * polynomial[1, 2] * (polynomial[2, 2] - pos.z)) /
                       (2 *
                        (Math.Pow(polynomial[1, 0], 2) +
                         Math.Pow(polynomial[1, 1], 2) +
                         Math.Pow(polynomial[1, 2], 2)));
            if (root < 0) root = 0;
            if (root > 1) root = 1;

            return new CloseRoot { _root = root, _distance = (Calculate(polynomial, root) - pos).magnitude };
        }
    }

    private static void Qr(double a2, double a1, double a0, out double q, out double r)
    {
        q = (3.0 * a1 - a2 * a2) / 9.0;
        r = (9.0 * a2 * a1 - 27.0 * a0 - 2.0 * a2 * a2 * a2) / 54.0;
    }

    private static double PowThird(double n)
    {
        return Math.Pow(Math.Abs(n), 1.0 / 3.0) * Math.Sign(n);
    }

    [Serializable]
    internal struct RealRoots
    {
        internal double _root1, _root2, _root3;
    }

    private static RealRoots GetRealRoots(double a0, double a1, double a2)
    {
        Qr(a2, a1, a0, out var q, out var r);
        double num1 = q * q * q;
        double x    = num1 + r * r;
        double num2 = -a2 / 3.0;
        double num3 = double.NaN;
        double num4 = double.NaN;
        double num5;

        if (x >= 0.0)
        {
            double num6 = Math.Pow(x, 0.5);
            double num7 = PowThird(r + num6);
            double num8 = PowThird(r - num6);
            num5        = num2 + (num7 + num8);

            if (Math.Abs(x) < 0.000001)
                num3 = num2 - num7;
        }
        else
        {
            double num6 = Math.Acos(r / Math.Sqrt(-num1));
            num5        = 2.0 * Math.Sqrt(-q) * Math.Cos(num6 / 3.0) + num2;
            num3        = 2.0 * Math.Sqrt(-q) * Math.Cos((num6 + 2.0 * Math.PI) / 3.0) + num2;
            num4        = 2.0 * Math.Sqrt(-q) * Math.Cos((num6 - 2.0 * Math.PI) / 3.0) + num2;
        }

        return new RealRoots{_root1 = num5, _root2 = num3, _root3 = num4};
    }

    public static Vector3 Calculate(double[,] polynomial, double root)
    {
        if (polynomial == null)
        {
            Debug.LogError("RoadUtilities.Calculate called with null polynomial reference");

            return Vector3.zero;
        }

        if (polynomial.GetLength(0) != 3 || polynomial.GetLength(1) != 3)
        {
            Debug.LogError("RoadUtilities.Calculate called with " + polynomial.GetLength(0) + " x " + polynomial.GetLength(1) + " size polynomial matrix (3 x 3 required)");

            return Vector3.zero;
        }

        foreach (var number in polynomial)
        {
            if (double.IsNaN(number))
            {
                Debug.LogError("RoadUtilities.Calculate called with a NaN in the polynomial");

                return Vector3.zero;
            }
        }

        if (root < -0.1 || root > 1.1)
        {
            Debug.LogError("RoadUtilities.Calculate called with " + root + " root (range 0-1 required).");

            return Vector3.zero;
        }

        return new Vector3(
                           (float)(polynomial[0, 0] * root * root + polynomial[1, 0] * root + polynomial[2, 0]),
                           (float)(polynomial[0, 1] * root * root + polynomial[1, 1] * root + polynomial[2, 1]),
                           (float)(polynomial[0, 2] * root * root + polynomial[1, 2] * root + polynomial[2, 2]));
    }

    public static Vector3 CalculateFirstDerivative(double[,] polynomial, double root)
    {
        if (polynomial == null)
        {
            Debug.LogError("RoadUtilities.CalculateFirstDerivative called with null polynomial reference");

            return Vector3.zero;
        }

        if (polynomial.GetLength(0) != 3 || polynomial.GetLength(1) != 3)
        {
            Debug.LogError("RoadUtilities.CalculateFirstDerivative called with " + polynomial.GetLength(0) + " x " + polynomial.GetLength(1) + " size polynomial matrix (3 x 3 required)");

            return Vector3.zero;
        }

        foreach (var number in polynomial)
        {
            if (double.IsNaN(number))
            {
                Debug.LogError("RoadUtilities.CalculateFirstDerivative called with a NaN in the polynomial");

                return Vector3.zero;
            }
        }

        if (double.IsNaN(root) || root < -0.1 || root > 1.1)
        {
            Debug.LogError("RoadUtilities.CalculateFirstDerivative called with root " + root + " (range 0-1 required).");

            return Vector3.zero;
        }

        return new Vector3(
                           (float)(2 * polynomial[0, 0] * root + polynomial[1, 0]),
                           (float)(2 * polynomial[0, 1] * root + polynomial[1, 1]),
                           (float)(2 * polynomial[0, 2] * root + polynomial[1, 2]));
    }

    public static double GetArcLengthBetween(double[] arcLengthValues, double startRoot, double endRoot)
    {
        if (startRoot < 0 || startRoot > 1 || endRoot < 0 || endRoot > 1)
        {
            Debug.LogError("RoadUtilities.GetArcLengthBetween called with roots " + startRoot + " | " + endRoot + " (must be between 0 and 1)");

            return double.NaN;
        }

        return
            arcLengthValues[0] * endRoot * endRoot * endRoot +
            arcLengthValues[1] * endRoot * endRoot +
            arcLengthValues[2] * endRoot -
            (arcLengthValues[0] * startRoot * startRoot * startRoot + arcLengthValues[1] * startRoot * startRoot + arcLengthValues[2] * startRoot);
    }

    ///<summary> Returns a tuple (remaining distance, givendRoot)
    ///</summary>
    public static PiavRoadContainer.Projection GetProjection(Segment s, double startRoot, double distance)
    {
        if (startRoot < 0 || startRoot > 1)
        {
            Debug.LogError("RoadUtilities.GetProjection called with root " + startRoot + " (must be between 0 and 1)");

            return new PiavRoadContainer.Projection { _remainingDistance = double.NaN, _targetSegment = null, _targetRoot = double.NaN };
        }

        //Debug.Log("start root " + startRoot);
        var startDistance = s.ArcLength[0] * startRoot * startRoot * startRoot +
                            s.ArcLength[1] * startRoot * startRoot +
                            s.ArcLength[2] * startRoot;

        //Debug.Log("start distance " + startDistance);

        if (startDistance + distance <= 0)
            return new PiavRoadContainer.Projection { _remainingDistance = Math.Abs(startDistance) + distance, _targetSegment = s, _targetRoot = 0 };

        var totalDistance = s.ArcLength[0] + s.ArcLength[1] + s.ArcLength[2];

        if (startDistance + distance >= totalDistance)
            return new PiavRoadContainer.Projection { _remainingDistance = startDistance + distance - totalDistance, _targetSegment = s, _targetRoot = 1 };

        var finalDistance = startDistance + distance;

        return new PiavRoadContainer.Projection
        {
            _remainingDistance = 0,
            _targetSegment     = s,
            _targetRoot        = s.InvArcLength[0] * finalDistance * finalDistance * finalDistance +
                                 s.InvArcLength[1] * finalDistance * finalDistance +
                                 s.InvArcLength[2] * finalDistance
        };
    }

    public static PiavRoadContainer.Projection GetProjectionTo(double[] arcLengthValues, double[] invarcLengthValues, Segment s, double startRoot, double endRoot, double distance)
    {
        if (startRoot < 0 || startRoot > 1 || endRoot < 0 || endRoot > 1)
        {
            Debug.LogError("RoadUtilities.GetProjectionTo called with roots " + startRoot + " " + endRoot + " (must be between 0 and 1)");

            return new PiavRoadContainer.Projection { _remainingDistance = double.NaN,_targetSegment = s,_targetRoot = double.NaN };
        }

        if (distance < 0)
        {
            Debug.LogError("RoadUtilities.GetProjectionTo called with distance " + distance + " (must be positive)");

            return new PiavRoadContainer.Projection { _remainingDistance = double.NaN, _targetSegment = s, _targetRoot = double.NaN };
        }

        var startDistance = arcLengthValues[0] * startRoot * startRoot * startRoot +
                            arcLengthValues[1] * startRoot * startRoot +
                            arcLengthValues[2] * startRoot;

        var endDistance = arcLengthValues[0] * endRoot * endRoot * endRoot +
                          arcLengthValues[1] * endRoot * endRoot +
                          arcLengthValues[2] * endRoot;

        if (startRoot >= endRoot && startDistance - distance <= endDistance)
            return new PiavRoadContainer.Projection { _remainingDistance = Math.Abs(startDistance - distance), _targetSegment = s, _targetRoot = endRoot };

        if (startRoot <= endRoot && startDistance + distance >= endDistance)
            return new PiavRoadContainer.Projection { _remainingDistance = startDistance + distance - endDistance, _targetSegment = s, _targetRoot = endRoot };

        var finalDistance = startRoot <= endRoot ? startDistance + distance : startDistance - distance;

        return new PiavRoadContainer.Projection
        {
            _remainingDistance = 0,
            _targetSegment = s,
            _targetRoot =
                invarcLengthValues[0] * finalDistance * finalDistance * finalDistance +
                invarcLengthValues[1] * finalDistance * finalDistance +
                invarcLengthValues[2] * finalDistance
        };
    }
}


