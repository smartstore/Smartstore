
using System.Text.Json.Serialization;
using Smartstore.Collections;
using Smartstore.Json;

namespace Smartstore.Core.Security
{
    [DefaultImplementation(typeof(PermissionNode))]
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

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Allow { get; set; }
    }
}