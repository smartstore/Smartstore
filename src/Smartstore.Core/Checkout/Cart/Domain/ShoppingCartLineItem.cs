using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Core.Checkout.Cart
{
    // TODO: (mg) (core) describe ShoppingCartLineItem when ready.
    public partial class ShoppingCartLineItem
    {
        public ShoppingCartLineItem(OrganizedShoppingCartItem item)
        {
            Guard.NotNull(item, nameof(item));

            Item = item;
        }

        public OrganizedShoppingCartItem Item { get; private set; }

        public CalculatedPrice UnitPrice { get; init; }

        public CalculatedPrice Subtotal { get; init; }
    }
}
