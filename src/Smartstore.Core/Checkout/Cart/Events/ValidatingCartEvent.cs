using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Customers;
using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class ValidatingCartEvent
    {
        public ValidatingCartEvent(
            IList<OrganizedShoppingCartItem> cart,
            IList<string> warnings,
            Customer customer)
        {
            Cart = cart;
            Warnings = warnings;
            Customer = customer;
        }

        public Customer Customer { get; set; }

        public IList<OrganizedShoppingCartItem> Cart { get; set; }

        public IList<string> Warnings { get; set; }

        public ActionResult Result { get; set; }
    }
}