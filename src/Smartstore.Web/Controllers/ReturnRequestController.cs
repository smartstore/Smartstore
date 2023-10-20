using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
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
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;
        private readonly LocalizationSettings _localizationSettings;

        public ReturnRequestController(
            SmartDbContext db,
            IOrderProcessingService orderProcessingService,
            ICurrencyService currencyService,
            ProductUrlHelper productUrlHelper,
            IMessageFactory messageFactory,
            OrderSettings orderSettings,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _orderProcessingService = orderProcessingService;
            _currencyService = currencyService;
            _productUrlHelper = productUrlHelper;
            _messageFactory = messageFactory;
            _orderSettings = orderSettings;
            _localizationSettings = localizationSettings;
        }

        [DisallowRobot]
        public async Task<IActionResult> ReturnRequest(int id /* orderId */)
        {
            var order = await _db.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
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
            await PrepareReturnRequestModelAsync(model, order);

            return View(model);
        }

        [HttpPost, ActionName("ReturnRequest")]
        public async Task<IActionResult> ReturnRequestSubmit(int id /* orderId */, SubmitReturnRequestModel model)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            var customer = Services.WorkContext.CurrentCustomer;

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

            foreach (var orderItem in order.OrderItems)
            {
                var form = Request.Form;

                var quantity = 0;
                foreach (var formKey in form.Keys)
                {
                    if (formKey.EqualsNoCase($"quantity{orderItem.Id}"))
                    {
                        _ = int.TryParse(form[formKey], out quantity);
                        break;
                    }
                }

                if (quantity > 0)
                {
                    var rr = new ReturnRequest
                    {
                        StoreId = Services.StoreContext.CurrentStore.Id,
                        OrderItemId = orderItem.Id,
                        Quantity = quantity,
                        CustomerId = customer.Id,
                        ReasonForReturn = model.ReturnReason,
                        RequestedAction = model.ReturnAction,
                        CustomerComments = model.Comments,
                        StaffNotes = string.Empty,
                        ReturnRequestStatus = ReturnRequestStatus.Pending
                    };

                    customer.ReturnRequests.Add(rr);

                    _db.TryUpdate(customer);
                    await _db.SaveChangesAsync();

                    model.AddedReturnRequestIds.Add(rr.Id);

                    // Notify store owner here by sending an email.
                    await _messageFactory.SendNewReturnRequestStoreOwnerNotificationAsync(rr, orderItem, _localizationSettings.DefaultAdminLanguageId);
                }
            }

            await PrepareReturnRequestModelAsync(model, order);

            if (model.AddedReturnRequestIds.Any())
            {
                model.Result = T("ReturnRequests.Submitted");
            }
            else
            {
                NotifyWarning(T("ReturnRequests.NoItemsSubmitted"));
            }

            return View(model);
        }

        protected async Task PrepareReturnRequestModelAsync(SubmitReturnRequestModel model, Order order)
        {
            Guard.NotNull(order);
            Guard.NotNull(model);

            model.OrderId = order.Id;

            var customerCurrency = await _db.Currencies
                .AsNoTracking()
                .Where(x => x.CurrencyCode == order.CustomerCurrencyCode)
                .FirstOrDefaultAsync() ?? new() { CurrencyCode = order.CustomerCurrencyCode };

            foreach (var orderItem in order.OrderItems)
            {
                var orderItemModel = new SubmitReturnRequestModel.OrderItemModel
                {
                    Id = orderItem.Id,
                    ProductId = orderItem.Product.Id,
                    ProductName = orderItem.Product.GetLocalized(x => x.Name),
                    ProductSeName = await orderItem.Product.GetActiveSlugAsync(),
                    AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(orderItem.AttributeDescription)),
                    Quantity = orderItem.Quantity
                };

                orderItemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(orderItemModel.ProductSeName, orderItem);

                switch (order.CustomerTaxDisplayType)
                {
                    case TaxDisplayType.ExcludingTax:
                        orderItemModel.UnitPrice = _currencyService.ConvertToExchangeRate(orderItem.UnitPriceExclTax, order.CurrencyRate, customerCurrency, true);
                        break;

                    case TaxDisplayType.IncludingTax:
                        orderItemModel.UnitPrice = _currencyService.ConvertToExchangeRate(orderItem.UnitPriceInclTax, order.CurrencyRate, customerCurrency, true);
                        break;
                }

                model.Items.Add(orderItemModel);
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
    }
}
