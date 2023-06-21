using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Web.Api.Models.Catalog;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ShoppingCartItem entity.
    /// </summary>
    public class ShoppingCartItemsController : WebApiController<ShoppingCartItem>
    {
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<ICurrencyService> _currencyService;

        public ShoppingCartItemsController(
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<ICurrencyService> currencyService)
        {
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
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
            return Forbidden();
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return Forbidden();
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return Forbidden($"Use endpoint \"{nameof(DeleteItem)}\" instead.");
        }

        #region Actions and functions

        // TODO: (mg) allow to specify variants for AddToCart somehow. ProductVariantQuery is unsuitable and too difficult for clients here. RawAttributes?

        /// <summary>
        /// Adds a product to cart or wishlist.
        /// </summary>
        /// <param name="customerId" example="1234">Identifier of the customer who owns the cart.</param>
        /// <param name="storeId" example="0">Identifier of the store the cart item belongs to. If empty, the current store is used.</param>
        /// <param name="quantity" example="1">The quantity to add.</param>
        /// <param name="shoppingCartType" example="1">A value indicating whether to add the product to the shopping cart or wishlist.</param>
        /// <param name="customerEnteredPrice" example="false">An optional price entered by customer. Only applicable if the product supports it.</param>
        /// <param name="currencyCode">Currency code for **customerEnteredPrice**. If empty, then **customerEnteredPrice** must be in the primary currency of the store.</param>
        [HttpPost("ShoppingCartItems/AddToCart")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> AddToCart(
            //[FromQuery] ProductVariantQuery query,
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] int productId,
            [FromODataBody] int storeId = 0,
            [FromODataBody] int quantity = 1,
            [FromODataBody] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] decimal customerEnteredPrice = decimal.Zero,
            [FromODataBody] string currencyCode = null)
        {
            try
            {
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

                if (await _shoppingCartService.Value.AddToCartAsync(addToCartContext))
                {
                    return Ok();
                }

                return ErrorResult(null, string.Join(". ", addToCartContext.Warnings));
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
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
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
        /// <param name="customerId" example="1234">Identifier of the customer who owns the cart.</param>
        /// <param name="shoppingCartType" example="1">A value indicating whether to delete the shopping cart or the wishlist.</param>
        /// <param name="storeId" example="0">Identifier to filter cart items by store. 0 to delete all items.</param>
        /// <response code="200">Number of deleted shopping cart items.</response>
        [HttpPost("ShoppingCartItems/DeleteCart")]
        [Permission(Permissions.Cart.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(int), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> DeleteCart(
            [FromODataBody, Required] int customerId,
            [FromODataBody, Required] ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart,
            [FromODataBody] int storeId = 0)
        {
            try
            {
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

        #endregion
    }
}
