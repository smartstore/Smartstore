namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationModel
    {
        // TODO: (mg) make localizable
        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("*PageSize")]
        public int? PageSize { get; set; }

        [LocalizedDisplay("*SearchMinAssociatedCount")]
        public int? SearchMinAssociatedCount { get; set; }

        [LocalizedDisplay("*Collapsible")]
        public bool? Collapsible { get; set; }

        [LocalizedDisplay("*HeaderFields")]
        public string[] HeaderFields { get; set; }
    }
}
