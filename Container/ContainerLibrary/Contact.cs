public class Contact
{
    protected Contact(Segment origin, Segment target)
    {
        Origin = origin;
        Target = target;
    }

    #region Variables

    public Segment Origin;
    public Segment Target;

    #endregion
}