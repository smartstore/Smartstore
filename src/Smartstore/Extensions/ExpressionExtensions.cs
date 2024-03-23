using System.Reflection;
using System.Runtime.CompilerServices;

namespace Smartstore
{
    public static class ExpressionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyInfo ExtractPropertyInfo(this LambdaExpression propertyAccessor)
        {
            return propertyAccessor.ExtractMemberInfo() as PropertyInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo ExtractFieldInfo(this LambdaExpression propertyAccessor)
        {
            return propertyAccessor.ExtractMemberInfo() as FieldInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo ExtractMethodInfo(this LambdaExpression propertyAccessor)
        {
            return propertyAccessor.ExtractMemberInfo() as MethodInfo;
        }

        public static MemberInfo ExtractMemberInfo(this LambdaExpression memberAccessor)
        {
            Guard.NotNull(memberAccessor);

            if (memberAccessor.Body is UnaryExpression unaryExp)
            {
                if (unaryExp.Operand is MemberExpression memberExp)
                {
                    return memberExp.Member;
                }
            }
            else if (memberAccessor.Body is MemberExpression memberExpr)
            {
                return memberExpr.Member;
            }
            else if (memberAccessor.Body is MethodCallExpression methodCall)
            {
                return methodCall.Method;
            }

            throw new ArgumentException($"The member accessor expression [{memberAccessor}] is not in the expected format 'o => o.PropertyOrField' or 'o => o.MethodCall(...)'.", nameof(memberAccessor));
        }

        public static PropertyInvoker<T, TProp> GetPropertyInvoker<T, TProp>(this Expression<Func<T, TProp>> expression)
        {
            if (expression.Body is not MemberExpression member)
            {
                throw new ArgumentException($"Expression body must refer to a property.", nameof(expression));
            }

            if (member.Member is not PropertyInfo pi)
            {
                throw new ArgumentException($"Expression body member must refer to a property.", nameof(expression));
            }

            return new PropertyInvoker<T, TProp>(pi);
        }
    }

    public sealed class PropertyInvoker<T, TProp>
    {
        internal PropertyInvoker(PropertyInfo prop)
        {
            Property = prop;
        }

        public PropertyInfo Property { get; }

        public TProp Invoke(T obj)
        {
            return (TProp)Property.GetValue(obj);
        }

        public static implicit operator Func<T, TProp>(PropertyInvoker<T, TProp> obj)
        {
            return obj.Invoke;
        }
    }
}
