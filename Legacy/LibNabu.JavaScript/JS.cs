using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;

namespace Nabu.JavaScript;

public static class JS
{
    public static ITypedArray<T> CreateArray<T>(this V8ScriptEngine engine, int length)
    {
        var type = typeof(T);
        var jsType = type switch
        {
            _ when type == typeof(byte) => "UInt8Array",
            _ when type == typeof(short) => "UInt16Array",
            _ => "Array"
        };

        return (ITypedArray<T>)FuncNewJsArray(engine, jsType)(length);
    }

    private static dynamic FuncNewJsArray(V8ScriptEngine engine, string type)
    {
        return engine.Evaluate(
            $"  (function (length) {{\n" +
            $"      return new {type}(length);\n" +
            $"  }}).valueOf()\n"
        );
    }
}