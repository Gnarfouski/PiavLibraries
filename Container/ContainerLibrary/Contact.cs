public class Contact
{
    protected Contact(Segment origin, Segment target)
    {
        _origin = origin;
        _target = target;
    }

    #region Variables

    internal Segment _origin;
    internal Segment _target;

    #endregion
}