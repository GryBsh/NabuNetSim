using System.Reflection;

namespace Gry.Conversion;

public interface IConvert<TIn, TOut>
{
    bool CanConvert(TIn? input);
    TOut? Convert(TIn? input);
}

public class ParseMethodConverter<TOut> : IConvert<string,  TOut>
{
    static MethodInfo? ParseMethod<T>() => typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, [typeof(string)]);

    public static (bool, Func<string?, TOut?>) CanParse(string? source)
    {
        try
        {
            var parseMethod = ParseMethod<TOut>();

            return (
                source is not null &&
                parseMethod is not null &&
                parseMethod.ReturnType == typeof(TOut),
                v => (TOut?)parseMethod?.Invoke(null, [v])
            );
        }
        catch
        {
            return (false, v => default);
        }
    }

    public bool CanConvert(string? source)
    {
        var (should, _) = CanParse(source);
        return should;
    }

    public TOut? Convert(string? source)
    {
        var (result, parser) = CanParse(source);
        if (result) return parser(source);
        return default;
    }
}

