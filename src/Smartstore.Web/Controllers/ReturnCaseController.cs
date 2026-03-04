using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public class ReturnCaseController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;
        private readonly LocalizationSettings _localizationSettings;

        public ReturnCaseController(
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
        public async Task<IActionResult> ReturnCase(int id /* orderId */)
        {
            var order = await _db.Orders
                .IncludeCustomer()
                .IncludeOrderItems()
                .Include(x => x.Customer.ReturnCases)
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

            var model = new ReturnCaseModel();
            await PrepareReturnCaseModel(model, order);

            return View(model);
        }

        [HttpPost, ActionName("ReturnCase")]
        public async Task<IActionResult> ReturnCaseSubmit(int id /* orderId */, ReturnCaseModel model)
        {
            var form = Request.Form;
            var order = await _db.Orders
                .IncludeCustomer()
                .IncludeOrderItems()
                .Include(x => x.Customer.ReturnCases)
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
                .Select(oi =>
                {
                    var existingQuantity = order.Customer.ReturnCases
                        .Where(x => x.OrderItemId == oi.Id)
                        .Sum(x => x.Quantity);

                    var quantity = 0;
                    if (model.Items.ReturnAllItems)
                    {
                        quantity = oi.Quantity;
                    }
                    else if (form.TryGetValue($"orderitem-select{oi.Id}", out var selectedVal) && selectedVal.ToString().ToBool())
                    {
                        quantity = form.TryGetValue($"orderitem-quantity{oi.Id}", out var qtyVal) ? qtyVal.ToString().ToInt() : 0;
                    }

                    quantity = Math.Max(quantity - existingQuantity, 0);
                    if (quantity == 0)
                    {
                        return null;
                    }

                    return new ReturnItem
                    {
                        OrderItem = oi,
                        ReturnCase = new ReturnCase
                        {
                            StoreId = order.StoreId,
                            OrderItemId = oi.Id,
                            Quantity = quantity,
                            CustomerId = order.CustomerId,
                            ReasonForReturn = model.ReturnReason,
                            RequestedAction = model.ReturnAction,
                            CustomerComments = model.Comments,
                            StaffNotes = string.Empty,
                            ReturnCaseStatus = ReturnCaseStatus.Pending
                        }
                    };
                })
                .Where(x => x != null)
                .ToList();

            if (items.Count > 0)
            {
                _db.ReturnCases.AddRange(items.Select(x => x.ReturnCase));
                await _db.SaveChangesAsync();

                foreach (var item in items)
                {
                    await _messageFactory.SendNewReturnCaseStoreOwnerNotificationAsync(item.ReturnCase, item.OrderItem, _localizationSettings.DefaultAdminLanguageId);
                }

                NotifySuccess(T("ReturnRequests.Submitted"));
                return RedirectToAction(nameof(CustomerController.Orders), "Customer");
            }

            ModelState.AddModelError(string.Empty, T("ReturnRequests.NoItemsSubmitted"));
            await PrepareReturnCaseModel(model, order);

            return View(model);
        }

        private async Task PrepareReturnCaseModel(ReturnCaseModel model, Order order)
        {
            Guard.NotNull(order);
            Guard.NotNull(model);

            model.OrderId = order.Id;
            model.Items = await order.MapAsync(true, model.Items?.ReturnAllItems ?? true);

            string returnReasons = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestReasons, order.CustomerLanguageId, order.StoreId, true, false);
            string returnActions = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestActions, order.CustomerLanguageId, order.StoreId, true, false);

            ViewBag.AvailableReturnReasons = returnReasons
                .SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x })
                .ToList();

            ViewBag.AvailableReturnActions = returnActions
                .SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x })
                .ToList();
        }

        record ReturnItem
        {
            public OrderItem OrderItem { get; init; }
            public ReturnCase ReturnCase { get; init; }
        }
    }
}
