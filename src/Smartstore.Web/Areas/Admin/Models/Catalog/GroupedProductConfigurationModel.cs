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

    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationModel2 : ILocalizedModel<GroupedProductConfigurationLocalizedModel>
    {
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

        public List<GroupedProductConfigurationLocalizedModel> Locales { get; set; } = [];
    }

    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
    }
}
