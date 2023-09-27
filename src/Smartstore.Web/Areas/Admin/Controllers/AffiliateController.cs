using Smartstore.Admin.Models.Affiliates;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class AffiliateController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;

        public AffiliateController(SmartDbContext db, CustomerSettings customerSettings)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        private async Task PrepareAffiliateModelAsync(AffiliateModel model, Affiliate affiliate, bool excludeProperties)
        {
            if (affiliate != null)
            {
                model.Id = affiliate.Id;
                model.Url = Services.WebHelper.ModifyQueryString(Services.WebHelper.GetStoreLocation(), "affiliateid=" + affiliate.Id);

                if (!excludeProperties)
                {
                    model.Active = affiliate.Active;
                    await affiliate.Address.MapAsync(model.Address);
                }
            }

            model.Address.CompanyEnabled = true;
            model.Address.CountryEnabled = true;
            model.Address.StateProvinceEnabled = true;
            model.Address.CityEnabled = true;
            model.Address.CityRequired = true;
            model.Address.StreetAddressEnabled = true;
            model.Address.StreetAddressRequired = true;
            model.Address.StreetAddress2Enabled = true;
            model.Address.ZipPostalCodeEnabled = true;
            model.Address.ZipPostalCodeRequired = true;
            model.Address.PhoneEnabled = true;
            model.Address.PhoneRequired = true;
            model.Address.FaxEnabled = true;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Promotion.Affiliate.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Affiliate.Read)]
        public async Task<IActionResult> AffiliateList(GridCommand command)
        {
            var affiliates = await _db.Affiliates
                .AsNoTracking()
                .Include(x => x.Address)
                .OrderBy(x => x.Id)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var addressMapper = MapperFactory.GetMapper<Address, AddressModel>();
            var rows = await affiliates
                .SelectAwait(async x => new AffiliateModel
                {
                    Id = x.Id,
                    Active = x.Active,
                    EditUrl = Url.Action(nameof(Edit), new { id = x.Id }),
                    Address = await addressMapper.MapAsync(x.Address)
                })
                .AsyncToList();

            return Json(new GridModel<AffiliateModel>
            {
                Rows = rows,
                Total = await affiliates.GetTotalCountAsync()
            });
        }

        [Permission(Permissions.Promotion.Affiliate.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new AffiliateModel();
            await PrepareAffiliateModelAsync(model, null, false);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Promotion.Affiliate.Create)]
        public async Task<IActionResult> Create(AffiliateModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var affiliate = new Affiliate
                {
                    Active = model.Active
                };

                affiliate.Address = await MapperFactory.MapAsync<AddressModel, Address>(model.Address);
                affiliate.Address.CreatedOnUtc = DateTime.UtcNow;

                _db.Affiliates.Add(affiliate);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Affiliates.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = affiliate.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareAffiliateModelAsync(model, null, true);

            return View(model);
        }

        [Permission(Permissions.Promotion.Affiliate.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var affiliate = await _db.Affiliates
                .Include(x => x.Address)
                .FindByIdAsync(id, false);

            if (affiliate == null || affiliate.Deleted)
            {
                return NotFound();
            }

            var model = new AffiliateModel();
            await PrepareAffiliateModelAsync(model, affiliate, false);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Promotion.Affiliate.Update)]
        public async Task<IActionResult> Edit(AffiliateModel model, bool continueEditing)
        {
            var affiliate = await _db.Affiliates.FindByIdAsync(model.Id, true);

            if (affiliate == null || affiliate.Deleted)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                affiliate.Active = model.Active;
                affiliate.Address = await MapperFactory.MapAsync<AddressModel, Address>(model.Address);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Affiliates.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), affiliate.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareAffiliateModelAsync(model, affiliate, true);
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var affiliate = await _db.Affiliates.FindByIdAsync(id);
            if (affiliate == null)
            {
                return NotFound();
            }

            _db.Affiliates.Remove(affiliate);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Affiliates.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> AffiliateDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds().ToList();

            if (ids.Any())
            {
                var affiliates = await _db.Affiliates.GetManyAsync(ids, true);

                _db.Affiliates.RemoveRange(affiliates);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new
            {
                Success = success,
                Count = numDeleted
            });
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Affiliate.Read)]
        public async Task<IActionResult> AffiliatedOrderList(int affiliateId, GridCommand command)
        {
            var orders = await _db.Orders
                .Where(x => x.AffiliateId == affiliateId)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;

            var orderModels = orders
                .Select(x => new AffiliateModel.AffiliatedOrderModel
                {
                    Id = x.Id,
                    OrderStatus = Services.Localization.GetLocalizedEnum(x.OrderStatus),
                    PaymentStatus = Services.Localization.GetLocalizedEnum(x.PaymentStatus),
                    ShippingStatus = Services.Localization.GetLocalizedEnum(x.ShippingStatus),
                    OrderTotal = Services.CurrencyService.CreateMoney(x.OrderTotal, primaryCurrency),
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("Edit", "Order", new { id = x.Id })
                })
                .ToList();

            var gridModel = new GridModel<AffiliateModel.AffiliatedOrderModel>
            {
                Rows = orderModels,
                Total = orders.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Affiliate.Read)]
        public async Task<IActionResult> AffiliatedCustomerList(int affiliateId, GridCommand command)
        {
            var customers = await _db.Customers
                .Where(x => x.AffiliateId == affiliateId)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var customerModels = customers
                .Select(x => new AffiliateModel.AffiliatedCustomerModel
                {
                    Id = x.Id,
                    Email = x.Email,
                    Username = x.Username,
                    FullName = x.GetFullName(),
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("Edit", "Customer", new { id = x.Id })
                })
                .ToList();

            var gridModel = new GridModel<AffiliateModel.AffiliatedCustomerModel>
            {
                Rows = customerModels,
                Total = customers.TotalCount
            };

            return Json(gridModel);
        }
    }
}