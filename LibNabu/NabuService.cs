using Nabu.Services;

namespace Nabu;

public abstract class NabuService : NabuBase
{
  
    protected AdaptorSettings Settings { get; set; }
    public NabuService(ILog logger, AdaptorSettings settings, string? label = null) : base(logger, label)
    {
        
        Settings = settings;
       
    }
    
    protected override string LogMessage(string message)
    {
        var label = Label is not null ? Label : Settings.Type.ToString();
        return $"{label}:{Settings.Port}: {message}";
    }

}



