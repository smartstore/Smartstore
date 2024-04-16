namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationModel
    {
        [LocalizedDisplay("*PageSize")]
        public int PageSize { get; set; } = 20;

        [LocalizedDisplay("*Collapsable")]
        public bool Collapsable { get; set; }

        [LocalizedDisplay("*HeaderFields")]
        public string[] HeaderFields { get; set; }
    }
}
