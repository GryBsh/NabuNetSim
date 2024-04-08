namespace Gry.Adapters;

public enum AdapterState : byte
{
    Stopped = 0,
    Starting = 1,
    Running = 2,
    Stopping = 4,
    Failed = 8
}
