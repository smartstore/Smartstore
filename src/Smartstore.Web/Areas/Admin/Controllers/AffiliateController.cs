using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Affiliates;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Controllers
{
    public class AffiliateController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IWebHelper _webHelper;
        private readonly CustomerSettings _customerSettings;

        public AffiliateController(
            SmartDbContext db,
            ICommonServices services,
            IDateTimeHelper dateTimeHelper, 
            IWebHelper webHelper,
            CustomerSettings customerSettings)
        {
            _db = db;
            _services = services;
            _dateTimeHelper = dateTimeHelper;
            _webHelper = webHelper;
            _customerSettings = customerSettings;
        }
        
        [NonAction]
        protected async Task PrepareAffiliateModelAsync(AffiliateModel model, Affiliate affiliate, bool excludeProperties)
        {
            if (affiliate != null)
            {
                model.Id = affiliate.Id;
                model.Url = _webHelper.ModifyQueryString(_webHelper.GetStoreLocation(false), "affiliateid=" + affiliate.Id, null);
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

            var countries = await _db.Countries
                .AsNoTracking()
                .Include(x => x.StateProvinces.OrderBy(x => x.DisplayOrder))
                .ApplyStandardFilter(true)
                .ToListAsync();

            foreach (var c in countries)
            {
                model.Address.AvailableCountries.Add(new SelectListItem
                { 
                    Text = c.Name, 
                    Value = c.Id.ToString(), 
                    Selected = affiliate != null && c.Id == affiliate.Address.CountryId 
                });
            }

            var availableStates = new List<SelectListItem>();
            if (model.Address.CountryId != null)
            {
                var states = await _db.StateProvinces
                    .AsNoTracking()
                    .ApplyCountryFilter(model.Address.CountryId.Value)
                    .ToListAsync();

                if (states.Any())
                {
                    foreach (var s in states)
                    {
                        availableStates.Add(new SelectListItem
                        {
                            Text = s.GetLocalized(x => x.Name),
                            Value = s.Id.ToString(),
                            Selected = s.Id == affiliate.Address.StateProvinceId
                        });
                    }
                }
                else
                {
                    availableStates.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
                }
            }
            else
            {
                availableStates.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
            }

            model.Address.AvailableStates = availableStates;
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
        public async Task<IActionResult> List(GridCommand command)
        {
            var affiliates = await _db.Affiliates
                .AsNoTracking()
                .Include(x => x.Address)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var affiliateModels = affiliates
                .Select(x =>
                {
                    return new AffiliateModel {
                        Id = x.Id,
                        AddressEmail = x.Address.Email,
                        AddressFirstName = x.Address.FirstName,
                        AddressLastName = x.Address.LastName,
                        EditUrl = Url.Action("Edit", "Affiliate", new { id = x.Id })
                    };
                })
                .ToList();

            var gridModel = new GridModel<AffiliateModel>
            {
                Rows = affiliateModels,
                Total = await affiliates.GetTotalCountAsync()
            };

            return Json(gridModel);
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
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = affiliate.Id }) : RedirectToAction(nameof(List));
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
                return continueEditing ? RedirectToAction(nameof(Edit), affiliate.Id) : RedirectToAction(nameof(List));
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
        public async Task<IActionResult> AffiliatesDelete(GridSelection selection)
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
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var orderModels = await orders.SelectAsync(async order =>
            {
                var orderModel = new AffiliateModel.AffiliatedOrderModel
                {
                    Id = order.Id,
                    OrderStatus = await _services.Localization.GetLocalizedEnumAsync(order.OrderStatus),
                    PaymentStatus = await _services.Localization.GetLocalizedEnumAsync(order.PaymentStatus),
                    ShippingStatus = await _services.Localization.GetLocalizedEnumAsync(order.ShippingStatus),
                    OrderTotal = Services.CurrencyService.PrimaryCurrency.AsMoney(order.OrderTotal),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("Edit", "Order", new { id = order.Id })
                };
                return orderModel;
            }).AsyncToList();

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
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var customerModels = customers.Select(customer =>
            {
                return new AffiliateModel.AffiliatedCustomerModel
                {
                    Id = customer.Id,
                    Email = customer.Email,
                    Username = customer.Username,
                    FullName = customer.GetFullName(),
                    EditUrl = Url.Action("Edit", "Customer", new { id = customer.Id })
            };

            }).ToList();

            var gridModel = new GridModel<AffiliateModel.AffiliatedCustomerModel>
            {
                Rows = customerModels,
                Total = customers.TotalCount
            };

            return Json(gridModel);
        }
    }
}