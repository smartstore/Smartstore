using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Catalog.Attributes.Modelling;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore.Core.Catalog.Attributes
{
    [ModelBinder(typeof(ProductAttributeQueryModelBinder))]
    [ValidateNever]
    public class ProductVariantQuery
    {
        private readonly List<ProductVariantQueryItem> _variants = new();
        private readonly List<GiftCardQueryItem> _giftCards = new();
        private readonly List<CheckoutAttributeQueryItem> _checkoutAttributes = new();

        public int VariantCombinationId { get; set; }

        public IReadOnlyList<ProductVariantQueryItem> Variants => _variants;

        public IReadOnlyList<GiftCardQueryItem> GiftCards => _giftCards;

        public IReadOnlyList<CheckoutAttributeQueryItem> CheckoutAttributes => _checkoutAttributes;

        public void AddVariant(ProductVariantQueryItem item)
        {
            var exists = _variants.Any(x =>
                x.ProductId == item.ProductId &&
                x.BundleItemId == item.BundleItemId &&
                x.AttributeId == item.AttributeId &&
                x.VariantAttributeId == item.VariantAttributeId &&
                x.Value == item.Value
            );

            if (!exists)
            {
                _variants.Add(item);
            }
        }

        public void AddGiftCard(GiftCardQueryItem item)
        {
            _giftCards.Add(item);
        }

        public void AddCheckoutAttribute(CheckoutAttributeQueryItem item)
        {
            _checkoutAttributes.Add(item);
        }

        public string GetGiftCardValue(int productId, int bundleItemId, string name)
        {
            return _giftCards.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.BundleItemId == bundleItemId &&
                x.Name.EqualsNoCase(name))
                ?.Value;
        }

        public GiftCardInfo GetGiftCardInfo(int productId, int bundleItemId)
        {
            var values = _giftCards
                .Where(x => x.ProductId == productId && x.BundleItemId == bundleItemId)
                .ToDictionarySafe(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

            var giftCardInfo = new GiftCardInfo
            {
                RecipientName = values.Get(nameof(GiftCardInfo.RecipientName)),
                RecipientEmail = values.Get(nameof(GiftCardInfo.RecipientEmail)),
                SenderName = values.Get(nameof(GiftCardInfo.SenderName)),
                SenderEmail = values.Get(nameof(GiftCardInfo.SenderEmail)),
                Message = values.Get(nameof(GiftCardInfo.Message))
            };

            return giftCardInfo;
        }

        public override string ToString()
        {
            var groups = new string[]
            {
                string.Join('&', Variants.Select(x => x.ToString())),
                string.Join('&', GiftCards.Select(x => x.ToString())),
                string.Join('&', CheckoutAttributes.Select(x => x.ToString()))
            };

            return string.Join('&', groups.Where(x => x.HasValue()));
        }
    }
}
