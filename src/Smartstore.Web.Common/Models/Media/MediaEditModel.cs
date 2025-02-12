#nullable enable

using Newtonsoft.Json;

namespace Smartstore.Web.Models.Media
{
    /// <summary>
    /// Represents commands to edit a media file such as an image or video.
    /// </summary>
    public partial class MediaEditModel
    {
        [JsonProperty("commands")]
        public required List<MediaEditCommand> Commands { get; set; }

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

    public partial class MediaEditCommand
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string? Value { get; set; }
    }
}
