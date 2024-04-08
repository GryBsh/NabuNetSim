using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Diagnostics;

namespace Lgc;

public static class ReflectionExtensions
{
    public static MemberInfo GetMember(this Expression expression)
        => expression switch
        {
            MemberExpression member => member.Member,
            UnaryExpression unary when unary.Operand is MemberExpression member => member.Member,
            null or _ =>
                throw new ArgumentException(
                   "Provided expression does not reflect a member",
                   nameof(expression)
                )
        };

    public static T? GetMemberValue<T>(this Expression expression, object target)
    {
        return GetMemberValue<T>(GetMember(expression), target);
    }

    public static T? GetMemberValue<T>(this MemberInfo member, object target, params object[] parameters)
        => member switch
        {
            PropertyInfo p => (T?)p.GetValue(target),
            FieldInfo f => (T?)f.GetValue(target),
            MethodInfo m when
                m.ReturnType == typeof(T) &&
                m.GetParameters().Length == parameters.Length
                    => (T?)m.Invoke(target, parameters),
            _ => default
        };
}