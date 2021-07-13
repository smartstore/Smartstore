using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Scheduling;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class CustomerRoleController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IRoleStore _roleStore;
        private readonly Lazy<ITaskStore> _taskStore;
        private readonly Lazy<ITaskScheduler> _taskScheduler;
        private readonly CustomerSettings _customerSettings;
        
        public CustomerRoleController(
            SmartDbContext db,
            IRoleStore roleStore,
            Lazy<ITaskStore> taskStore,
            Lazy<ITaskScheduler> taskScheduler,
            CustomerSettings customerSettings)
        {
            _db = db;
            _roleStore = roleStore;
            _taskStore = taskStore;
            _taskScheduler = taskScheduler;
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
            var query = _roleStore.Roles.AsNoTracking();
            
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

            var customerRoles = await _roleStore.Roles
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

                await _roleStore.CreateAsync(customerRole, default);

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
            var customerRole = await _roleStore.FindByIdAsync(id.ToString(), default);
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
            var customerRole = await _roleStore.FindByIdAsync(model.Id.ToString(), default);
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

                    var mapper = MapperFactory.GetMapper<CustomerRoleModel, CustomerRole>();
                    await mapper.MapAsync(model, customerRole);

                    await _roleStore.UpdateAsync(customerRole, default);

                    // INFO: cached permission tree removed by PermissionRoleMappingHook.
                    await UpdatePermissionRoleMappings(customerRole, form);

                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditCustomerRole, T("ActivityLog.EditCustomerRole"), customerRole.Name);
                    NotifySuccess(T("Admin.Customers.CustomerRoles.Updated"));

                    return continueEditing 
                        ? RedirectToAction("Edit", new { id = customerRole.Id }) 
                        : RedirectToAction("List");
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id = customerRole.Id });
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var customerRole = await _roleStore.FindByIdAsync(id.ToString(), default);
            if (customerRole == null)
            {
                return NotFound();
            }

            try
            {
                await _roleStore.DeleteAsync(customerRole, default);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteCustomerRole, T("ActivityLog.DeleteCustomerRole"), customerRole.Name);
                NotifySuccess(T("Admin.Customers.CustomerRoles.Deleted"));

                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return RedirectToAction("Edit", new { id = customerRole.Id });
            }
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> CustomerRoleMappingsList(GridCommand command, int id)
        {
            var customerRoleMappings = await _db.CustomerRoleMappings
                .AsNoTracking()
                .ApplyStandardFilter(new[] { id })
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var customerRole = await _roleStore.FindByIdAsync(id.ToString(), default);
            var isGuestRole = customerRole.SystemName.EqualsNoCase(SystemCustomerRoleNames.Guests);
            var emailFallbackStr = isGuestRole ? T("Admin.Customers.Guest").Value : string.Empty;

            var rows = customerRoleMappings.Select(x =>
            {
                var mappingModel = new CustomerRoleMappingModel
                {
                    Id = x.Id,
                    Active = x.Customer.Active,
                    CustomerId = x.CustomerId,
                    Email = x.Customer.Email.NullEmpty() ?? emailFallbackStr,
                    Username = x.Customer.Username,
                    FullName = x.Customer.GetFullName(),
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.Customer.CreatedOnUtc, DateTimeKind.Utc),
                    LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(x.Customer.LastActivityDateUtc, DateTimeKind.Utc),
                    IsSystemMapping = x.IsSystemMapping
                };

                return mappingModel;
            })
            .ToList();

            var gridModel = new GridModel<CustomerRoleMappingModel>
            {
                Rows = rows,
                Total = customerRoleMappings.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Update)]
        public async Task<IActionResult> ApplyRules(int id)
        {
            var customerRole = await _roleStore.FindByIdAsync(id.ToString(), default);
            if (customerRole == null)
            {
                return NotFound();
            }

            var task = await _taskStore.Value.GetTaskByTypeAsync<TargetGroupEvaluatorTask>();
            if (task != null)
            {
                await _taskScheduler.Value.RunSingleTaskAsync(task.Id, new Dictionary<string, string>
                {
                    { "CustomerRoleIds", customerRole.Id.ToString() }
                });

                NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress"));
            }
            else
            {
                NotifyError(T("Admin.System.ScheduleTasks.TaskNotFound", nameof(TargetGroupEvaluatorTask)));
            }

            return RedirectToAction("Edit", new { id = customerRole.Id });
        }

        private async Task PrepareViewBag(CustomerRoleModel model, CustomerRole role)
        {
            Guard.NotNull(model, nameof(model));

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

        private async Task<int> UpdatePermissionRoleMappings(CustomerRole role, IFormCollection form)
        {
            await _db.LoadCollectionAsync(role, x => x.PermissionRoleMappings);

            var save = false;
            var permissionKey = "permission-";
            var existingMappings = role.PermissionRoleMappings.ToDictionarySafe(x => x.PermissionRecordId, x => x);

            var mappings = form.Keys.Where(x => x.StartsWith(permissionKey))
                .Select(x =>
                {
                    var id = x[permissionKey.Length..].ToInt();
                    bool? allow = null;
                    var value = form[x].ToString().EmptyNull();
                    if (value.StartsWith("2"))
                    {
                        allow = true;
                    }
                    else if (value.StartsWith("1"))
                    {
                        allow = false;
                    }

                    return new { id, allow };
                })
                .ToDictionary(x => x.id, x => x.allow);

            foreach (var item in mappings)
            {
                if (existingMappings.TryGetValue(item.Key, out var mapping))
                {
                    if (item.Value.HasValue)
                    {
                        if (mapping.Allow != item.Value.Value)
                        {
                            mapping.Allow = item.Value.Value;
                            save = true;
                        }
                    }
                    else
                    {
                        _db.PermissionRoleMappings.Remove(mapping);
                        save = true;
                    }
                }
                else if (item.Value.HasValue)
                {
                    _db.PermissionRoleMappings.Add(new PermissionRoleMapping
                    {
                        Allow = item.Value.Value,
                        PermissionRecordId = item.Key,
                        CustomerRoleId = role.Id
                    });
                    save = true;
                }
            }

            if (save)
            {
                return await _db.SaveChangesAsync();
            }

            return 0;
        }
    }
}
