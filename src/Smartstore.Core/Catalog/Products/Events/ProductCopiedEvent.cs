namespace Smartstore.Core.Catalog.Products
{
    public class ProductCopiedEvent
    {
        public ProductCopiedEvent(Product originalProduct, Product newProduct)
        {
            Guard.NotNull(originalProduct, nameof(Product));
            Guard.NotNull(newProduct, nameof(Product));

            OriginalProduct = originalProduct;
            NewProduct = newProduct;
        }

        public Product OriginalProduct { get; set; }
        public Product NewProduct { get; set; }
    }
}
