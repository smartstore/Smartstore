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
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class GiftCardController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IGiftCardService _giftCardService;
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;
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
            _currencyService = currencyService;
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
                new() { Value = "true", Text = T("Common.Activated") },
                new() { Value = "false", Text = T("Common.Deactivated") }
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
                    UsedValue = _currencyService.CreateMoney(x.UsedValue, _primaryCurrency).ToString(true),
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
        public async Task<IActionResult> Create()
        {
            await PrepareViewBag();

            return View(new GiftCardModel());
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Order.GiftCard.Create)]
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

            await PrepareViewBag();

            return View(model);
        }

        [Permission(Permissions.Order.GiftCard.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var giftCard = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .ThenInclude(x => x.Order)
                .FindByIdAsync(id);

            if (giftCard == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<GiftCard, GiftCardModel>();
            var model = await mapper.MapAsync(giftCard);

            await PrepareGiftCardModel(model, giftCard);
            await PrepareViewBag();

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.GiftCard.Update)]
        public async Task<IActionResult> Edit(GiftCardModel model, bool continueEditing)
        {
            var giftCard = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .ThenInclude(x => x.Order)
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
                    ? RedirectToAction(nameof(Edit), new { id = giftCard.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareGiftCardModel(model, giftCard);
            await PrepareViewBag();

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

        [HttpPost]
        [Permission(Permissions.Order.GiftCard.Notify)]
        public async Task<IActionResult> NotifyRecipient(GiftCardModel model)
        {
            var giftCard = await _db.GiftCards.FindByIdAsync(model.Id);
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
                        var msg = await _messageFactory.SendGiftCardNotificationAsync(giftCard, model.LanguageId);
                        if (msg?.Email?.Id != null)
                        {
                            giftCard.IsRecipientNotified = true;

                            await _db.SaveChangesAsync();
                            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
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

            return RedirectToAction(nameof(Edit), new { id = giftCard.Id });
        }

        private async Task PrepareGiftCardModel(GiftCardModel model, GiftCard giftCard)
        {
            if (giftCard != null)
            {
                var remainAmount = await _giftCardService.GetRemainingAmountAsync(giftCard);
                var languageId = giftCard?.PurchasedWithOrderItem?.Order?.CustomerLanguageId;

                if (languageId.HasValue && !await _db.Languages.AnyAsync(x => x.Id == languageId.Value))
                {
                    languageId = null;
                }

                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(giftCard.CreatedOnUtc, DateTimeKind.Utc);
                model.AmountStr = _currencyService.CreateMoney(giftCard.Amount, _primaryCurrency).ToString(true);
                model.RemainingAmountStr = remainAmount.ToString(true);
                model.EditUrl = Url.Action(nameof(Edit), "GiftCard", new { id = giftCard.Id, area = "Admin" });
                model.PurchasedWithOrderId = giftCard.PurchasedWithOrderItem?.OrderId;
                model.LanguageId = languageId ?? Services.WorkContext.WorkingLanguage.Id;
            }
        }

        private async Task PrepareViewBag()
        {
            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;
            ViewBag.AllLanguages = (await _languageService.GetAllLanguagesAsync(true)).ToSelectListItems();
        }
    }
}
