namespace Smartstore.Core.Catalog.Products
{
    public class BarcodeScannedEvent
    {
        public BarcodeScannedEvent(int productId, string action)
        {
            ProductId = Guard.NotNull(productId);
            Action = action;
        }

        public int ProductId { get; }
        public string Action { get; }
    }
}
