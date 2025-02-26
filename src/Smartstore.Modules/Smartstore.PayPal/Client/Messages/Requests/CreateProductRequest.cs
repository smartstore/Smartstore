namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a product.
    /// </summary>
    public class CreateProductRequest : PayPalRequest<ProductMessage>
    {
        public CreateProductRequest()
            : base("/v1/catalogs/products", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public CreateProductRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public CreateProductRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public CreateProductRequest WithBody(ProductMessage productMessage)
        {
            Body = productMessage;
            return this;
        }
    }
}