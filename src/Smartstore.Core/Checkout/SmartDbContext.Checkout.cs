using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
        public DbSet<GiftCard> GiftCards { get; set; }
        public DbSet<Affiliate> Affiliates { get; set; }
        public DbSet<CheckoutAttribute> CheckoutAttributes { get; set; }
        public DbSet<CheckoutAttributeValue> CheckoutAttributeValues { get; set; }
    }
}