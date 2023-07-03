namespace Napa
{
    public static class AdaptorFeatures
    {
        public static string NHACPv0 { get; } = nameof(NHACPv0).ToLowerInvariant();
        public static string NHACPv01 { get; } = nameof(NHACPv01).ToLowerInvariant();
        public static string RetroNet { get; } = nameof(RetroNet).ToLowerInvariant();
        public static string RetroNetServer { get; } = nameof(RetroNetServer).ToLowerInvariant();
        public static string ExploitLoader { get; } = nameof(ExploitLoader).ToLowerInvariant();
    }
}