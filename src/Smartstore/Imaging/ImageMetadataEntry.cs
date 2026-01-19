#nullable enable

using System.Text.Json.Serialization;

namespace Smartstore.Imaging
{
    public enum ImageMetadataProfile
    {
        Iptc,
        Exif,
        Other = 100
    }
    
    public sealed class ImageMetadataEntry
    {
        public ImageMetadataEntry(string tag, string value, ImageMetadataProfile profile)
        {
            Guard.NotEmpty(tag);

            Tag = tag;
            Value = value;
            Profile = profile;
        }

        [JsonPropertyName("tag")]
        public string Tag { get; }

        [JsonPropertyName("value")]
        public string? Value { get; }

        [JsonPropertyName("profile")]
        public ImageMetadataProfile Profile { get; }
    }
}
