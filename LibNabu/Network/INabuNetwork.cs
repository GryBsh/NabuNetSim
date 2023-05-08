namespace Nabu.Network
{
    public interface INabuNetwork
    {
        ProgramSource? Source(AdaptorSettings settings);
        IEnumerable<NabuProgram> Programs(AdaptorSettings settings);
        Task<(ImageType, Memory<byte>)> Request(AdaptorSettings settings, int pak);
        void BackgroundRefresh(RefreshType refresh);
        void UnCachePak(AdaptorSettings settings, int pak);
    }
}