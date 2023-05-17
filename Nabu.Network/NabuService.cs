using Nabu.Services;

namespace Nabu;

public abstract class NabuService : NabuBase
{
  
    protected AdaptorSettings Settings { get; set; }
    public NabuService(IConsole logger, AdaptorSettings settings, string? label = null) : base(logger, label)
    {
        
        Settings = settings;
       
    }
    
    protected override string LogMessage(string message)
    {
        return $"{Settings.Type}:{Settings.Port}:{message}";
    }

}



