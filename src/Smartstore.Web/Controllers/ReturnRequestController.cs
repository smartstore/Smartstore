using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public class ReturnRequestController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICurrencyService _currencyService;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly OrderHelper _orderHelper;
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MediaSettings _mediaSettings;

        public ReturnRequestController(
            SmartDbContext db,
            IOrderProcessingService orderProcessingService,
            ICurrencyService currencyService,
            ProductUrlHelper productUrlHelper,
            OrderHelper orderHelper,
            IMessageFactory messageFactory,
            OrderSettings orderSettings,
            LocalizationSettings localizationSettings,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings)
        {
            _db = db;
            _orderProcessingService = orderProcessingService;
            _currencyService = currencyService;
            _productUrlHelper = productUrlHelper;
            _orderHelper = orderHelper;
            _messageFactory = messageFactory;
            _orderSettings = orderSettings;
            _localizationSettings = localizationSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;
        }

        [DisallowRobot]
        public async Task<IActionResult> ReturnRequest(int id /* orderId */)
        {
            var order = await _db.Orders
                .IncludeOrderItems()
                .FindByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (Services.WorkContext.CurrentCustomer.Id != order.CustomerId)
            {
                return ChallengeOrForbid();
            }

            if (!_orderProcessingService.IsReturnRequestAllowed(order))
            {
                return RedirectToRoute("Homepage");
            }

            var model = new SubmitReturnRequestModel();
            await PrepareReturnRequestModel(model, order);

            return View(model);
        }

        [HttpPost, ActionName("ReturnRequest")]
        public async Task<IActionResult> ReturnRequestSubmit(int id /* orderId */, SubmitReturnRequestModel model)
        {
            var form = Request.Form;
            var customer = Services.WorkContext.CurrentCustomer;
            var order = await _db.Orders
                .IncludeOrderItems()
                .FindByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (customer.Id != order.CustomerId)
            {
                return ChallengeOrForbid();
            }

            if (!_orderProcessingService.IsReturnRequestAllowed(order))
            {
                return RedirectToRoute("Homepage");
            }

            var items = order.OrderItems
                .Select(oi => new ReturnRequestItem
                {
                    OrderItem = oi,
                    ReturnRequest = new ReturnRequest
                    {
                        StoreId = order.StoreId,
                        OrderItemId = oi.Id,
                        Quantity = form.TryGetValue($"quantity{oi.Id}", out var qtyValues) ? qtyValues.ToString().ToInt() : 0,
                        CustomerId = order.CustomerId,
                        ReasonForReturn = model.ReturnReason,
                        RequestedAction = model.ReturnAction,
                        CustomerComments = model.Comments,
                        StaffNotes = string.Empty,
                        ReturnRequestStatus = ReturnRequestStatus.Pending
                    }
                })
                .Where(x => x.ReturnRequest.Quantity > 0)
                .ToList();

            if (items.Count > 0)
            {
                _db.ReturnRequests.AddRange(items.Select(x => x.ReturnRequest));
                await _db.SaveChangesAsync();

                // Notify store owner here by sending an email.
                foreach (var item in items)
                {
                    await _messageFactory.SendNewReturnRequestStoreOwnerNotificationAsync(item.ReturnRequest, item.OrderItem, _localizationSettings.DefaultAdminLanguageId);
                }

                NotifySuccess(T("ReturnRequests.Submitted"));
                return RedirectToAction(nameof(CustomerController.Orders), "Customer");
            }

            ModelState.AddModelError(string.Empty, T("ReturnRequests.NoItemsSubmitted"));
            await PrepareReturnRequestModel(model, order);

            return View(model);
        }

        private async Task PrepareReturnRequestModel(SubmitReturnRequestModel model, Order order)
        {
            Guard.NotNull(order);
            Guard.NotNull(model);

            model.OrderId = order.Id;

            var customer = Services.WorkContext.CurrentCustomer;
            var customerCurrency = await _db.Currencies
                .AsNoTracking()
                .Where(x => x.CurrencyCode == order.CustomerCurrencyCode)
                .FirstOrDefaultAsync() ?? new() { CurrencyCode = order.CustomerCurrencyCode };

            var store = Services.StoreContext.GetCachedStores().GetStoreById(order.StoreId) ?? Services.StoreContext.CurrentStore;
            var catalogSettings = await Services.SettingFactory.LoadSettingsAsync<CatalogSettings>(store.Id);

            var orderItemIds = order.OrderItems.Select(x => x.Id).ToArray();
            var existingRequestsQuantities = customer.ReturnRequests
                .Where(x => orderItemIds.Contains(x.OrderItemId))
                .GroupBy(x => x.OrderItemId)
                .Select(rr => new
                {
                    OrderItemId = rr.Key,
                    Quantity = rr.Sum(x => x.Quantity)
                })
                .ToDictionarySafe(x => x.OrderItemId, x => x.Quantity);

            foreach (var oi in order.OrderItems)
            {
                var quantity = Math.Max(0, existingRequestsQuantities.TryGetValue(oi.Id, out var returnedQuantity)
                    ? oi.Quantity - returnedQuantity
                    : oi.Quantity);

                var oiModel = new SubmitReturnRequestModel.OrderItemModel
                {
                    Id = oi.Id,
                    ProductId = oi.Product.Id,
                    ProductName = oi.Product.GetLocalized(x => x.Name),
                    ProductSeName = await oi.Product.GetActiveSlugAsync(),
                    AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(oi.AttributeDescription)),
                    Quantity = quantity,
                    ReturnedQuantity = returnedQuantity
                };

                oiModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(oiModel.ProductSeName, oi);

                switch (order.CustomerTaxDisplayType)
                {
                    case TaxDisplayType.ExcludingTax:
                        oiModel.UnitPrice = _currencyService.ConvertToExchangeRate(oi.UnitPriceExclTax, order.CurrencyRate, customerCurrency, true);
                        break;

                    case TaxDisplayType.IncludingTax:
                        oiModel.UnitPrice = _currencyService.ConvertToExchangeRate(oi.UnitPriceInclTax, order.CurrencyRate, customerCurrency, true);
                        break;
                }

                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    oiModel.Image = await _orderHelper.PrepareOrderItemImageModelAsync(
                        oi.Product,
                        _mediaSettings.CartThumbPictureSize,
                        oiModel.ProductName,
                        oi.AttributeSelection,
                        catalogSettings);
                }

                model.Items.Add(oiModel);
            }

            string returnRequestReasons = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestReasons, order.CustomerLanguageId, order.StoreId, true, false);
            string returnRequestActions = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestActions, order.CustomerLanguageId, order.StoreId, true, false);

            ViewBag.AvailableReturnReasons = returnRequestReasons
                .SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x })
                .ToList();

            ViewBag.AvailableReturnActions = returnRequestActions
                .SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x })
                .ToList();
        }

        record ReturnRequestItem
        {
            public OrderItem OrderItem { get; init; }
            public ReturnRequest ReturnRequest { get; init; }
        }
    }
}
