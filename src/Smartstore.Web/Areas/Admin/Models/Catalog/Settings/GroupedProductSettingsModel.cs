namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Configuration.Settings.Catalog.")]
    public class GroupedProductSettingsModel : ILocalizedModel<GroupedProductSettingsLocalizedModel>
    {
        [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.Title")]
        public string AssociatedProductsTitle { get; set; }

        [LocalizedDisplay("*AssociatedProductsPageSize")]
        public int AssociatedProductsPageSize { get; set; }

        [LocalizedDisplay("*SearchMinAssociatedProductsCount")]
        public int SearchMinAssociatedProductsCount { get; set; }

        [LocalizedDisplay("*CollapsibleAssociatedProducts")]
        public bool CollapsibleAssociatedProducts { get; set; }

        [LocalizedDisplay("*CollapsibleAssociatedProductsHeaders")]
        public string[] CollapsibleAssociatedProductsHeaders { get; set; }

        public List<GroupedProductSettingsLocalizedModel> Locales { get; set; } = [];
    }

    public class GroupedProductSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.Title")]
        public string AssociatedProductsTitle { get; set; }
    }
}
