namespace Smartstore.Core.Catalog.Products
{
    public class BarcodeScannedEvent(int productId, string action)
    {
        public int ProductId { get; } = Guard.NotNull(productId);
        public string Action { get; } = action;
    }
}
