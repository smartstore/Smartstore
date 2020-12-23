using Smartstore.Core.Common;
using Smartstore.Core.Configuration;

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
        public int MaximumCartItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating maximum number of items in the wishlist
        /// </summary>
        public int MaximumWishlistItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to show product images in the mini-shopping cart block
        /// </summary>
        public bool AllowOutOfStockItemsWishlist { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to move items from wishlist to cart when clicking "Add to cart" button. Otherwise, they are copied
        /// </summary>
        public bool MoveItemsFromWishlistToCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the items from wishlist to ahopping cart button
        /// </summary>
        public bool DisplayFromWishlistToCartButton { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on shopping cart page
        /// </summary>
        public bool DisplayImagesOnCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product bundle images on shopping cart page
        /// </summary>
        public bool DisplayBundleImagesOnCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on wishlist page
        /// </summary>
        public bool DisplayImagesOnWishlist { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on wishlist page
        /// </summary>
        public bool DisplayBundleImagesOnWishlist { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show discount box on shopping cart page
        /// </summary>
        public bool DisplayDiscountBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show gift card box on shopping cart page
        /// </summary>
        public bool DisplayGiftCardBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a comment box on shopping cart page
        /// </summary>
        public bool DisplayCommentBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a revocation waiver checkbox box for ESD products
        /// </summary>
        public bool DisplayEsdWaiverBox { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a checkbox to subscribe to newsletters
        /// </summary>
        public CheckoutNewsLetterSubscription NewsLetterSubscription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show a checkbox to let the customer accept to hand over email address to third party
        /// </summary>
        public CheckoutThirdPartyEmailHandOver ThirdPartyHandOver { get; set; }

        /// <summary>
        /// Gets or sets the label to accept to hand over the email to third party
        /// </summary>
        public string ThirdPartyLabel { get; set; }

        /// <summary>
        /// Gets or sets a number of "Cross-sells" on shopping cart page
        /// </summary>
        public int CrossSells { get; set; } = 8;

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
        public bool MiniCartEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show product images in the mini-shopping cart block
        /// </summary>
        public bool DisplayImagesInMiniCart { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show a legal hint in the order summary
        /// </summary>
        public bool DisplayOrderLegalHint { get; set; }

        /// <summary>
        /// Specifies the presentation of delivery times in shopping cart.
        /// </summary>
        public DeliveryTimesPresentation DeliveryTimes { get; set; } = DeliveryTimesPresentation.DateOnly;

        /// <summary>
        /// Gets or sets a value indicating whether to show the product short description in the order summary
        /// </summary>
        public bool DisplayShortDesc { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the product weight in the order summary
        /// </summary>
        public bool DisplayWeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the product short description in the order summary
        /// </summary>
        public bool DisplayBasePrice { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the quantity of linked product attribute value
        /// </summary>
        public bool DisplayLinkedAttributeQuantity { get; set; }
    }
}