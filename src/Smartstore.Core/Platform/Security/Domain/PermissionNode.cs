using Newtonsoft.Json;

namespace Smartstore.Core.Security
{
    [JsonConverter(typeof(IPermissionNodeConverter))]
    public interface IPermissionNode
    {
        int PermissionRecordId { get; }
        string SystemName { get; }
        bool? Allow { get; }
    }

    public class PermissionNode : IPermissionNode
    {
        public int PermissionRecordId { get; set; }
        public string SystemName { get; set; }
        public bool? Allow { get; set; }
    }

    internal class IPermissionNodeConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(IPermissionNode);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var node = new PermissionNode();
            serializer.Populate(reader, node);
            return node;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}