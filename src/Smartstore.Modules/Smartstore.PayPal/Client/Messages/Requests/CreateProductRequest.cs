namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a product.
    /// </summary>
    public class CreateProductRequest : PayPalRequest2<CreateProductRequest, ProductMessage>
    {
        public CreateProductRequest()
            : base("/v1/catalogs/products", HttpMethod.Post)
        {
        }
    }
}