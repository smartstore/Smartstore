#nullable enable

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Smartstore.Web.Models.Media
{
    /// <summary>
    /// Represents commands to edit a media file such as an image or video.
    /// </summary>
    [DataContract]
    public partial class MediaEditModel
    {
        [JsonProperty("commands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DataMember(Name = "commands")]
        public MediaEditCommandModel[]? Commands { get; set; }

        public virtual string? ToJson()
        {
            if (IsDefault())
            {
                return null;
            }

            return JsonConvert.SerializeObject(this);
        }

        private bool IsDefault()
        {
            return Commands.IsNullOrEmpty();
        }
    }

    [DataContract]
    public partial class MediaEditCommandModel
    {
        [JsonProperty("cmd")]
        [DataMember(Name = "cmd")]
        public required string Command { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        [DataMember(Name = "value")]
        public string? Value { get; set; }
    }
}
