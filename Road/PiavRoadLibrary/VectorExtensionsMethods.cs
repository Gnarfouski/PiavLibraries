using System;

internal static class VectorExtensionMethods
{
    public static double[] Normalize(this double[] a)
    {
        var dist = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);

        return new[]
        {
            a[0] / dist,
            a[1] / dist,
            a[2] / dist
        };
    }

    public static double[] Cross(this double[] a, double[] b)
    {
        return new[]
        {
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0]
        };
    }

    public static double Dot(this double[] a, double[] b)
    {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }

    public static double AngleTo(this double[] a, double[] b)
    {
        return Math.Acos(a.Normalize().Dot(b.Normalize()));
    }

    public static double[] Add(this double[] a, double[] b)
    {
        return new[]
        {
            a[0] + b[0],
            a[1] + b[1],
            a[2] + b[2]
        };
    }

    public static double[] Sub(this double[] a, double[] b)
    {
        return new[]
        {
            a[0] - b[0],
            a[1] - b[1],
            a[2] - b[2]
        };
    }
}
