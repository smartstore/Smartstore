using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public class ReturnRequestController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;
        private readonly LocalizationSettings _localizationSettings;

        public ReturnRequestController(
            SmartDbContext db,
            IOrderProcessingService orderProcessingService,
            IMessageFactory messageFactory,
            OrderSettings orderSettings,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _orderProcessingService = orderProcessingService;
            _messageFactory = messageFactory;
            _orderSettings = orderSettings;
            _localizationSettings = localizationSettings;
        }

        [DisallowRobot]
        public async Task<IActionResult> ReturnRequest(int id /* orderId */)
        {
            var order = await _db.Orders
                .IncludeCustomer()
                .IncludeOrderItems()
                .Include(x => x.Customer.ReturnRequests)
                .FindByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.CustomerId != Services.WorkContext.CurrentCustomer.Id)
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
            var order = await _db.Orders
                .IncludeCustomer()
                .IncludeOrderItems()
                .Include(x => x.Customer.ReturnRequests)
                .FindByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.CustomerId != Services.WorkContext.CurrentCustomer.Id)
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
                        Quantity = form.TryGetValue($"orderitem-quantity{oi.Id}", out var qtyValues) ? qtyValues.ToString().ToInt() : 0,
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
            model.Items = await order.MapAsync(_db);

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
