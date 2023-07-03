namespace Nabu;

public static partial class NabuLib
{
    public static bool StartSafeNoGC(long requires)
    {
        try
        {
            return GC.TryStartNoGCRegion(requires);
        }
        catch
        {
            return false;
        }
    }

    public static void EndSafeNoGC()
    {
        try { GC.EndNoGCRegion(); } catch { }
    }
}