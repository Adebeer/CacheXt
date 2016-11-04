using System.Diagnostics;

namespace CacheXt.NCache
{
    public static class Tracing
    {
        public static readonly TraceSwitch Switch = new TraceSwitch("NCache", "DistributedCache");
    }
}
