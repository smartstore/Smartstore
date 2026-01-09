
using Smartstore.Collections;
using System.Text.Json;

using NSJ = Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;

namespace Smartstore.Core.Security
{
    [NSJ.JsonConverter(typeof(IPermissionNodeConverter))]
    [STJ.JsonConverter(typeof(IPermissionNodeStjConverter))]
    public interface IPermissionNode
    {
        int PermissionRecordId { get; }
        string SystemName { get; }
        bool? Allow { get; }
    }

    public class PermissionNode : IPermissionNode, IKeyedNode
    {
        object IKeyedNode.GetNodeKey() => SystemName;
        public int PermissionRecordId { get; set; }
        public string SystemName { get; set; }
        public bool? Allow { get; set; }
    }

    internal class IPermissionNodeConverter : NSJ.JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(IPermissionNode);

        public override object ReadJson(NSJ.JsonReader reader, Type objectType, object existingValue, NSJ.JsonSerializer serializer)
        {
            var node = new PermissionNode();
            serializer.Populate(reader, node);
            return node;
        }

        public override void WriteJson(NSJ.JsonWriter writer, object value, NSJ.JsonSerializer serializer)
            => throw new NotImplementedException();
    }

    internal class IPermissionNodeStjConverter : STJ.JsonConverter<IPermissionNode>
    {
        public override IPermissionNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<PermissionNode>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IPermissionNode value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}