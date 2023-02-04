namespace Nabu
{
    public interface ISimulation
    {
        void StartAdaptor(AdaptorSettings settings);
        void StopAdaptor(AdaptorSettings settings);
        void ToggleAdaptor(AdaptorSettings settings);
    }
}