using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class CustomerRoleController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;
        
        public CustomerRoleController(
            SmartDbContext db,
            CustomerSettings customerSettings)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available customer roles. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="includeSystemRoles">Specifies whether to include system roles.</param>
        /// <returns>List of all customer roles as JSON.</returns>
        public async Task<IActionResult> AllCustomerRoles(string label, string selectedIds, bool? includeSystemRoles)
        {
            var query = _db.CustomerRoles.AsNoTracking();
            
            if (!(includeSystemRoles ?? true))
            {
                query = query.Where(x => x.IsSystemRole);
            }

            query = query.ApplyStandardFilter(true);

            var rolesPager = new FastPager<CustomerRole>(query, 500);
            var customerRoles = new List<CustomerRole>();
            var ids = selectedIds.ToIntArray();

            while ((await rolesPager.ReadNextPageAsync<CustomerRole>()).Out(out var roles))
            {
                customerRoles.AddRange(roles);
            }

            var list = customerRoles
                .OrderBy(x => x.Name)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = ids.Contains(x.Id)
                })
                .ToList();

            if (label.HasValue())
            {
                list.Insert(0, new ChoiceListItem
                {
                    Id = "0",
                    Text = label,
                    Selected = false
                });
            }

            return new JsonResult(list);
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Customer.Role.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> RolesList(GridCommand command)
        {
            var mapper = MapperFactory.GetMapper<CustomerRole, CustomerRoleModel>();

            var customerRoles = await _db.CustomerRoles
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await customerRoles.SelectAsync(async x =>
            {
                var model = await mapper.MapAsync(x);
                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<CustomerRoleModel>
            {
                Rows = rows,
                Total = customerRoles.TotalCount
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Customer.Role.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerRoleModel
            {
                Active = true
            };

            await PrepareViewBag(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Customer.Role.Create)]
        public async Task<IActionResult> Create(CustomerRoleModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<CustomerRoleModel, CustomerRole>();
                var customerRole = await mapper.MapAsync(model);

                _db.Add(customerRole);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewCustomerRole, T("ActivityLog.AddNewCustomerRole"), customerRole.Name);
                NotifySuccess(T("Admin.Customers.CustomerRoles.Added"));

                return continueEditing 
                    ? RedirectToAction("Edit", new { id = customerRole.Id }) 
                    : RedirectToAction("List");
            }

            return View(model);
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var customerRole = await _db.CustomerRoles
                .Include(x => x.RuleSets)
                .FindByIdAsync(id, false);
            if (customerRole == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<CustomerRole, CustomerRoleModel>();
            var model = await mapper.MapAsync(customerRole);

            await PrepareViewBag(model, customerRole);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Customer.Role.Update)]
        public async Task<IActionResult> Edit(CustomerRoleModel model, bool continueEditing, IFormCollection form)
        {
            var customerRole = await _db.CustomerRoles
                .Include(x => x.RuleSets)
                .FindByIdAsync(model.Id, true);
            if (customerRole == null)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    if (customerRole.IsSystemRole && !model.Active)
                    {
                        throw new SmartException(T("Admin.Customers.CustomerRoles.Fields.Active.CantEditSystem"));
                    }

                    if (customerRole.IsSystemRole && !customerRole.SystemName.EqualsNoCase(model.SystemName))
                    {
                        throw new SmartException(T("Admin.Customers.CustomerRoles.Fields.SystemName.CantEditSystem"));
                    }

                    //...

                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id = customerRole.Id });
        }


        private async Task PrepareViewBag(CustomerRoleModel model, CustomerRole role)
        {
            if (role != null)
            {
                var showRuleApplyButton = model.SelectedRuleSetIds.Any();

                if (!showRuleApplyButton)
                {
                    // Ignore deleted customers.
                    showRuleApplyButton = await _db.CustomerRoleMappings
                        .AsNoTracking()
                        .Where(x => x.CustomerRoleId == role.Id && x.Customer != null)
                        .AnyAsync();
                }

                ViewBag.ShowRuleApplyButton = showRuleApplyButton;
                ViewBag.PermissionTree = await Services.Permissions.GetPermissionTreeAsync(role, true);
                ViewBag.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
            }

            ViewBag.TaxDisplayTypes = model.TaxDisplayType.HasValue
                ? ((TaxDisplayType)model.TaxDisplayType.Value).ToSelectList().ToList()
                : TaxDisplayType.IncludingTax.ToSelectList(false).ToList();

            ViewBag.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
        }
    }
}
