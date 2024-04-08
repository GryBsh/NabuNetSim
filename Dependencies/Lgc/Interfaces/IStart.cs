namespace Lgc;

/// <summary>
/// Defines a type which both configures the host builder and acts as registrar.
/// </summary>
public interface IStart : IRegister, IBuildHost { }