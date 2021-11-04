using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Products.AddNew.")]
    public partial class AddOrderProductModel : EntityModelBase
    {
        public int OrderId { get; set; }
        public ProductType ProductType { get; set; }
        public string Name { get; set; }

        [LocalizedDisplay("*UnitPriceInclTax")]
        public decimal UnitPriceInclTax { get; set; }
        [LocalizedDisplay("*UnitPriceExclTax")]
        public decimal UnitPriceExclTax { get; set; }

        [LocalizedDisplay("*TaxRate")]
        public decimal TaxRate { get; set; }

        [LocalizedDisplay("*Quantity")]
        public int Quantity { get; set; }

        [LocalizedDisplay("*SubTotalInclTax")]
        public decimal SubTotalInclTax { get; set; }
        [LocalizedDisplay("*SubTotalExclTax")]
        public decimal SubTotalExclTax { get; set; }

        [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.AdjustInventory")]
        public bool AdjustInventory { get; set; }

        [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.UpdateTotals")]
        public bool UpdateTotals { get; set; }


        public string GiftCardFieldPrefix 
            => GiftCardQueryItem.CreateKey(Id, 0, null);

        //....
    }
}
