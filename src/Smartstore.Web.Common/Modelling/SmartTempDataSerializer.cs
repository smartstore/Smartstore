using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Smartstore.Json;

namespace Smartstore.Web.Modelling;

internal class SmartTempDataSerializer : TempDataSerializer
{
    private readonly IJsonSerializer _serializer;

    public SmartTempDataSerializer(IJsonSerializer serializer)
    {
        _serializer = serializer;
    }

    public override bool CanSerializeType(Type type)
        => _serializer.CanSerialize(type);

    public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
    {
        if (unprotectedData is null || unprotectedData.Length == 0)
            return new Dictionary<string, object>(StringComparer.Ordinal);

        if (_serializer.TryDeserialize(typeof(Dictionary<string, object>), unprotectedData, false, out var result) &&
            result is IDictionary<string, object> dict)
        {
            return dict;
        }

        return new Dictionary<string, object>(StringComparer.Ordinal);
    }

    public override byte[] Serialize(IDictionary<string, object> values)
    {
        if (_serializer.TrySerialize(values, false, out var result))
            return result;

        return Array.Empty<byte>();
    }
}
