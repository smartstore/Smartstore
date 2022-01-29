using System.Collections;

namespace Smartstore.Templating.Liquid
{
    internal static class LiquidUtility
    {
        private static readonly IDictionary<Type, Func<object, object>> _typeWrapperCache
            = new Dictionary<Type, Func<object, object>>();

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

            if (!_typeWrapperCache.TryGetValue(valueType, out var fn))
            {
                if (value is IDictionary<string, object> dict)
                {
                    fn = x => new DictionaryDrop((IDictionary<string, object>)x);
                }
                else if (valueType.IsEnumerableType(out var elementType))
                {
                    var seqType = elementType;
                    if (!IsSafeType(seqType))
                    {
                        fn = x => new EnumerableWrapper((IEnumerable)x);
                    }
                }
                else if (valueType.IsPlainObjectType())
                {
                    fn = x => new ObjectDrop(x);
                }

                _typeWrapperCache[valueType] = fn;
            }

            return fn?.Invoke(value) ?? value;
        }

        public static bool IsSafeType(Type type)
        {
            return type.IsBasicOrNullableType();
        }
    }
}
