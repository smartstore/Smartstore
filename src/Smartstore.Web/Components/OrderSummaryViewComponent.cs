using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component for rendering order totals.
    /// </summary>
    public class OrderSummaryViewComponent : SmartViewComponent
    {
        private readonly IShoppingCartService _shoppingCartService;

        public OrderSummaryViewComponent(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            ShoppingCartModel model = null,
            Customer customer = null,
            int? storeId = null)
        {
            if (model == null)
            {
                customer ??= Services.WorkContext.CurrentCustomer;
                storeId ??= Services.StoreContext.CurrentStore.Id;

                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId.Value);

                model = await cart.MapAsync(isEditable: false, prepareEstimateShippingIfEnabled: false);
            }

            return View(model);
        }
    }
}
