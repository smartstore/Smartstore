namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationModel
    {
        [LocalizedDisplay("*PageSize")]
        public int PageSize { get; set; } = 20;

        [LocalizedDisplay("*SearchMinAssociatedCount")]
        public int SearchMinAssociatedCount { get; set; } = 10;

        [LocalizedDisplay("*Collapsible")]
        public bool Collapsible { get; set; }

        [LocalizedDisplay("*HeaderFields")]
        public string[] HeaderFields { get; set; }
    }
}
