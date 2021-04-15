using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Web.Models.ShoppingCart;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IViewComponentResult> InvokeAsync(bool prepareAndDisplayOrderReviewData = false, Customer customer = null, int? storeId = null)
        {
            customer ??= Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId ?? Services.StoreContext.CurrentStore.Id);
            var model = new ShoppingCartModel();

            await cart.AsEnumerable().MapAsync(model,
                isEditable: false,
                prepareEstimateShippingIfEnabled: false,
                prepareAndDisplayOrderReviewData: prepareAndDisplayOrderReviewData);

            return View(model);
        }
    }
}
