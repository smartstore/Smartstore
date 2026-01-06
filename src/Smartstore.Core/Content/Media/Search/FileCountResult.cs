using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Smartstore.Core.Content.Media
{
    public partial class FileCountResult
    {
        [IgnoreDataMember]
        public MediaFilesFilter Filter { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("trash")]
        public int Trash { get; set; }

        [JsonPropertyName("unassigned")]
        public int Unassigned { get; set; }

        [JsonPropertyName("transient")]
        public int Transient { get; set; }

        [JsonPropertyName("orphan")]
        public int Orphan { get; set; }

        [JsonPropertyName("folders")]
        public IDictionary<int, int> Folders { get; set; }
    }
}