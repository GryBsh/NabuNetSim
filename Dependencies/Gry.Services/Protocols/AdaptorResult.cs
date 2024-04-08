namespace Gry.Protocols;

public record AdaptorResult<T>(bool IsExpected, T Result);
