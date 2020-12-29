using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class ValidatingCartEvent
    {
        public ValidatingCartEvent(
            IList<OrganizedShoppingCartItem> cart,
            IList<string> warnings,
            Customer customer)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(warnings, nameof(warnings));
            Guard.NotNull(customer, nameof(customer));

            Cart = cart;
            Warnings = warnings;
            Customer = customer;
        }

        public Customer Customer { get; init; }

        public IList<OrganizedShoppingCartItem> Cart { get; init; }

        public IList<string> Warnings { get; init; }

        public ActionResult Result { get; set; }
    }
}