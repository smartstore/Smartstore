using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Web.Api.Models.Checkout;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ShoppingCartItem entity and managing shopping carts.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    [ProducesResponseType(Status403Forbidden)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status422UnprocessableEntity)]
    public class ShoppingCartItemsController : WebApiController<ShoppingCartItem>
    {
        private readonly Lazy<IStoreContext> _storeContext;
        private readonly Lazy<IWorkContext> _workContext;
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IPermissionService> _permissionService;
        private readonly Lazy<IProductAttributeMaterializer> _productAttributeMaterializer;
        private readonly Lazy<ICheckoutAttributeMaterializer> _checkoutAttributeMaterializer;

        public ShoppingCartItemsController(
            Lazy<IStoreContext> storeContext,
            Lazy<IWorkContext> workContext,
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<ICurrencyService> currencyService,
            Lazy<IPermissionService> permissionService,
            Lazy<IProductAttributeMaterializer> productAttributeMaterializer,
            Lazy<ICheckoutAttributeMaterializer> checkoutAttributeMaterializer)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _permissionService = permissionService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
        }

        [HttpGet("ShoppingCartItems"), ApiQueryable]
        [Permission(Permissions.Cart.Read)]
        public IQueryable<ShoppingCartItem> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ShoppingCartItems({key})"), ApiQueryable]
        [Permission(Permissions.Cart.Read)]
        public SingleResult<ShoppingCartItem> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ShoppingCartItems({key})/Product"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpGet("ShoppingCartItems({key})/Customer"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> GetCustomer(int key)
        {
            return GetRelatedEntity(key, x => x.Customer);
        }

        [HttpGet("ShoppingCartItems({key})/BundleItem"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductBundleItem> GetBundleItem(int key)
        {
            return GetRelatedEntity(key, x => x.BundleItem);
        }

        [HttpPost, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Post()
        {
            return Forbidden($"Use endpoint \"{nameof(AddToCart)}\" instead.");
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return Forbidden($"Use endpoint \"{nameof(UpdateItem)}\" instead.");
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return Forbidden($"Use endpoint \"{nameof(UpdateItem)}\" instead.");
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return Forbidden($"Use endpoint \"{nameof(DeleteItem)}\" instead.");
        }

        #region Actions and functions

        /// <summary>
        /// Adds a product to cart or wishlist.
        /// </summary>
        /// <remarks>
        /// Returns the cart or the wishlist items of the customer depending on the value of the **shoppingCartType** parameter.
        /// </remarks>
        /// <param name="customerId" example="5678">Identifier of the customer who owns the cart.</param>
        /// <param name="productId" example="1234">Identifier of the product to add.</param>
        /// <param name="quantity" example="1">The quantity to add.</param>
        /// <param name="shoppingCartType" example="1">**1** to add item to the shopping cart. **2** to add to wishlist.</param>
        /// <param name="storeId" example="0">Identifier of the store the cart item belongs to. If **0**, then the current store is used.</param>
        /// <param name="extraData">Optional extra data to apply, e.g. product attributes.</param>
        [HttpPost("ShoppingCartItems/AddToCart")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ShoppingCartItem>), Status200OK)]
        public async Task<IActionResult> AddToCart(
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] int productId,
            [FromODataBody, Required] int quantity = 1,
            [FromODataBody] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] int storeId = 0,
            [FromODataBody] AddToCartExtraData extraData = null)
        {
            try
            {
                var message = await CheckAccess(shoppingCartType);
                if (message.HasValue())
                {
                    return Forbidden(message);
                }

                var customer = await Db.Customers
                    .AsSplitQuery()
                    .IncludeCustomerRoles()
                    .Include(x => x.ShoppingCartItems)
                    .FindByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound(customerId, nameof(Customer));
                }

                var product = await Db.Products
                    .Include(x => x.ProductVariantAttributes)
                    .ThenInclude(x => x.ProductAttribute)
                    .FindByIdAsync(productId);
                if (product == null)
                {
                    return NotFound(productId, nameof(Product));
                }

                var context = new AddToCartContext
                {
                    Customer = customer,
                    Product = product,
                    StoreId = storeId > 0 ? storeId : _storeContext.Value.CurrentStore.Id,
                    CartType = shoppingCartType,
                    Quantity = quantity,
                    AutomaticallyAddRequiredProducts = product.RequireOtherProducts && product.AutomaticallyAddRequiredProducts,
                    AutomaticallyAddBundleProducts = true
                };

                if (extraData != null)
                {
                    var result = await ApplyExtraData(extraData, product, context);
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (!await _shoppingCartService.Value.AddToCartAsync(context))
                {
                    return ErrorResult(null, string.Join(" ", context.Warnings.Select(x => x.EnsureEndsWith('.'))));
                }

                if (extraData != null)
                {
                    await SaveCheckoutAttributes(extraData, customer, context);
                }

                return Ok(customer.ShoppingCartItems.Where(x => x.ShoppingCartType == shoppingCartType).AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        private async Task<IActionResult> ApplyExtraData(AddToCartExtraData extraData, Product product, AddToCartContext context)
        {
            var selection = new ProductVariantAttributeSelection(null);

            // Variant attributes.
            if (!extraData.Attributes.IsNullOrEmpty())
            {
                var query = new ProductVariantQuery();
                extraData.Attributes.Each(query.AddVariant);

                (selection, _) = await _productAttributeMaterializer.Value.CreateAttributeSelectionAsync(
                    query,
                    product.ProductVariantAttributes,
                    product.Id,
                    context.BundleItemId,
                    false);
            }
            else if (!extraData.SearchAttributes.IsNullOrEmpty())
            {
                var existingMappings = product.ProductVariantAttributes
                    .ToDictionarySafe(x => x.ProductAttribute.Name, x => x, StringComparer.OrdinalIgnoreCase);

                foreach (var attribute in extraData.SearchAttributes)
                {
                    if (existingMappings.TryGetValue(attribute.Name, out var pva))
                    {
                        var pvav = pva.ProductVariantAttributeValues.FirstOrDefault(x => x.Name.EqualsNoCase(attribute.Value));
                        if (pvav != null)
                        {
                            selection.AddAttributeValue(pva.Id, pvav.Id);
                        }
                    }
                }
            }

            // Gift card.
            if (extraData.GiftCard != null && product.IsGiftCard)
            {
                selection.AddGiftCardInfo(extraData.GiftCard);
            }

            context.RawAttributes = selection.AsJson();

            // Price entered by customer.
            var enteredPrice = extraData.CustomerEnteredPrice;
            if (product.CustomerEntersPrice && enteredPrice != null && enteredPrice.Price > 0)
            {
                if (enteredPrice.CurrencyCode.HasValue())
                {
                    var currency = await Db.Currencies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.CurrencyCode == enteredPrice.CurrencyCode);
                    if (currency == null)
                    {
                        return NotFound($"Cannot find currency with code {enteredPrice.CurrencyCode}.");
                    }

                    context.CustomerEnteredPrice = _currencyService.Value.ConvertToPrimaryCurrency(new Money(enteredPrice.Price, currency));
                }
                else
                {
                    context.CustomerEnteredPrice = new(enteredPrice.Price, _currencyService.Value.PrimaryCurrency);
                }
            }

            return null;
        }

        private async Task SaveCheckoutAttributes(AddToCartExtraData extraData, Customer customer, AddToCartContext context)
        {
            // Checkout attributes.
            if (!extraData.CheckoutAttributes.IsNullOrEmpty())
            {
                var query = new ProductVariantQuery();
                extraData.CheckoutAttributes.Each(query.AddCheckoutAttribute);

                var cart = await _shoppingCartService.Value.GetCartAsync(customer, context.CartType, context.StoreId.Value);
                cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.Value.CreateCheckoutAttributeSelectionAsync(query, cart);
                await Db.SaveChangesAsync();
            }
            else if (!extraData.SearchCheckoutAttributes.IsNullOrEmpty())
            {
                var selection = new CheckoutAttributeSelection(null);
                var existingCheckoutAttributes = (await Db.CheckoutAttributes
                    .Include(x => x.CheckoutAttributeValues)
                    .AsNoTracking()
                    .ApplyStandardFilter(false, context.StoreId.Value)
                    .ToListAsync())
                    .ToDictionarySafe(x => x.Name, x => x);

                foreach (var attribute in extraData.SearchCheckoutAttributes)
                {
                    if (existingCheckoutAttributes.TryGetValue(attribute.Name, out var ca))
                    {
                        var cav = ca.CheckoutAttributeValues.FirstOrDefault(x => x.Name.EqualsNoCase(attribute.Value));
                        if (cav != null)
                        {
                            selection.AddAttributeValue(ca.Id, cav.Id);
                        }
                    }
                }

                customer.GenericAttributes.CheckoutAttributes = selection;
                await Db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Updates a shopping cart item.
        /// </summary>
        /// <param name="quantity" example="1">The quantity to set.</param>
        /// <param name="enabled" example="true">A value indicating whether to enable or disable the cart item.</param>
        [HttpPost("ShoppingCartItems({key})/UpdateItem")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ShoppingCartItem), Status200OK)]
        public async Task<IActionResult> UpdateItem(int key,
            [FromODataBody] int? quantity = null,
            [FromODataBody] bool? enabled = null)
        {
            try
            {
                var entity = await Entities
                    .AsSplitQuery()
                    .Include(x => x.Customer)
                    .ThenInclude(x => x.ShoppingCartItems)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductVariantAttributes)
                    .FindByIdAsync(key);
                if (entity == null)
                {
                    return NotFound(key);
                }

                var message = await CheckAccess(entity.ShoppingCartType);
                if (message.HasValue())
                {
                    return Forbidden(message);
                }

                var warnings = await _shoppingCartService.Value.UpdateCartItemAsync(entity.Customer, key, quantity, enabled);
                if (warnings.Count > 0)
                {
                    return ErrorResult(null, string.Join(". ", warnings));
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Deletes a shopping cart item.
        /// </summary>
        /// <param name="resetCheckoutData" example="false">
        /// A value indicating whether to reset checkout data of the customer to whom the cart item belongs.
        /// For example selected payment and shipping method.
        /// </param>
        /// <param name="removeInvalidCheckoutAttributes" example="false">
        /// A value indicating whether to remove checkout attributes that require shipping, if the cart does not require shipping at all.
        /// </param>
        [HttpPost("ShoppingCartItems({key})/DeleteItem")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(Status204NoContent)]
        public async Task<IActionResult> DeleteItem(int key,
            [FromODataBody] bool resetCheckoutData = false,
            [FromODataBody] bool removeInvalidCheckoutAttributes = false)
        {
            try
            {
                var entity = await Entities
                    .Include(x => x.Customer)
                    .FindByIdAsync(key);
                if (entity == null)
                {
                    return NotFound(key);
                }

                var message = await CheckAccess(entity.ShoppingCartType);
                if (message.HasValue())
                {
                    return Forbidden(message);
                }

                await _shoppingCartService.Value.DeleteCartItemAsync(entity, resetCheckoutData, removeInvalidCheckoutAttributes);

                return NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Deletes the shopping cart of a Customer. Returns the number of deleted shopping cart items.
        /// </summary>
        /// <param name="customerId" example="5678">Identifier of the customer who owns the cart.</param>
        /// <param name="shoppingCartType" example="1">A value indicating whether to delete the shopping cart or the wishlist.</param>
        /// <param name="storeId" example="0">Identifier to filter cart items by store. 0 to delete all items.</param>
        /// <response code="200">Number of deleted shopping cart items.</response>
        [HttpPost("ShoppingCartItems/DeleteCart")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(int), Status200OK)]
        public async Task<IActionResult> DeleteCart(
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] int storeId = 0)
        {
            try
            {
                var message = await CheckAccess(shoppingCartType);
                if (message.HasValue())
                {
                    return Forbidden(message);
                }

                var customer = await Db.Customers
                    .AsSplitQuery()
                    .Include(x => x.ShoppingCartItems)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductVariantAttributes)
                    .FindByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound(customerId, nameof(Customer));
                }

                var cart = await _shoppingCartService.Value.GetCartAsync(customer, shoppingCartType, storeId);
                var count = await _shoppingCartService.Value.DeleteCartAsync(cart);

                return Ok(count);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        private async Task<string> CheckAccess(ShoppingCartType cartType)
        {
            var permission = cartType == ShoppingCartType.Wishlist ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart;
            if (!await _permissionService.Value.AuthorizeAsync(permission, _workContext.Value.CurrentCustomer))
            {
                return await _permissionService.Value.GetUnauthorizedMessageAsync(permission);
            }

            return null;
        }

        #endregion
    }
}
