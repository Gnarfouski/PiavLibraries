using UnityEngine;

internal static class AdHocGradientDescent
{
    /// <summary>A classic regression technique, used here to approximate polynom arc length and inverse arc length for the navigation xml
    /// </summary>

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

    #region Variables

    private static double _currentA;
    private static double _currentB;

    private static double[] _steps;
    private static double[] _values;

    #endregion
}