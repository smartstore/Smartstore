using System.Reflection;
using System.Runtime.CompilerServices;
using Smartstore.ComponentModel;

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
            Guard.NotNull(memberAccessor, nameof(memberAccessor));

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

        public static FastPropertyInvoker<T, TProp> CompileFast<T, TProp>(
            this Expression<Func<T, TProp>> expression,
            PropertyCachingStrategy cachingStrategy = PropertyCachingStrategy.Cached)
        {
            if (!(expression.Body is MemberExpression member))
            {
                throw new ArgumentException($"Expression body must refer to a property.", nameof(expression));
            }

            if (!(member.Member is PropertyInfo pi))
            {
                throw new ArgumentException($"Expression body member must refer to a property.", nameof(expression));
            }

            var fastProp = FastProperty.GetProperty(pi, cachingStrategy);
            return new FastPropertyInvoker<T, TProp>(fastProp);
        }
    }

    public sealed class FastPropertyInvoker<T, TProp>
    {
        internal FastPropertyInvoker(FastProperty prop)
        {
            Property = prop;
        }

        public FastProperty Property { get; private set; }

        public TProp Invoke(T obj)
        {
            return (TProp)Property.GetValue(obj);
        }

        public static implicit operator Func<T, TProp>(FastPropertyInvoker<T, TProp> obj)
        {
            return obj.Invoke;
        }
    }
}
