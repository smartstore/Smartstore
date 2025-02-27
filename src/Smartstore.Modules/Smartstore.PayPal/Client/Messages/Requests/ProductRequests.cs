namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a product.
    /// </summary>
    public class CreateProductRequest : PayPalRequest<CreateProductRequest, ProductMessage>
    {
        public CreateProductRequest()
            : base("/v1/catalogs/products", HttpMethod.Post)
        {
        }
    }
}
