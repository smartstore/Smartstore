namespace Smartstore.Core.Catalog.Products
{
    public class ProductClonedEvent
    {
        public ProductClonedEvent(Product source, Product clone)
        {
            Source = Guard.NotNull(source);
            Clone = Guard.NotNull(clone);
        }

        public Product Source { get; }
        public Product Clone { get; }
    }
}
