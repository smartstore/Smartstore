using System.Net;
using Amazon.Pay.API.Types;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Services
{
    public class AmazonPayException : PaymentException
    {
        public AmazonPayException(string message)
            : this(message, (Exception)null)
        {
        }

        public AmazonPayException(string message, Exception innerException)
            : base(message, innerException, AmazonPayProvider.SystemName)
        {
            RedirectRoute = CreateRouteValues();
        }

        public AmazonPayException(AmazonPayResponse response)
            : this(response.GetShortMessage(), response)
        {
        }

        /// <param name="message">Buyer friendly error message.</param>
        /// <param name="response">AmazonPay API response.</param>
        /// <remarks>
        /// The full error message is encapsulated via an inner exception
        /// so that the buyer does not see these details that are meaningless to him.
        /// </remarks>
        public AmazonPayException(string message, AmazonPayResponse response)
            : base(message,
                  new PaymentResponse((HttpStatusCode)response.Status, response.Headers),
                  new Exception(response.GetFullMessage()),
                  AmazonPayProvider.SystemName)
        {
            RedirectRoute = CreateRouteValues();
        }

        private RouteValueDictionary CreateRouteValues()
        {
            // Redirect back to where the payment button is.
            return new()
            {
                { "controller", "ShoppingCart" },
                { "action", nameof(ShoppingCartController.Cart) }
            };
        }
    }
}
