using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Cart
{
    public class ShoppingCartSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether a custoemr should be redirected to the shopping cart page after adding a product to the cart/wishlist
        /// </summary>
        public bool DisplayCartAfterAddingProduct { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a custoemr should be redirected to the shopping cart page after adding a product to the cart/wishlist
        /// </summary>
        public bool DisplayWishlistAfterAddingProduct { get; set; }

        /// <summary>
        /// Gets or sets a value indicating maximum number of items in the shopping cart
        /// </summary>
        public int MaximumShoppingCartItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating maximum number of items in the wishlist
        /// </summary>
        public int MaximumWishlistItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether the customer can deactivate cart items.
        /// </summary>
        public bool AllowActivatableCartItems { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to allow out of stock items to be added to the wishlist
        /// </summary>
        public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to move items from wishlist to cart when clicking "Add to cart" button. Otherwise, they are copied
        /// </summary>
        public bool MoveItemsFromWishlistToCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the items from wishlist to ahopping cart button
        /// </summary>
        public bool ShowItemsFromWishlistToCartButton { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on shopping cart page
        /// </summary>
        public bool ShowProductImagesOnShoppingCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product bundle images on shopping cart page
        /// </summary>
        public bool ShowProductBundleImagesOnShoppingCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on wishlist page
        /// </summary>
        public bool ShowProductImagesOnWishList { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on wishlist page
        /// </summary>
        public bool ShowProductBundleImagesOnWishList { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show discount box on shopping cart page
        /// </summary>
        public bool ShowDiscountBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show gift card box on shopping cart page
        /// </summary>
        public bool ShowGiftCardBox { get; set; } = true;

        #region Checkout

        /// <summary>
        /// Gets or sets the checkout process.
        /// </summary>
        public string CheckoutProcess { get; set; } = Orders.CheckoutProcess.Standard;

        /// <summary>
        /// Gets or sets a value indicating whether quick checkout is enabled.
        /// With quick checkout, checkout steps are skipped if the required data is already known.
        /// </summary>
        public bool QuickCheckoutEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether customers can change their preferred shipping method.
        /// </summary>
        public bool CustomersCanChangePreferredShipping { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether customers can change their preferred payment method.
        /// </summary>
        public bool CustomersCanChangePreferredPayment { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a comment box on shopping cart page
        /// </summary>
        public bool ShowCommentBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a checkbox to subscribe to newsletters
        /// </summary>
        public CheckoutNewsletterSubscription NewsletterSubscription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show a legal hint in the order summary
        /// </summary>
        public bool ShowConfirmOrderLegalHint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show a revocation waiver checkbox box for ESD products
        /// </summary>
        public bool ShowEsdRevocationWaiverBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a checkbox to let the customer accept to hand over email address to third party
        /// </summary>
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }

        /// <summary>
        /// Gets or sets the label to accept to hand over the email to third party
        /// </summary>
        [LocalizedProperty]
        public string ThirdPartyEmailHandOverLabel { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets a number of "Cross-sells" on shopping cart page
        /// </summary>
        public int CrossSellsNumber { get; set; } = 8;

        /// <summary>
        /// Gets or sets a value indicating whether "email a wishlist" feature is enabled
        /// </summary>
        public bool EmailWishlistEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enabled "email a wishlist" for anonymous users.
        /// </summary>
        public bool AllowAnonymousUsersToEmailWishlist { get; set; }

        /// <summary>Gets or sets a value indicating whether mini-shopping cart is enabled
        /// </summary>
        public bool MiniShoppingCartEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product images in the mini-shopping cart.
        /// </summary>
        public bool ShowProductImagesInMiniShoppingCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show essential specification attributes in the mini-shopping cart.
        /// </summary>
        public bool ShowEssentialAttributesInMiniShoppingCart { get; set; }

        /// <summary>
        /// Specifies the presentation of delivery times in shopping cart.
        /// </summary>
        public DeliveryTimesPresentation DeliveryTimesInShoppingCart { get; set; } = DeliveryTimesPresentation.DateOnly;

        /// <summary>
        /// Gets or sets a value indicating whether to show the product short description in the order summary
        /// </summary>
        public bool ShowShortDesc { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the product weight in the order summary
        /// </summary>
        public bool ShowWeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the product short description in the order summary
        /// </summary>
        public bool ShowBasePrice { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the quantity of linked product attribute value
        /// </summary>
        public bool ShowLinkedAttributeValueQuantity { get; set; }

        /// <summary>
        /// Gets or sets the upper limit of possible order quantities up to which a drop-down menu for entering the order quantity is to be offered.
        /// If the number is greater, a numeric input field is used.
        /// </summary>
        public int MaxQuantityInputDropdownItems { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether to add products to the basket in single positions
        /// </summary>
        public bool AddProductsToBasketInSinglePositions { get; set; }
        // TODO: (mh) (core) A really fucked up name for something that I don't understand! What is a "single position"? Better name and explanation please.
    }
}