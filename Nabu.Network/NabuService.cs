using Microsoft.Extensions.Logging;

namespace Nabu;

public abstract class NabuService : NabuBase
{
  
    protected AdaptorSettings settings { get; set; }
    public NabuService(IConsole logger, AdaptorSettings settings) : base(logger)
    {
        
        this.settings = settings;
       
    }
    
    protected override string LogMessage(string message)
    {
        return $"{settings.Type}:{settings.Port}:{message}";
    }

   

}