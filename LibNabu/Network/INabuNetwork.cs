namespace Nabu.Network
{
    public interface INabuNetwork
    {
        IEnumerable<NabuProgram> Programs(AdaptorSettings settings);
        Task<(ImageType, byte[])> Request(AdaptorSettings settings, int pak);
        ProgramSource Source(AdaptorSettings settings);

        void BackgroundRefresh(RefreshType refresh);

        void UncachePak(AdaptorSettings settings, int pak);
    }
}