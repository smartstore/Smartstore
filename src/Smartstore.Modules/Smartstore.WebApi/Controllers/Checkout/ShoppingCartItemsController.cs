#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Web.Api.Models.Catalog;
using Smartstore.Web.Api.Models.Checkout;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ShoppingCartItem entity and managing shopping carts.
    /// </summary>
    [ProducesResponseType(Status403Forbidden)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status422UnprocessableEntity)]
    public class ShoppingCartItemsController : WebApiController<ShoppingCartItem>
    {
        private readonly Lazy<IWorkContext> _workContext;
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IPermissionService> _permissionService;

        public ShoppingCartItemsController(
            Lazy<IWorkContext> workContext,
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<ICurrencyService> currencyService,
            Lazy<IPermissionService> permissionService)
        {
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _permissionService = permissionService;
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

        // TODO: (mg) allow to specify Variants, GiftCards and CheckoutAttributes for AddToCart somehow. See ProductVariantQuery.

        /// <summary>
        /// Adds a product to cart or wishlist.
        /// </summary>
        /// <remarks>
        /// Returns the cart or the wishlist items of the customer depending on the **shoppingCartType** value.
        /// </remarks>
        /// <param name="customerId" example="5678">Identifier of the customer who owns the cart.</param>
        /// <param name="productId" example="1234">Identifier of the product to add.</param>
        /// <param name="storeId" example="0">Identifier of the store the cart item belongs to. If 0, then the current store is used.</param>
        /// <param name="quantity" example="1">The quantity to add.</param>
        /// <param name="shoppingCartType" example="1">A value indicating whether to add the product to the shopping cart or wishlist.</param>
        /// <param name="customerEnteredPrice" example="0">An optional price entered by customer. Only applicable if the product supports it.</param>
        /// <param name="currencyCode">Currency code for **customerEnteredPrice**. If empty, then **customerEnteredPrice** must be in the primary currency of the store.</param>
        [HttpPost("ShoppingCartItems/AddToCartTest")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ShoppingCartItem>), Status200OK)]
        public async Task<IActionResult> AddToCartTest(
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] int productId,
            [FromODataBody] int storeId = 0,
            [FromODataBody] int quantity = 1,
            [FromODataBody] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] decimal customerEnteredPrice = decimal.Zero,
            [FromODataBody] string? currencyCode = null,
            [FromODataBody] AddToCartAttributes? attributes = null)
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
                    .FindByIdAsync(productId);
                if (product == null)
                {
                    return NotFound(productId, nameof(Product));
                }

                // Price entered by customer (optional).
                var customerPrice = new Money();
                if (product.CustomerEntersPrice && customerEnteredPrice > 0)
                {
                    if (currencyCode.HasValue())
                    {
                        var currency = await Db.Currencies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode);
                        if (currency == null)
                        {
                            return NotFound($"Cannot find currency with code {currencyCode}.");
                        }

                        customerPrice = _currencyService.Value.ConvertToPrimaryCurrency(new Money(customerEnteredPrice, currency));
                    }
                    else
                    {
                        customerPrice = new(customerEnteredPrice, _currencyService.Value.PrimaryCurrency);
                    }
                }

                var addToCartContext = new AddToCartContext
                {
                    Customer = customer,
                    Product = product,
                    StoreId = storeId > 0 ? storeId : null,
                    //VariantQuery = query,
                    CartType = shoppingCartType,
                    CustomerEnteredPrice = customerPrice,
                    Quantity = quantity,
                    AutomaticallyAddRequiredProducts = product.RequireOtherProducts && product.AutomaticallyAddRequiredProducts,
                    AutomaticallyAddBundleProducts = true
                };

                if (!await _shoppingCartService.Value.AddToCartAsync(addToCartContext))
                {
                    return ErrorResult(null, string.Join(". ", addToCartContext.Warnings));
                }

                return Ok(customer.ShoppingCartItems.Where(x => x.ShoppingCartType == shoppingCartType).AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }


        /// <summary>
        /// Adds a product to cart or wishlist.
        /// </summary>
        /// <remarks>
        /// Returns the cart or the wishlist items of the customer depending on the **shoppingCartType** value.
        /// </remarks>
        /// <param name="customerId" example="5678">Identifier of the customer who owns the cart.</param>
        /// <param name="productId" example="1234">Identifier of the product to add.</param>
        /// <param name="storeId" example="0">Identifier of the store the cart item belongs to. If 0, then the current store is used.</param>
        /// <param name="quantity" example="1">The quantity to add.</param>
        /// <param name="shoppingCartType" example="1">A value indicating whether to add the product to the shopping cart or wishlist.</param>
        /// <param name="customerEnteredPrice" example="0">An optional price entered by customer. Only applicable if the product supports it.</param>
        /// <param name="currencyCode">Currency code for **customerEnteredPrice**. If empty, then **customerEnteredPrice** must be in the primary currency of the store.</param>
        [HttpPost("ShoppingCartItems/AddToCart")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ShoppingCartItem>), Status200OK)]
        public async Task<IActionResult> AddToCart(
            //[FromQuery] ProductVariantQuery query,
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] int productId,
            [FromODataBody] int storeId = 0,
            [FromODataBody] int quantity = 1,
            [FromODataBody] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] decimal customerEnteredPrice = decimal.Zero,
            [FromODataBody] string? currencyCode = null)
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
                    .FindByIdAsync(productId);
                if (product == null)
                {
                    return NotFound(productId, nameof(Product));
                }

                // Price entered by customer (optional).
                var customerPrice = new Money();
                if (product.CustomerEntersPrice && customerEnteredPrice > 0)
                {
                    if (currencyCode.HasValue())
                    {
                        var currency = await Db.Currencies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode);
                        if (currency == null)
                        {
                            return NotFound($"Cannot find currency with code {currencyCode}.");
                        }

                        customerPrice = _currencyService.Value.ConvertToPrimaryCurrency(new Money(customerEnteredPrice, currency));
                    }
                    else
                    {
                        customerPrice = new(customerEnteredPrice, _currencyService.Value.PrimaryCurrency);
                    }
                }

                var addToCartContext = new AddToCartContext
                {
                    Customer = customer,
                    Product = product,
                    StoreId = storeId > 0 ? storeId : null,
                    //VariantQuery = query,
                    CartType = shoppingCartType,
                    CustomerEnteredPrice = customerPrice,
                    Quantity = quantity,
                    AutomaticallyAddRequiredProducts = product.RequireOtherProducts && product.AutomaticallyAddRequiredProducts,
                    AutomaticallyAddBundleProducts = true
                };

                if (!await _shoppingCartService.Value.AddToCartAsync(addToCartContext))
                {
                    return ErrorResult(null, string.Join(". ", addToCartContext.Warnings));
                }

                return Ok(customer.ShoppingCartItems.Where(x => x.ShoppingCartType == shoppingCartType).AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Updates a shopping cart item.
        /// </summary>
        /// <param name="quantity" example="1">The quantity to set.</param>
        [HttpPost("ShoppingCartItems({key})/UpdateItem")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ShoppingCartItem), Status200OK)]
        public async Task<IActionResult> UpdateItem(int key,
            [FromODataBody, Required] int quantity)
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

                var warnings = await _shoppingCartService.Value.UpdateCartItemAsync(entity.Customer, key, quantity, false);
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

        private async Task<string?> CheckAccess(ShoppingCartType cartType)
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
