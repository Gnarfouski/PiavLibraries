using System;
using UnityEngine;

internal class QuadraticPolynomial
{
    /// <summary> Class used to house a 3-dimensional quadratic polynomial defined on [-inf,+inf]. The part that interests us is the [0,1] time span.
    /// A piecewise polynomial is a row of several quadratic polynomials, with the end of the n'th being continuous with the beginning of the n+1'th.
    /// These represent divider and lane ppolys.
    /// </summary>

    /// <summary> The calculate method gives us a Vector3 position from a root situated between 0 and 1
    /// </summary>
    internal Vector3 Calculate(double t)
    {
        return new Vector3((float)(_coeffs[0, 0] * t * t + _coeffs[1, 0] * t + _coeffs[2, 0]),
                           (float)(_coeffs[0, 1] * t * t + _coeffs[1, 1] * t + _coeffs[2, 1]),
                           (float)(_coeffs[0, 2] * t * t + _coeffs[1, 2] * t + _coeffs[2, 2]));
    }

    /// <summary> The first derivative is the Vector3 tangent to the space curve at target root
    /// </summary>
    internal Vector3 CalculateFirstDerivative(double t)
    {
        return new Vector3((float)(2 * _coeffs[0, 0] * t + _coeffs[1, 0]),
                           (float)(2 * _coeffs[0, 1] * t + _coeffs[1, 1]),
                           (float)(2 * _coeffs[0, 2] * t + _coeffs[1, 2]));
    }

    /// <summary> The arc length is computed from the indefinite integral formula explicited in online docs.
    /// </summary>
    internal double ArcLength(double st, double ft)
    {

        var distX = Math.Abs(_coeffs[0,0]) > 0.001 ? IntegralLength(0, ft) - IntegralLength(0, st) : Math.Abs(_coeffs[1, 0] * ft - _coeffs[1, 0] * st);
        var distY = Math.Abs(_coeffs[0,1]) > 0.001 ? IntegralLength(1, ft) - IntegralLength(1, st) : Math.Abs(_coeffs[1, 1] * ft - _coeffs[1, 1] * st);
        var distZ = Math.Abs(_coeffs[0,2]) > 0.001 ? IntegralLength(2, ft) - IntegralLength(2, st) : Math.Abs(_coeffs[1, 2] * ft - _coeffs[1, 2] * st);

        return Math.Sqrt(distX * distX + distY * distY + distZ * distZ);
    }

    private double IntegralLength(int dim, double t)
    {
        var bo2A  = _coeffs[1,dim] / (2 * _coeffs[0,dim]);
        var sqrtP = Math.Sqrt(Math.Pow(t + bo2A, 2) + Math.Pow(1 / (2 * _coeffs[0,dim]), 2));
        return (_coeffs[0,dim] * t + _coeffs[1,dim] / 2) * sqrtP + 1 / (4 * _coeffs[0,dim]) * Math.Log(Math.Abs(t + bo2A + sqrtP));
    }

    #region Operators

    public static QuadraticPolynomial operator +(QuadraticPolynomial a, QuadraticPolynomial b)
    {
        var res = new QuadraticPolynomial();

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
                res._coeffs[i,j] = a._coeffs[i,j] + b._coeffs[i,j];
        }

        return res;
    }

    public static QuadraticPolynomial operator -(QuadraticPolynomial a, QuadraticPolynomial b)
    {
        var res = new QuadraticPolynomial();

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
                res._coeffs[i,j] = a._coeffs[i,j] - b._coeffs[i,j];
        }

        return res;
    }

    public static QuadraticPolynomial operator *(QuadraticPolynomial a, double b)
    {
        var res = new QuadraticPolynomial();

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
                res._coeffs[i, j] = a._coeffs[i, j] * b;
        }

        return res;
    }

    public static QuadraticPolynomial operator /(QuadraticPolynomial a, double b)
    {
        var res = new QuadraticPolynomial();

        for (var i = 0; i < 3; i++)
        {
            for(var j = 0; j < 3; j++)
                res._coeffs[i,j] = a._coeffs[i,j] / b;
        }

        return res;
    }

    #endregion

    #region Variables

    internal double[,] _coeffs = new double[3, 3];

    #endregion

}