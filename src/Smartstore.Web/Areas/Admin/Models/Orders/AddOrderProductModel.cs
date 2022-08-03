using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Products.AddNew.")]
    public partial class AddOrderProductModel : ModelBase
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }
        public ProductType ProductType { get; set; }
        public string Name { get; set; }

        [LocalizedDisplay("*UnitPriceInclTax")]
        public decimal UnitPriceInclTax { get; set; }
        [LocalizedDisplay("*UnitPriceExclTax")]
        public decimal UnitPriceExclTax { get; set; }

        [LocalizedDisplay("*SubTotalInclTax")]
        public decimal PriceInclTax { get; set; }
        [LocalizedDisplay("*SubTotalExclTax")]
        public decimal PriceExclTax { get; set; }

        [LocalizedDisplay("*TaxRate")]
        public decimal TaxRate { get; set; }

        [LocalizedDisplay("*Quantity")]
        public int Quantity { get; set; } = 1;

        [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.AdjustInventory")]
        public bool AdjustInventory { get; set; } = true;

        [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.UpdateTotals")]
        public bool UpdateTotals { get; set; }
        public bool ShowUpdateTotals { get; set; }

        public string GiftCardFieldPrefix
            => GiftCardQueryItem.CreateKey(ProductId, 0, null);

        public GiftCardModel GiftCard { get; set; } = new();

        public List<ProductVariantAttributeModel> ProductVariantAttributes { get; set; } = new();


        public partial class ProductVariantAttributeModel : ChoiceModel
        {
            public int ProductId { get; set; }
            public int BundleItemId { get; set; }
            public int ProductAttributeId { get; set; }

            public override string BuildControlId()
            {
                return ProductVariantQueryItem.CreateKey(ProductId, BundleItemId, ProductAttributeId, Id);
            }
        }

        public partial class ProductVariantAttributeValueModel : ChoiceItemModel
        {
            public override string GetItemLabel()
            {
                var label = QuantityInfo > 1
                    ? $"{QuantityInfo} x {Name}"
                    : Name;

                if (PriceAdjustment.HasValue())
                {
                    label += PriceAdjustment;
                }

                return label;
            }
        }

        [LocalizedDisplay("Products.GiftCard.")]
        public class GiftCardModel : ModelBase
        {
            public bool IsGiftCard { get; set; }
            public GiftCardType GiftCardType { get; set; }

            [LocalizedDisplay("*RecipientName")]
            public string RecipientName { get; set; }

            [LocalizedDisplay("*RecipientEmail")]
            public string RecipientEmail { get; set; }

            [LocalizedDisplay("*SenderName")]
            public string SenderName { get; set; }

            [LocalizedDisplay("*SenderEmail")]
            public string SenderEmail { get; set; }

            [UIHint("Textarea"), AdditionalMetadata("rows", 4)]
            [LocalizedDisplay("*Message")]
            public string Message { get; set; }
        }
    }
}
