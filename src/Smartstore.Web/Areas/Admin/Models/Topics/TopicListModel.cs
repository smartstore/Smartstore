using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Topics
{
    [LocalizedDisplay("Admin.ContentManagement.Topics.Fields.")]
    public class TopicListModel : ModelBase
    {
        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("*RenderAsWidget")]
        public bool? RenderAsWidget { get; set; }

        [LocalizedDisplay("*WidgetZone")]
        public string WidgetZone { get; set; }

        public bool IsSingleStoreMode { get; set; }
    }
}
