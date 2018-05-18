public class PointContact
{
    public PointContact(double st, double ft, Segment origin, Segment target)
    {
        Origin = origin;
        Target = target;
        OriginRoot = st;
        TargetRoot = ft;
    }

    #region Variables

    public double OriginRoot;
    public double TargetRoot;
    public Segment Origin;
    public Segment Target;

    #endregion
}