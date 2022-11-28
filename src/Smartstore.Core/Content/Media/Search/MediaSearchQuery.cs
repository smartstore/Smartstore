using System.Runtime.Serialization;
using Newtonsoft.Json;

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

    [DataContract]
    public partial class MediaFilesFilter
    {
        [JsonProperty("mediaTypes")]
        [DataMember(Name = "mediaTypes")]
        public string[] MediaTypes { get; set; }

        [JsonProperty("mimeTypes")]
        [DataMember(Name = "mimeTypes")]
        public string[] MimeTypes { get; set; }

        [JsonProperty("extensions")]
        [DataMember(Name = "extensions")]
        public string[] Extensions { get; set; }

        [JsonProperty("dimensions")]
        [DataMember(Name = "dimensions")]
        public ImageDimension[] Dimensions { get; set; }

        [JsonProperty("tags")]
        [DataMember(Name = "tags")]
        public int[] Tags { get; set; }

        [JsonProperty("hidden")]
        [DataMember(Name = "hidden")]
        public bool? Hidden { get; set; }

        [JsonProperty("deleted")]
        [DataMember(Name = "deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("term")]
        [DataMember(Name = "term")]
        public string Term { get; set; }

        [JsonProperty("exact")]
        [DataMember(Name = "exact")]
        public bool ExactMatch { get; set; }

        [JsonProperty("includeAlt")]
        [DataMember(Name = "includeAlt")]
        public bool IncludeAltForTerm { get; set; }
    }

    [DataContract]
    public partial class MediaSearchQuery : MediaFilesFilter
    {
        [JsonProperty("folderId")]
        [DataMember(Name = "folderId")]
        public int? FolderId { get; set; }

        [JsonProperty("deep")]
        [DataMember(Name = "deep")]
        public bool DeepSearch { get; set; }


        [JsonProperty("page")]
        [DataMember(Name = "page")]
        public int PageIndex { get; set; }

        [JsonProperty("pageSize")]
        [DataMember(Name = "pageSize")]
        public int PageSize { get; set; } = int.MaxValue;

        [JsonProperty("sortBy")]
        [DataMember(Name = "sortBy")]
        public string SortBy { get; set; } = nameof(MediaFile.Id);

        [JsonProperty("sortDesc")]
        [DataMember(Name = "sortDesc")]
        public bool SortDesc { get; set; }
    }
}
