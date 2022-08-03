using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Orders;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CheckoutAttributeController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IActivityLogger _activityLogger;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ICurrencyService _currencyService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly MeasureSettings _measureSettings;
        private readonly AdminAreaSettings _adminAreaSettings;

        public CheckoutAttributeController(
            SmartDbContext db,
            IActivityLogger activityLogger,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            ICurrencyService currencyService,
            MeasureSettings measureSettings,
            IStoreMappingService storeMappingService,
            AdminAreaSettings adminAreaSettings)
        {
            _db = db;
            _activityLogger = activityLogger;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _currencyService = currencyService;
            _measureSettings = measureSettings;
            _storeMappingService = storeMappingService;
            _adminAreaSettings = adminAreaSettings;
        }

        #region Utilities

        [NonAction]
        public async Task UpdateAttributeLocalesAsync(CheckoutAttribute checkoutAttribute, CheckoutAttributeModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(checkoutAttribute, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(checkoutAttribute, x => x.TextPrompt, localized.TextPrompt, localized.LanguageId);
            }
        }

        [NonAction]
        public async Task UpdateValueLocalesAsync(CheckoutAttributeValue checkoutAttributeValue, CheckoutAttributeValueModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(checkoutAttributeValue, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        [NonAction]
        private async Task PrepareCheckoutAttributeModelAsync(CheckoutAttributeModel model, CheckoutAttribute checkoutAttribute, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableTaxCategories = new List<SelectListItem>();
            foreach (var tc in taxCategories)
            {
                ViewBag.AvailableTaxCategories.Add(new SelectListItem
                {
                    Text = tc.Name,
                    Value = tc.Id.ToString(),
                    Selected = checkoutAttribute != null && !excludeProperties && tc.Id == checkoutAttribute.TaxCategoryId
                });
            }

            if (!excludeProperties)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(checkoutAttribute);
            }
        }

        [NonAction]
        private async Task PrepareModelAsync(CheckoutAttributeValueModel model, CheckoutAttribute attribute)
        {
            var baseWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);

            model.CheckoutAttributeId = attribute?.Id ?? 0;
            model.IsListTypeAttribute = attribute?.IsListTypeAttribute ?? false;
            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            model.BaseWeightIn = baseWeight != null ? baseWeight.GetLocalized(x => x.Name) : string.Empty;
        }

        #endregion

        #region Checkout attributes

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public IActionResult List()
        {
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View();
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public async Task<IActionResult> CheckoutAttributeList(GridCommand command)
        {
            var model = new GridModel<CheckoutAttributeModel>();

            var checkoutAttributes = await _db.CheckoutAttributes
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<CheckoutAttribute, CheckoutAttributeModel>();
            var checkoutAttributesModels = await checkoutAttributes
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.AttributeControlTypeName = x.AttributeControlType.GetLocalizedEnum(Services.WorkContext.WorkingLanguage.Id);
                    model.EditUrl = Url.Action(nameof(Edit), "CheckoutAttribute", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<CheckoutAttributeModel>
            {
                Rows = checkoutAttributesModels,
                Total = await checkoutAttributes.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> CheckoutAttributeUpdate(CheckoutAttributeModel model)
        {
            var success = false;
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(model.Id);

            if (checkoutAttribute != null)
            {
                await MapperFactory.MapAsync(model, checkoutAttribute);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(Permissions.Cart.CheckoutAttribute.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CheckoutAttributeModel
            {
                IsActive = true
            };

            AddLocales(model.Locales);
            await PrepareCheckoutAttributeModelAsync(model, null, true);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Cart.CheckoutAttribute.Create)]
        public async Task<IActionResult> Create(CheckoutAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var checkoutAttribute = await MapperFactory.MapAsync<CheckoutAttributeModel, CheckoutAttribute>(model);
                _db.CheckoutAttributes.Add(checkoutAttribute);
                await _db.SaveChangesAsync();

                await UpdateAttributeLocalesAsync(checkoutAttribute, model);
                await SaveStoreMappingsAsync(checkoutAttribute, model.SelectedStoreIds);

                _activityLogger.LogActivity(KnownActivityLogTypes.AddNewCheckoutAttribute, T("ActivityLog.AddNewCheckoutAttribute"), checkoutAttribute.Name);

                NotifySuccess(T("Admin.Catalog.Attributes.CheckoutAttributes.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = checkoutAttribute.Id }) : RedirectToAction(nameof(List));
            }

            await PrepareCheckoutAttributeModelAsync(model, null, true);

            return View(model);
        }

        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(id, false);
            if (checkoutAttribute == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<CheckoutAttribute, CheckoutAttributeModel>(checkoutAttribute);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = checkoutAttribute.GetLocalized(x => x.Name, languageId, false, false);
                locale.TextPrompt = checkoutAttribute.GetLocalized(x => x.TextPrompt, languageId, false, false);
            });

            await PrepareCheckoutAttributeModelAsync(model, checkoutAttribute, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> Edit(CheckoutAttributeModel model, bool continueEditing)
        {
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(model.Id);
            if (checkoutAttribute == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, checkoutAttribute);
                await UpdateAttributeLocalesAsync(checkoutAttribute, model);
                await SaveStoreMappingsAsync(checkoutAttribute, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                _activityLogger.LogActivity(KnownActivityLogTypes.EditCheckoutAttribute, T("ActivityLog.EditCheckoutAttribute"), checkoutAttribute.Name);

                NotifySuccess(T("Admin.Catalog.Attributes.CheckoutAttributes.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), checkoutAttribute.Id) : RedirectToAction(nameof(List));
            }

            await PrepareCheckoutAttributeModelAsync(model, checkoutAttribute, true);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(id);
            if (checkoutAttribute == null)
            {
                return NotFound();
            }

            _db.CheckoutAttributes.Remove(checkoutAttribute);
            await _db.SaveChangesAsync();

            _activityLogger.LogActivity(KnownActivityLogTypes.DeleteCheckoutAttribute, T("ActivityLog.DeleteCheckoutAttribute"), checkoutAttribute.Name);

            NotifySuccess(T("Admin.Catalog.Attributes.CheckoutAttributes.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Delete)]
        public async Task<IActionResult> CheckoutAttributeDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var checkoutAttributes = await _db.CheckoutAttributes.GetManyAsync(ids, true);

                _db.CheckoutAttributes.RemoveRange(checkoutAttributes);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Checkout attribute values

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public async Task<IActionResult> ValueList(int checkoutAttributeId, GridCommand command)
        {
            var values = await _db.CheckoutAttributeValues
                 .AsNoTracking()
                 .Where(x => x.CheckoutAttributeId == checkoutAttributeId)
                 .ApplyGridCommand(command)
                 .ToPagedList(command)
                 .LoadAsync();

            var mapper = MapperFactory.GetMapper<CheckoutAttributeValue, CheckoutAttributeValueModel>();
            var valuesModel = await values
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.NameString = (x.Color.IsEmpty() ? x.Name : $"{x.Name} - {x.Color}").HtmlEncode();

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<CheckoutAttributeValueModel>
            {
                Rows = valuesModel,
                Total = await values.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> ValueUpdate(CheckoutAttributeValueModel model)
        {
            var success = false;
            var checkoutAttributeValue = await _db.CheckoutAttributeValues.FindByIdAsync(model.Id);

            if (checkoutAttributeValue != null)
            {
                await MapperFactory.MapAsync(model, checkoutAttributeValue);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> ValueCreatePopup(int checkoutAttributeId, string btnId, string formId)
        {
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(checkoutAttributeId, false);
            if (checkoutAttribute == null)
            {
                return NotFound();
            }

            var model = new CheckoutAttributeValueModel();
            await PrepareModelAsync(model, checkoutAttribute);

            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> ValueCreatePopup(string btnId, string formId, CheckoutAttributeValueModel model)
        {
            var checkoutAttribute = await _db.CheckoutAttributes.FindByIdAsync(model.CheckoutAttributeId, false);
            if (checkoutAttribute == null)
            {
                return NotFound();
            }

            await PrepareModelAsync(model, checkoutAttribute);

            if (ModelState.IsValid)
            {
                var checkoutAttributeValue = await MapperFactory.MapAsync<CheckoutAttributeValueModel, CheckoutAttributeValue>(model);
                _db.CheckoutAttributeValues.Add(checkoutAttributeValue);
                await _db.SaveChangesAsync();

                await UpdateValueLocalesAsync(checkoutAttributeValue, model);

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> ValueEditPopup(int id, string btnId, string formId)
        {
            var checkoutAttributeValue = await _db.CheckoutAttributeValues.FindByIdAsync(id);
            if (checkoutAttributeValue == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<CheckoutAttributeValue, CheckoutAttributeValueModel>(checkoutAttributeValue);
            await PrepareModelAsync(model, checkoutAttributeValue.CheckoutAttribute);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = checkoutAttributeValue.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public async Task<IActionResult> ValueEditPopup(CheckoutAttributeValueModel model, string btnId, string formId)
        {
            var checkoutAttributeValue = await _db.CheckoutAttributeValues.FindByIdAsync(model.Id);
            if (checkoutAttributeValue == null)
            {
                return NotFound();
            }

            await PrepareModelAsync(model, checkoutAttributeValue.CheckoutAttribute);

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, checkoutAttributeValue);
                await UpdateValueLocalesAsync(checkoutAttributeValue, model);
                await _db.SaveChangesAsync();

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Delete)]
        public async Task<IActionResult> DeleteValueSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var checkoutAttributeValues = await _db.CheckoutAttributeValues.GetManyAsync(ids, true);

                _db.CheckoutAttributeValues.RemoveRange(checkoutAttributeValues);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion
    }
}
