using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Lgc.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] enumerables)
    {
        foreach (var enumerable in enumerables)
            foreach (var item in enumerable)
                yield return item;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
    {
        foreach (var item in items)
            if (item is not null)
                yield return item;
    }
}