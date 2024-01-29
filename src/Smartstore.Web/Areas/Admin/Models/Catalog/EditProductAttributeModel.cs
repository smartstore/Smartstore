namespace Smartstore.Admin.Models.Catalog
{
    public partial class EditProductAttributeModel : TabbableModel
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; }
        public string ProductAttributeName { get; init; }

        public bool IsListTypeAttribute { get; init; }
    }
}
