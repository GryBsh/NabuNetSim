using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu;

public static partial class NabuLib
{
    public static void StartSafeNoGC(long requires)
    {
        try { GC.TryStartNoGCRegion(requires); } catch { }
    }

    public static void EndSafeNoGC()
    {
        try { GC.EndNoGCRegion(); } catch { }
    }
}
