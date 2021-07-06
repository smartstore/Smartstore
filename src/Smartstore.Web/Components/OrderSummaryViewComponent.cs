using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Web.Models.ShoppingCart;

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
            bool prepareAndDisplayOrderReviewData = false,
            Customer customer = null,
            int? storeId = null)
        {
            if (model == null)
            {
                customer ??= Services.WorkContext.CurrentCustomer;
                storeId ??= Services.StoreContext.CurrentStore.Id;

                model = new ShoppingCartModel();

                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId.Value);

                await cart.Items.MapAsync(model,
                    isEditable: false,
                    prepareEstimateShippingIfEnabled: false,
                    prepareAndDisplayOrderReviewData: prepareAndDisplayOrderReviewData);
            }

            return View(model);
        }
    }
}
