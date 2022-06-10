using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Smartstore.ComponentModel;

namespace Smartstore.Web.Modelling
{
    internal class SmartTempDataSerializer : TempDataSerializer
    {
        private readonly IJsonSerializer _serializer;

        public SmartTempDataSerializer(IJsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public override bool CanSerializeType(Type type)
        {
            return _serializer.CanSerialize(type);
        }

        public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
        {
            if (_serializer.TryDeserialize(typeof(IDictionary<string, object>), unprotectedData, false, out var result))
            {
                return (IDictionary<string, object>)result;
            }

            return null;
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            if (_serializer.TrySerialize(values, false, out var result))
            {
                return result;
            }

            return Array.Empty<byte>();
        }
    }
}
