using System.Collections;
using System.Collections.Concurrent;

namespace Smartstore.Templating.Liquid
{
    internal static class LiquidUtility
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object>> _typeWrapperCache = new();

        internal static object CreateSafeObject(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is TestDrop || value is IFormattable)
            {
                return value;
            }

            var valueType = value.GetType();

            var fn = _typeWrapperCache.GetOrAdd(valueType, key => 
            {
                if (value is IDictionary<string, object> dict)
                {
                    return x => new DictionaryDrop((IDictionary<string, object>)x);
                }
                else if (valueType.IsEnumerableType(out var elementType))
                {
                    var seqType = elementType;
                    if (!IsSafeType(seqType))
                    {
                        return x => new EnumerableWrapper((IEnumerable)x);
                    }
                }
                else if (valueType.IsPlainObjectType())
                {
                    return x => new ObjectDrop(x);
                }

                return null;
            });

            return fn?.Invoke(value) ?? value;
        }

        public static bool IsSafeType(Type type)
        {
            return type.IsBasicOrNullableType();
        }
    }
}
