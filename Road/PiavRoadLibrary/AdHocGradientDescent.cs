using System;
using UnityEngine;

internal static class AdHocGradientDescent
{
    /// <summary>A classic regression technique, used here to approximate polynom arc length and inverse arc length for the navigation xml
    /// </summary>

    internal static double LinearRegressionBoldDriver(double[] s, double[] v, int n, double alphaValue)
    {
        _steps  = s;
        _values = v;

        _currentA = 0;

        var lastDistanceSquared = 0.0;
        for (var k = 0; k < _steps.Length; k++)
            lastDistanceSquared += Math.Pow(_steps[k] * _currentA - _values[k], 2);

        for (var i = 0; i < n && lastDistanceSquared > 0.0000001; i++)
        {
            var totalDistance = 0.0;
            for (var k = 0; k < _steps.Length; k++)
                totalDistance += _steps[k] * (_steps[k] * _currentA - _values[k]);
            totalDistance /= _steps.Length;

            _currentA = _currentA - alphaValue * totalDistance;

            var totalDistanceSquared  = 0.0;
            for (var k = 0; k < _steps.Length; k++)
                totalDistanceSquared += Math.Pow(_steps[k] * _currentA - _values[k], 2);

            if (totalDistanceSquared > lastDistanceSquared) alphaValue /= 2.0;
            else alphaValue                                            *= 1.1;
            lastDistanceSquared                                        =  totalDistanceSquared;
        }

        //Debug.Log(resStr);
        return _currentA;
    }

    internal static System.Tuple<double, double> QuadraticRegressionBoldDriver(double[] s, double[] v, int n, double alphaValue)
    {
        _steps                = s;
        _values               = v;

        _currentA = 0;
        _currentB = 0;

        var lastDistanceSquared = 0.0;
        for (var k = 0; k < _steps.Length; k++) lastDistanceSquared += Mathf.Pow((float)(_steps[k] * _steps[k] * _currentA + _steps[k] * _currentB - _values[k]), 2);

        for (var i = 0; i < n && lastDistanceSquared > 0.0000001; i++)
        {
            var totalDistance = 0.0;
            for (var k = 0; k < _steps.Length; k++) totalDistance += _steps[k] * _steps[k] * (_steps[k] * _steps[k] * _currentA + _steps[k] * _currentB - _values[k]);
            var tempA = _currentA - alphaValue * totalDistance;

            totalDistance                                        =  0.0;
            for (var k = 0; k < _steps.Length; k++) totalDistance += _steps[k] * (_steps[k] * _steps[k] * _currentA + _steps[k] * _currentB - _values[k]);
            var tempB = _currentB - alphaValue * totalDistance;

            _currentA = tempA;
            _currentB = tempB;

            var totalDistanceSquared = 0.0;
            for (var k = 0; k < _steps.Length; k++) totalDistanceSquared += Mathf.Pow((float)(_steps[k] * _steps[k] * _currentA + _steps[k] * _currentB - _values[k]), 2);

            if (totalDistanceSquared > lastDistanceSquared) alphaValue /= 2.0;
            else alphaValue                                            *= 1.1;
            lastDistanceSquared                                   =  totalDistanceSquared;
        }

        //Debug.Log(resStr);
        return new System.Tuple<double, double>(_currentA, _currentB);
    }

    internal static System.Tuple<double, double, double> CubicRegressionBoldDriver(double[] s, double[] v, int n, double alphaValue, double startA, double startB, double startC)
    {
        _steps  = s;
        _values = v;

        _currentA = startA;
        _currentB = startB;
        _currentC = startC;

        var lastDistanceSquared = 0.0;
        for (var k = 0; k < _steps.Length; k++)
            lastDistanceSquared += Mathf.Pow((float)(_steps[k] * _steps[k] * _steps[k] * _currentA + _steps[k] * _steps[k] * _currentB  + _steps[k] * _currentC -_values[k]), 2);

        int count = 0;

        for (var i = 0; i < n && lastDistanceSquared > 0.0001; i++)
        {
            double totalDistanceA = 0.0, totalDistanceB = 0.0, totalDistanceC = 0.0;

            for (var k = 0; k < _steps.Length; k++)
            {
                var local = _steps[k] * _steps[k] * _steps[k] * _currentA + _steps[k] * _steps[k] * _currentB + _steps[k] * _currentC - _values[k];
                totalDistanceA += _steps[k] * _steps[k] * _steps[k] * local;
                totalDistanceB += _steps[k] * _steps[k] * local;
                totalDistanceC += _steps[k] * local;
            }
            totalDistanceA /= _steps.Length;
            totalDistanceB /= _steps.Length;
            totalDistanceC /= _steps.Length;

            _currentA = _currentA - alphaValue * totalDistanceA;
            _currentB = _currentB - alphaValue * totalDistanceB;
            _currentC = _currentC - alphaValue * totalDistanceC;

            var totalDistanceSquared = 0.0;
            for (var k = 0; k < _steps.Length; k++)
                totalDistanceSquared += Mathf.Pow((float)(_steps[k] * _steps[k] * _currentA + _steps[k] * _currentB - _values[k]), 2);

            if (totalDistanceSquared > lastDistanceSquared) alphaValue /= 2.0;
            else alphaValue *= 1.1;
            lastDistanceSquared =  totalDistanceSquared;

            count++;
        }

        Debug.Log(count);

        //Debug.Log(resStr);
        return new System.Tuple<double, double,double>(_currentA, _currentB,_currentC);
    }

    internal static System.Tuple<double, double, double> PolyfitRegression(double[] s, double[] v)
    {
        var poly = new PolyFit(s, v, 3);
        Debug.Log(poly.Coeff[0]);
        return new Tuple<double, double, double>(poly.Coeff[3], poly.Coeff[2], poly.Coeff[1]);
    }

    #region Variables

    private static double _currentA;
    private static double _currentB;
    private static double _currentC;

    private static double[] _steps;
    private static double[] _values;

    #endregion
}