using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.ShoppingCart.")]
    public class ShoppingCartSettingsModel : ILocalizedModel<ShoppingCartSettingsLocalizedModel>
    {
        [LocalizedDisplay("*DisplayCartAfterAddingProduct")]
        public bool DisplayCartAfterAddingProduct { get; set; }

        [LocalizedDisplay("*DisplayWishlistAfterAddingProduct")]
        public bool DisplayWishlistAfterAddingProduct { get; set; }

        [LocalizedDisplay("*MaximumShoppingCartItems")]
        public int MaximumShoppingCartItems { get; set; }

        [LocalizedDisplay("*MaximumWishlistItems")]
        public int MaximumWishlistItems { get; set; }

        [LocalizedDisplay("*AllowActivatableCartItems")]
        public bool AllowActivatableCartItems { get; set; }

        [LocalizedDisplay("*AllowOutOfStockItemsToBeAddedToWishlist")]
        public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }

        [LocalizedDisplay("*ShowProductImagesOnShoppingCart")]
        public bool ShowProductImagesOnShoppingCart { get; set; }

        [LocalizedDisplay("*ShowProductBundleImagesOnShoppingCart")]
        public bool ShowProductBundleImagesOnShoppingCart { get; set; }

        [LocalizedDisplay("*ShowProductImagesOnWishList")]
        public bool ShowProductImagesOnWishList { get; set; }

        [LocalizedDisplay("*ShowProductBundleImagesOnWishList")]
        public bool ShowProductBundleImagesOnWishList { get; set; }

        [LocalizedDisplay("*ShowDiscountBox")]
        public bool ShowDiscountBox { get; set; }

        [LocalizedDisplay("*ShowGiftCardBox")]
        public bool ShowGiftCardBox { get; set; }

        [LocalizedDisplay("*CrossSellsNumber")]
        public int CrossSellsNumber { get; set; }

        [LocalizedDisplay("*EmailWishlistEnabled")]
        public bool EmailWishlistEnabled { get; set; }

        [LocalizedDisplay("*AllowAnonymousUsersToEmailWishlist")]
        public bool AllowAnonymousUsersToEmailWishlist { get; set; }

        [LocalizedDisplay("*MiniShoppingCartEnabled")]
        public bool MiniShoppingCartEnabled { get; set; }

        [LocalizedDisplay("*ShowProductImagesInMiniShoppingCart")]
        public bool ShowProductImagesInMiniShoppingCart { get; set; }

        [LocalizedDisplay("*ShowEssentialAttributesInMiniShoppingCart")]
        public bool ShowEssentialAttributesInMiniShoppingCart { get; set; }

        [LocalizedDisplay("*ShowConfirmOrderLegalHint")]
        public bool ShowConfirmOrderLegalHint { get; set; }

        [LocalizedDisplay("*DeliveryTimesInShoppingCart")]
        public DeliveryTimesPresentation DeliveryTimesInShoppingCart { get; set; }

        [LocalizedDisplay("*ShowShortDesc")]
        public bool ShowShortDesc { get; set; }

        [LocalizedDisplay("*ShowWeight")]
        public bool ShowWeight { get; set; }

        [LocalizedDisplay("*ShowBasePrice")]
        public bool ShowBasePrice { get; set; }

        [LocalizedDisplay("*ShowLinkedAttributeValueQuantity")]
        public bool ShowLinkedAttributeValueQuantity { get; set; }

        [LocalizedDisplay("*MaxQuantityInputDropdownItems")]
        public int MaxQuantityInputDropdownItems { get; set; }

        [LocalizedDisplay("*CheckoutProcess")]
        public string CheckoutProcess { get; set; }

        [LocalizedDisplay("*QuickCheckoutEnabled")]
        public bool QuickCheckoutEnabled { get; set; }

        [LocalizedDisplay("*CustomersCanChangePreferredShipping")]
        public bool CustomersCanChangePreferredShipping { get; set; }

        [LocalizedDisplay("*CustomersCanChangePreferredPayment")]
        public bool CustomersCanChangePreferredPayment { get; set; }

        [LocalizedDisplay("*ShowCommentBox")]
        public bool ShowCommentBox { get; set; }

        [LocalizedDisplay("*ShowEsdRevocationWaiverBox")]
        public bool ShowEsdRevocationWaiverBox { get; set; }

        [LocalizedDisplay("*NewsletterSubscription")]
        public CheckoutNewsletterSubscription NewsletterSubscription { get; set; }
        public SelectList AvailableNewsletterSubscriptions { get; set; }

        [LocalizedDisplay("*ThirdPartyEmailHandOver")]
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }
        public SelectList AvailableThirdPartyEmailHandOver { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 5)]
        [LocalizedDisplay("*ThirdPartyEmailHandOverLabel")]
        public string ThirdPartyEmailHandOverLabel { get; set; }

        public List<ShoppingCartSettingsLocalizedModel> Locales { get; set; } = new();

        [LocalizedDisplay("*AddProductsToBasketInSinglePositions")]
        public bool AddProductsToBasketInSinglePositions { get; set; }
    }

    public class ShoppingCartSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.ShoppingCart.ThirdPartyEmailHandOverLabel")]
        public string ThirdPartyEmailHandOverLabel { get; set; }
    }

    public partial class ShoppingCartSettingsValidator : SettingModelValidator<ShoppingCartSettingsModel, ShoppingCartSettings>
    {
        public ShoppingCartSettingsValidator()
        {
            RuleFor(x => x.MaximumShoppingCartItems).GreaterThan(0);
            RuleFor(x => x.MaximumWishlistItems).GreaterThan(0);
            RuleFor(x => x.CrossSellsNumber).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxQuantityInputDropdownItems).GreaterThanOrEqualTo(0);
        }
    }
}
