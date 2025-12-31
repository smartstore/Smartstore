using System.Text.Json.Serialization;

namespace Smartstore.Core.Content.Media
{
    public enum ImageDimension
    {
        VerySmall = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
        VeryLarge = 4
    }

    public partial class MediaFilesFilter
    {
        [JsonPropertyName("mediaTypes")]
        public string[] MediaTypes { get; set; }

        [JsonPropertyName("mimeTypes")]
        public string[] MimeTypes { get; set; }

        [JsonPropertyName("extensions")]
        public string[] Extensions { get; set; }

        [JsonPropertyName("dimensions")]
        public ImageDimension[] Dimensions { get; set; }

        [JsonPropertyName("tags")]
        public int[] Tags { get; set; }

        [JsonPropertyName("hidden")]
        public bool? Hidden { get; set; }

        [JsonPropertyName("deleted")]
        public bool? Deleted { get; set; }

        [JsonPropertyName("term")]
        public string Term { get; set; }

        [JsonPropertyName("exact")]
        public bool ExactMatch { get; set; }

        [JsonPropertyName("includeAlt")]
        public bool IncludeAltForTerm { get; set; }
    }

    public partial class MediaSearchQuery : MediaFilesFilter
    {
        [JsonPropertyName("folderId")]
        public int? FolderId { get; set; }

        [JsonPropertyName("deep")]
        public bool DeepSearch { get; set; }


        [JsonPropertyName("page")]
        public int PageIndex { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = int.MaxValue;

        [JsonPropertyName("sortBy")]
        public string SortBy { get; set; } = nameof(MediaFile.Id);

        [JsonPropertyName("sortDesc")]
        public bool SortDesc { get; set; }
    }
}
