using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Shipping.Controllers
{
    [Route("[area]/fixedrateshipping/{action=index}/{id?}")]
    public class FixedRateController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;

        public FixedRateController(SmartDbContext db, IProviderManager providerManager)
        {
            _db = db;
            _providerManager = providerManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Configure));
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _db.ShippingMethods.AnyAsync())
            {
                NotifyWarning(T("Admin.Configuration.Shipping.Methods.NoMethodsLoaded"));
            }

            ViewBag.Provider = _providerManager.GetProvider("Shipping.FixedRate").Metadata;

            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> FixedRateList(GridCommand command)
        {
            var shippingMethods = await _db.ShippingMethods
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToPagedList(command)
                .LoadAsync();

            var ShippingRateModels = await shippingMethods.SelectAwait(async x => new FixedRateModel
            {
                ShippingMethodId = x.Id,
                ShippingMethodName = x.Name,
                Rate = await Services.Settings.GetSettingByKeyAsync<decimal>($"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{x.Id}")
            }).AsyncToList();

            var gridModel = new GridModel<FixedRateModel>
            {
                Rows = ShippingRateModels,
                Total = await shippingMethods.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public async Task<IActionResult> FixedRateUpdate(FixedRateModel model)
        {
            await Services.Settings.ApplySettingAsync($"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{model.ShippingMethodId}", model.Rate);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
