using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart validator
    /// </summary>
    public interface IShoppingCartValidator
    {
        // TODO: (ms) (core) Finish implementation of interface. Add dev documentation

        ///// <summary>
        ///// Validates required products (products which require other variant to be added to the cart)
        ///// </summary>
        ///// <param name="customer">Customer</param>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="product">Product</param>
        ///// <param name="storeId">Store identifier</param>
        ///// <param name="automaticallyAddRequiredProductsIfEnabled">Automatically add required products if enabled</param>
        ///// <returns>Warnings</returns>
        Task<List<string>> GetRequiredProductWarningsAsync(Customer customer, ShoppingCartType shoppingCartType, Product product, int storeId, bool addRequiredProducts);

        ///// <summary>
        ///// Validates a product for standard properties
        ///// </summary>
        ///// <param name="customer">Customer</param>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="product">Product</param>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <param name="customerEnteredPrice">Customer entered price</param>
        ///// <param name="quantity">Quantity</param>
        ///// <returns>Warnings</returns>
        Task<IList<string>> GetStandardWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            int? storeId = null);

        ///// <summary>
        ///// Validates shopping cart item attributes
        ///// </summary>
        ///// <param name="customer">The customer</param>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="product">Product</param>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <param name="combination">The product variant attribute combination instance (reduces database roundtrips)</param>
        ///// <param name="quantity">Quantity</param>
        ///// <param name="bundleItem">Product bundle item</param>
        ///// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartItemAttributeWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection,
            int quantity = 1,
            ProductBundleItem bundleItem = null,
            ProductVariantAttributeCombination combination = null);

        ///// <summary>
        ///// Validates shopping cart item (gift card)
        ///// </summary>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="product">Product</param>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <returns>Warnings</returns>
        IList<string> GetShoppingCartItemGiftCardWarnings(Product product, ShoppingCartType shoppingCartType, ProductVariantAttributeSelection selection);

        ///// <summary>
        ///// Validates bundle items
        ///// </summary>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="bundleItem">Product bundle item</param>
        ///// <returns>Warnings</returns>
        IList<string> GetBundleItemWarnings(ProductBundleItem bundleItem);
        IList<string> GetCartBundleItemWarnings(IList<OrganizedShoppingCartItem> cartItems);

        /// <summary>
        /// Validates shopping cart item
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="selectedAttributes">Selected attributes</param>
        /// <param name="customerEnteredPrice">Customer entered price</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="automaticallyAddRequiredProductsIfEnabled">Automatically add required products if enabled</param>
        /// <param name="getStandardWarnings">A value indicating whether we should validate a product for standard properties</param>
        /// <param name="getAttributesWarnings">A value indicating whether we should validate product attributes</param>
        /// <param name="getGiftCardWarnings">A value indicating whether we should validate gift card properties</param>
        /// <param name="getRequiredProductWarnings">A value indicating whether we should validate required products (products which require other products to be added to the cart)</param>
        /// <param name="getBundleWarnings">A value indicating whether we should validate bundle and bundle items</param>
        /// <param name="bundleItem">Product bundle item if bundles should be validated</param>
        /// <param name="childItems">Child cart items to validate bundle items</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartItemWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            int storeId,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            bool addRequiredProducts,
            bool getStandardWarnings = true,
            bool getAttributesWarnings = true,
            bool getGiftCardWarnings = true,
            bool getRequiredProductWarnings = true,
            bool getBundleWarnings = true,
            ProductBundleItem bundleItem = null,
            IList<OrganizedShoppingCartItem> childItems = null);

        ///// <summary>
        ///// Validates whether this shopping cart is valid
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <param name="checkoutAttributes">Checkout attributes</param>
        ///// <param name="validateCheckoutAttributes">A value indicating whether to validate checkout attributes</param>
        ///// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartWarningsAsync(IList<OrganizedShoppingCartItem> shoppingCart, CheckoutAttributeSelection selection, bool validateCheckoutAttributes);

    }
}