namespace Nabu
{
    /// <summary>
    ///     Version Numbers and adaptor Id
    ///     Used in various places
    /// </summary>
    public static class Emulator
    {
        public const string Branch = "release";
        public const int Build = 0;
        public const int Major = 1;
        public const int Minor = 3;
        public const int Release = 1;
        public static readonly string Id = $"NABU NetSim v{Version}";
        public static readonly string Version = $"{Major}.{Minor}.{Build}-{Branch}{Release}";
    }
}