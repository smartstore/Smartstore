using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Orders;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class GiftCardController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IGiftCardService _giftCardService;
        private readonly ILanguageService _languageService;
        private readonly IMessageFactory _messageFactory;
        private readonly LocalizationSettings _localizationSettings;
        private readonly Currency _primaryCurrency;

        public GiftCardController(
            SmartDbContext db,
            IGiftCardService giftCardService,
            ILanguageService languageService,
            ICurrencyService currencyService,
            IMessageFactory messageFactory,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _giftCardService = giftCardService;
            _languageService = languageService;
            _messageFactory = messageFactory;
            _localizationSettings = localizationSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Order.GiftCard.Read)]
        public IActionResult List()
        {
            ViewBag.ActivatedList = new List<SelectListItem>
            {
                new SelectListItem { Value = "true", Text = T("Common.Activated") },
                new SelectListItem { Value = "false", Text = T("Common.Deactivated") }
            };

            return View(new GiftCardListModel());
        }

        [Permission(Permissions.Order.GiftCard.Read)]
        public async Task<IActionResult> GiftCardList(GridCommand command, GiftCardListModel model)
        {
            var mapper = MapperFactory.GetMapper<GiftCard, GiftCardModel>();
            var query = _db.GiftCards
                .Include(x => x.GiftCardUsageHistory)
                .AsNoTracking();

            if (model.CouponCode.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.GiftCardCouponCode, model.CouponCode);
            }

            if (model.Activated.HasValue)
            {
                query = query.Where(x => x.IsGiftCardActivated == model.Activated);
            }

            var giftCards = await query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await giftCards
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    await PrepareGiftCardModel(model, x);
                    return model;
                })
                .AsyncToList();

            return Json(new GridModel<GiftCardModel>
            {
                Rows = rows,
                Total = giftCards.TotalCount
            });
        }

        [Permission(Permissions.Order.GiftCard.Read)]
        public async Task<IActionResult> GiftCardUsageHistoryList(GridCommand command, int id /* giftCardId */)
        {
            var historyEntries = await _db.GiftCardUsageHistory
                .AsNoTracking()
                .Where(x => x.GiftCardId == id)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = historyEntries
                .Select(x => new GiftCardUsageHistoryModel
                {
                    Id = x.Id,
                    OrderId = x.UsedWithOrderId,
                    UsedValue = Services.CurrencyService.PrimaryCurrency.AsMoney(x.UsedValue).ToString(true),
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    OrderEditUrl = Url.Action("Edit", "Order", new { id = x.UsedWithOrderId, area = "Admin" }),
                    OrderEditLinkText = T("Admin.Common.ViewObject", x.UsedWithOrderId)
                })
                .ToList();

            return Json(new GridModel<GiftCardUsageHistoryModel>
            {
                Rows = rows,
                Total = rows.Count
            });
        }

        [Permission(Permissions.Order.GiftCard.Create)]
        public IActionResult Create()
        {
            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(new GiftCardModel());
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Promotion.Discount.Create)]
        public async Task<IActionResult> Create(GiftCardModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<GiftCardModel, GiftCard>();
                var giftCard = await mapper.MapAsync(model);
                giftCard.CreatedOnUtc = DateTime.UtcNow;

                _db.GiftCards.Add(giftCard);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewGiftCard, T("ActivityLog.AddNewGiftCard"), giftCard.GiftCardCouponCode);
                NotifySuccess(T("Admin.GiftCards.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = giftCard.Id })
                    : RedirectToAction(nameof(List));
            }

            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(model);
        }

        [Permission(Permissions.Order.GiftCard.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var giftCard = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .FindByIdAsync(id);

            if (giftCard == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<GiftCard, GiftCardModel>();
            var model = await mapper.MapAsync(giftCard);

            await PrepareGiftCardModel(model, giftCard);

            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.GiftCard.Update)]
        public async Task<IActionResult> Edit(GiftCardModel model, bool continueEditing)
        {
            var giftCard = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .FindByIdAsync(model.Id);

            if (giftCard == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<GiftCardModel, GiftCard>();
                await mapper.MapAsync(model, giftCard);

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditGiftCard, T("ActivityLog.EditGiftCard"), giftCard.GiftCardCouponCode);
                NotifySuccess(T("Admin.GiftCards.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), giftCard.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareGiftCardModel(model, giftCard);

            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IActionResult GenerateCouponCode()
        {
            return Json(new { CouponCode = _giftCardService.GenerateGiftCardCode() });
        }

        [HttpPost]
        [Permission(Permissions.Order.GiftCard.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var giftCard = await _db.GiftCards.FindByIdAsync(id);
            if (giftCard == null)
            {
                return NotFound();
            }

            _db.GiftCards.Remove(giftCard);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteGiftCard, T("ActivityLog.DeleteGiftCard"), giftCard.GiftCardCouponCode);
            NotifySuccess(T("Admin.GiftCards.Deleted"));

            return RedirectToAction(nameof(List));
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("notifyRecipient")]
        [Permission(Permissions.Order.GiftCard.Notify)]
        public async Task<IActionResult> NotifyRecipient(GiftCardModel model)
        {
            var giftCard = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .FindByIdAsync(model.Id);

            if (giftCard == null)
            {
                return NotFound();
            }

            try
            {
                if (giftCard.RecipientEmail.IsEmail())
                {
                    if (giftCard.SenderEmail.IsEmail())
                    {
                        var languageId = (await _db.Languages.FindByIdAsync(giftCard.PurchasedWithOrderItem?.Order?.CustomerLanguageId ?? 0, false))?.Id ?? 0;

                        if (languageId == 0)
                        {
                            languageId = await _languageService.GetMasterLanguageIdAsync();
                        }
                        if (languageId == 0)
                        {
                            languageId = _localizationSettings.DefaultAdminLanguageId;
                        }

                        var msg = await _messageFactory.SendGiftCardNotificationAsync(giftCard, languageId);
                        if (msg?.Email?.Id != null)
                        {
                            giftCard.IsRecipientNotified = true;

                            await _db.SaveChangesAsync();

                            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

                            return RedirectToAction(nameof(Edit), giftCard.Id);
                        }
                    }
                    else
                    {
                        NotifyError(T("Admin.GiftCards.SenderEmailInvalid"));
                    }
                }
                else
                {
                    NotifyError(T("Admin.GiftCards.RecipientEmailInvalid"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex, false);
            }

            await PrepareGiftCardModel(model, giftCard);

            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(model);
        }

        private async Task PrepareGiftCardModel(GiftCardModel model, GiftCard giftCard)
        {
            if (giftCard != null)
            {
                var remainAmount = await _giftCardService.GetRemainingAmountAsync(giftCard);

                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(giftCard.CreatedOnUtc, DateTimeKind.Utc);
                model.AmountStr = Services.CurrencyService.PrimaryCurrency.AsMoney(giftCard.Amount).ToString(true);
                model.RemainingAmountStr = remainAmount.ToString(true);
                model.EditUrl = Url.Action("Edit", "GiftCard", new { id = giftCard.Id, area = "Admin" });
                model.PurchasedWithOrderId = giftCard.PurchasedWithOrderItem?.OrderId;
            }
        }
    }
}
