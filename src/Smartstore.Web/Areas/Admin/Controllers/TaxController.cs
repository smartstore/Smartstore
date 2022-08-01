using Smartstore.Admin.Models.Tax;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class TaxController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly ModuleManager _moduleManager;
        private readonly TaxSettings _taxSettings;

        public TaxController(
            SmartDbContext db,
            ITaxService taxService,
            ModuleManager moduleManager,
            TaxSettings taxSettings)
        {
            _db = db;
            _taxService = taxService;
            _moduleManager = moduleManager;
            _taxSettings = taxSettings;
        }

        #region Tax Providers

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Providers));
        }

        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> Providers()
        {
            var taxProviderModels = await _taxService.LoadAllTaxProviders()
                .SelectAwait(async x =>
                {
                    var model = _moduleManager.ToProviderModel<ITaxProvider, TaxProviderModel>(x);
                    if (x.Metadata.SystemName.Equals(_taxSettings.ActiveTaxProviderSystemName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        model.IsPrimaryTaxProvider = true;
                    }
                    else
                    {
                        await _moduleManager.ActivateDependentWidgetsAsync(x.Metadata, false);
                    }

                    return model;
                })
                .AsyncToList();

            return View(taxProviderModels);
        }

        [Permission(Permissions.Configuration.Tax.Activate)]
        public async Task<IActionResult> ActivateProvider(string systemName)
        {
            if (systemName.HasValue())
            {
                var taxProvider = _taxService.LoadTaxProviderBySystemName(systemName);
                if (taxProvider != null)
                {
                    _taxSettings.ActiveTaxProviderSystemName = systemName;
                    await Services.SettingFactory.SaveSettingsAsync(_taxSettings);
                    await _moduleManager.ActivateDependentWidgetsAsync(taxProvider.Metadata, true);
                }
            }

            return RedirectToAction(nameof(Providers));
        }

        #endregion

        #region Tax categories

        [Permission(Permissions.Configuration.Tax.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> TaxCategoryList(GridCommand command)
        {
            var categories = await _db.TaxCategories
                .AsNoTracking()
                // Info: We use OrderBy to circumvent EF caching issue.
                .OrderBy(x => x.Id)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var categoriesModels = await categories
                .SelectAwait(x => MapperFactory.MapAsync<TaxCategory, TaxCategoryModel>(x))
                .AsyncToList();

            var gridModel = new GridModel<TaxCategoryModel>
            {
                Rows = categoriesModels,
                Total = await categories.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Create)]
        public async Task<IActionResult> TaxCategoryInsert(TaxCategoryModel model)
        {
            var success = false;

            if (!await _db.TaxCategories.AnyAsync(x => x.Name == model.Name))
            {
                _db.TaxCategories.Add(new TaxCategory
                {
                    Name = model.Name,
                    DisplayOrder = model.DisplayOrder
                });

                await _db.SaveChangesAsync();
                success = true;
            }
            else
            {
                NotifyError(T("Admin.Tax.Categories.NoDuplicatesAllowed"));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Update)]
        public async Task<IActionResult> TaxCategoryUpdate(TaxCategoryModel model)
        {
            var success = false;
            var taxCategory = await _db.TaxCategories.FindByIdAsync(model.Id, true);

            if (taxCategory != null)
            {
                await MapperFactory.MapAsync(model, taxCategory);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Delete)]
        public async Task<IActionResult> TaxCategoryDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var taxCategories = await _db.TaxCategories.GetManyAsync(ids, true);

                _db.TaxCategories.RemoveRange(taxCategories);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion
    }
}
