using UnityEngine;

internal class PointContact : Contact
{
    public PointContact(double st, double ft, Segment origin, Segment target)
        : base(origin, target)
    {
        OriginRoot = st;
        TargetRoot = ft;
        Distance = Vector3.Magnitude(RoadUtilities.Calculate(origin.Polynomial, st) - RoadUtilities.Calculate(target.Polynomial, ft));
    }

    #region Variables

    public double OriginRoot;
    public double TargetRoot;
    public double Distance;

    #endregion
}