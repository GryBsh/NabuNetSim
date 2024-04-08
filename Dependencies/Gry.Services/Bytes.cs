using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gry;

public static partial class Bytes
{
    /// <summary>
    ///     Creates a String in X02 format from the given byte
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static string Format(byte b) => $"{b:X02}";

    /// <summary>
    ///     Creates a String of bytes in X02 format.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string Format(params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join(string.Empty, parts);
    }

    /// <summary>
    ///     Creates a separated String of bytes in X02 format
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string FormatSeparated(char separator, params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join(separator, parts);
    }

    /// <summary>
    ///     Creates a String of bytes in X02 format, separated by pipes (|)
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string FormatSeparated(params byte[] bytes) => FormatSeparated('|', bytes);
}
