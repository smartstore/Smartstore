namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class RestoreProductModel : ModelBase
    {
        public string ProductIds { get; set; }

        [LocalizedDisplay("*Published")]
        public bool? PublishAfterRestore { get; set; }
    }
}
