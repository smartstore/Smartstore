namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.List.")]
    public class DeletedProductListModel : ProductListModel
    {
        [LocalizedDisplay("*SearchWithOrders")]
        public bool? SearchWithOrders { get; set; }
    }
}
