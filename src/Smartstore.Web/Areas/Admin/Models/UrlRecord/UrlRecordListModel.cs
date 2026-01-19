namespace Smartstore.Admin.Models.UrlRecord
{
    [LocalizedDisplay("Admin.System.SeNames.")]
    public class UrlRecordListModel : ModelBase
    {
        [LocalizedDisplay("*Name")]
        public string SeName { get; set; }

        [LocalizedDisplay("*EntityName")]
        public string EntityName { get; set; }

        [LocalizedDisplay("*EntityId")]
        [AdditionalMetadata("invariant", true)]
        public int? EntityId { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool? IsActive { get; set; }

        [LocalizedDisplay("*Language")]
        public int? LanguageId { get; set; }
    }
}
