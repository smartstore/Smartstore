using System.Collections.Frozen;
using System.Dynamic;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.ComponentModel.TypeConverters;

public class DictionaryTypeConverterProvider : ITypeConverterProvider
{
    private static readonly ITypeConverter Default = new DictionaryTypeConverter<IDictionary<string, object>>();

    // Cache converter instances that are stateless (same behavior for all calls).
    private static readonly ITypeConverter RouteValueDictionaryConverter = new DictionaryTypeConverter<RouteValueDictionary>();
    private static readonly ITypeConverter ExpandoObjectConverter = new DictionaryTypeConverter<ExpandoObject>();
    private static readonly ITypeConverter HybridExpandoConverter = new DictionaryTypeConverter<HybridExpando>();
    private static readonly ITypeConverter FrozenDictionaryConverter = new DictionaryTypeConverter<FrozenDictionary<string, object>>();
    private static readonly ITypeConverter JsonObjectConverter = new JsonObjectConverter();

    public ITypeConverter GetConverter(Type type)
    {
        // Fast-path exact type matches first (cheaper than generic-shape reflection checks).
        if (type == typeof(RouteValueDictionary))
            return RouteValueDictionaryConverter;

        if (type == typeof(ExpandoObject))
            return ExpandoObjectConverter;

        if (type == typeof(HybridExpando))
            return HybridExpandoConverter;

        if (type == typeof(FrozenDictionary<string, object>))
            return FrozenDictionaryConverter;

        if (type == typeof(JsonObject))
            return JsonObjectConverter;

        if (type == typeof(IDictionary<string, object>) || type == typeof(Dictionary<string, object>))
            return Default;

        // Only then pay the (potentially) more expensive generic type-shape check.
        if (!type.IsClosedGenericTypeOf(typeof(IDictionary<,>)))
            return null;

        return null;
    }
}
