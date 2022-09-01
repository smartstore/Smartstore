#nullable enable

using Newtonsoft.Json;

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
            Guard.NotEmpty(tag, nameof(tag));

            Tag = tag;
            Value = value;
            Profile = profile;
        }

        [JsonProperty("tag")]
        public string Tag { get; }

        [JsonProperty("value")]
        public string? Value { get; }

        [JsonProperty("profile")]
        public ImageMetadataProfile Profile { get; }
    }
}
