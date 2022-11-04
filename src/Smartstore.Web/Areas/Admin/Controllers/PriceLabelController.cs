using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class PriceLabelController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public PriceLabelController(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public async Task<IActionResult> PriceLabelList(GridCommand command)
        {
            var priceLabels = await _db.PriceLabels
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<PriceLabel, PriceLabelModel>();
            var priceLabelModels = await priceLabels
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            var gridModel = new GridModel<PriceLabelModel>
            {
                Rows = priceLabelModels,
                Total = await priceLabels.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Delete)]
        public async Task<IActionResult> PriceLabelDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var priceLabels = await _db.PriceLabels.GetManyAsync(ids, true);
                    _db.PriceLabels.RemoveRange(priceLabels);

                    numDeleted = await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [Permission(Permissions.Configuration.PriceLabel.Create)]
        public IActionResult CreatePriceLabelPopup(string btnId, string formId)
        {
            var model = new PriceLabelModel();
            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Create)]
        public async Task<IActionResult> CreatePriceLabelPopup(PriceLabelModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var priceLabel = await MapperFactory.MapAsync<PriceLabelModel, PriceLabel>(model);
                    _db.PriceLabels.Add(priceLabel);
                    await _db.SaveChangesAsync();

                    await UpdateLocalesAsync(priceLabel, model);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.PriceLabel.Added"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public async Task<IActionResult> EditPriceLabelPopup(int id, string btnId, string formId)
        {
            var priceLabel = await _db.PriceLabels.FindByIdAsync(id, false);
            if (priceLabel == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<PriceLabel, PriceLabelModel>(priceLabel);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = priceLabel.GetLocalized(x => x.Name, languageId, false, false);
                locale.ShortName = priceLabel.GetLocalized(x => x.ShortName, languageId, false, false);
                locale.Description = priceLabel.GetLocalized(x => x.Description, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public async Task<IActionResult> EditPriceLabelPopup(PriceLabelModel model, string btnId, string formId)
        {
            var priceLabel = await _db.PriceLabels.FindByIdAsync(model.Id);
            if (priceLabel == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await MapperFactory.MapAsync(model, priceLabel);
                    await UpdateLocalesAsync(priceLabel, model);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.PriceLabels.Updated"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [HttpPost, LoadSetting, SaveSetting]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public IActionResult SetDefault(int id, string type, PriceSettings priceSettings)
        {
            Guard.NotZero(id, nameof(id));

            if (type == "compare-price")
            {
                priceSettings.DefaultComparePriceLabelId = id;
            }
            else
            {
                priceSettings.DefaultRegularPriceLabelId = id;
            }
            
            return Json(new { Success = true });
        }

        private async Task UpdateLocalesAsync(PriceLabel priceLabel, PriceLabelModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(priceLabel, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(priceLabel, x => x.ShortName, localized.ShortName, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(priceLabel, x => x.Description, localized.Description, localized.LanguageId);
            }
        }
    }
}