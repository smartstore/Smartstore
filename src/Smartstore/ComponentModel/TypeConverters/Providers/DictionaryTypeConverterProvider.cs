using System.Collections.Frozen;
using System.Dynamic;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;

namespace Smartstore.ComponentModel.TypeConverters
{
    public class DictionaryTypeConverterProvider : ITypeConverterProvider
    {
        static readonly ITypeConverter Default = new DictionaryTypeConverter<IDictionary<string, object>>();

        public ITypeConverter GetConverter(Type type)
        {
            if (!type.IsClosedGenericTypeOf(typeof(IDictionary<,>)))
            {
                return null;
            }

            if (type == typeof(RouteValueDictionary))
            {
                return new DictionaryTypeConverter<RouteValueDictionary>();
            }
            else if (type == typeof(ExpandoObject))
            {
                return new DictionaryTypeConverter<ExpandoObject>();
            }
            else if (type == typeof(HybridExpando))
            {
                return new DictionaryTypeConverter<HybridExpando>();
            }
            else if (type == typeof(FrozenDictionary<string, object>))
            {
                return new DictionaryTypeConverter<FrozenDictionary<string, object>>();
            }
            else if (type == typeof(JObject))
            {
                return new JObjectConverter();
            }
            else if (type == typeof(IDictionary<string, object>) || type == typeof(Dictionary<string, object>))
            {
                return Default;
            }

            return null;
        }
    }
}
