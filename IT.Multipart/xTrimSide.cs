namespace IT.Multipart;

internal static class xTrimSide
{
    public static bool HasStart(this TrimSide side)
        => (side & TrimSide.Start) == TrimSide.Start;

    public static bool HasEnd(this TrimSide side)
        => (side & TrimSide.End) == TrimSide.End;
}