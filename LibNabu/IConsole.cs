namespace Nabu;

public interface IConsole
{
    void Write(string message);
    void WriteWarning(string message);
    void WriteVerbose(string message);
    void WriteError(string message, Exception? exception = null);
    void WriteWarning(string message, Exception? exception = null);
    
}

public interface IConsole<T> : IConsole { }