namespace Smartstore.Core.Catalog.Products
{
    public class ProductClonedEvent
    {
        public ProductClonedEvent(Product source, Product clone)
        {
            Source = Guard.NotNull(source, nameof(source));
            Clone = Guard.NotNull(clone, nameof(clone));
        }

        public Product Source { get; }
        public Product Clone { get; }
    }
}
