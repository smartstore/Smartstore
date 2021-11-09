using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Tax.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Polls.Controllers
{
    [Area("Admin")]
    [Route("[area]/taxfixedrate/{action=index}/{id?}")]
    public class TaxFixedRateController : AdminController
    {
        private readonly SmartDbContext _db;

        public TaxFixedRateController(SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Configure));
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _db.TaxCategories.AnyAsync())
            {
                NotifyWarning(T("Plugins.Tax.CountryStateZip.NoTaxCategoriesFound"));
            }

            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> List(GridCommand command)
        {
            var taxRates = await _db.TaxCategories
                .AsNoTracking()
                .ToPagedList(command)
                .LoadAsync();

            var taxRateModels = await taxRates.SelectAsync(async x => new FixedTaxRateModel()
            {
                TaxCategoryId = x.Id,
                TaxCategoryName = x.Name,
                Rate = await Services.Settings.GetSettingByKeyAsync<decimal>($"Tax.TaxProvider.FixedRate.TaxCategoryId{x.Id}")
            }).AsyncToList();

            var gridModel = new GridModel<FixedTaxRateModel>
            {
                Rows = taxRateModels,
                Total = await taxRates.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Update)]
        public async Task<IActionResult> Update(FixedTaxRateModel model)
        {
            await Services.Settings.ApplySettingAsync($"Tax.TaxProvider.FixedRate.TaxCategoryId{model.TaxCategoryId}", model.Rate);
            return Json(new { success = true });
        }
    }
}
