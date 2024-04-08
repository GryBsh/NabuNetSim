namespace Nabu.Protocols.RetroNet
{
    public record FileDetails
    {
        public int FileSize { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Filename { get; set; } = string.Empty;

        public static implicit operator byte[](FileDetails file)
        {
            var fileName = NabuLib.ToSizedASCII(file.Filename, 64).ToArray();
            return NabuLib.Concat<byte>(
                BitConverter.GetBytes(file.FileSize),
                BitConverter.GetBytes((short)file.Created.Year),
                new[] {
                    (byte)file.Created.Month,
                    (byte)file.Created.Day,
                    (byte)file.Created.Hour,
                    (byte)file.Created.Minute,
                    (byte)file.Created.Second,
                },
                BitConverter.GetBytes((short)file.Modified.Year),
                new[]
                {
                    (byte)file.Modified.Month,
                    (byte)file.Modified.Day,
                    (byte)file.Modified.Hour,
                    (byte)file.Modified.Minute,
                    (byte)file.Modified.Second,
                },
                fileName
            ).ToArray();
        }
    }
}