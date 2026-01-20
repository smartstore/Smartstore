using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Smartstore.Json;
using Smartstore.Json.Polymorphy;

namespace Smartstore.Web.Modelling;

internal class SmartTempDataSerializer : TempDataSerializer
{
    protected virtual JsonSerializerOptions CreateDefaultOptions()
        => SmartJsonOptions.Default;

    protected JsonSerializerOptions Options => field ??= CreateDefaultOptions();

    public override bool CanSerializeType(Type type)
        => true;

    public override IDictionary<string, object> Deserialize(byte[] value)
    {
        if (value is null || value.Length == 0)
            return new Dictionary<string, object>();

        return Options.DeserializePolymorphic<Dictionary<string, object>>(value);
    }

    public override byte[] Serialize(IDictionary<string, object> values)
    {
        if (values == null || values.Count == 0)
            return [];

        var result = Options.SerializePolymorphicToUtf8Bytes(values);
        return result;
    }
}
