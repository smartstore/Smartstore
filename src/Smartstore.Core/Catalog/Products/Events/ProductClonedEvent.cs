namespace Smartstore.Core.Catalog.Products
{
    public class ProductClonedEvent(Product source, Product clone)
    {
        public Product Source { get; } = Guard.NotNull(source);
        public Product Clone { get; } = Guard.NotNull(clone);
    }
}
