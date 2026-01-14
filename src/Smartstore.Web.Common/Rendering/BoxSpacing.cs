using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Smartstore.Web.Rendering
{
    public class BoxSpacing
    {
        [Range(0, 6)]
        [JsonPropertyName("t")]
        public byte? Top { get; set; }

        [Range(0, 6)]
        [JsonPropertyName("r")]
        public byte? Right { get; set; }

        [Range(0, 6)]
        [JsonPropertyName("b")]
        public byte? Bottom { get; set; }

        [Range(0, 6)]
        [JsonPropertyName("l")]
        public byte? Left { get; set; }
    }
}
