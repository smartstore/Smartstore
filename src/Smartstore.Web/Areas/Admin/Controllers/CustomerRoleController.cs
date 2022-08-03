using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Smartstore.Admin.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Scheduling;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class CustomerRoleController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly RoleManager<CustomerRole> _roleManager;
        private readonly IRuleService _ruleService;
        private readonly ICurrencyService _currencyService;
        private readonly Lazy<ITaskStore> _taskStore;
        private readonly Lazy<ITaskScheduler> _taskScheduler;
        private readonly CustomerSettings _customerSettings;

        public CustomerRoleController(
            SmartDbContext db,
            RoleManager<CustomerRole> roleManager,
            IRuleService ruleService,
            ICurrencyService currencyService,
            Lazy<ITaskStore> taskStore,
            Lazy<ITaskScheduler> taskScheduler,
            CustomerSettings customerSettings)
        {
            _db = db;
            _roleManager = roleManager;
            _taskStore = taskStore;
            _ruleService = ruleService;
            _currencyService = currencyService;
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
            var query = _roleManager.Roles.AsNoTracking();

            if (!(includeSystemRoles ?? true))
            {
                query = query.Where(x => !x.IsSystemRole);
            }

            query = query.ApplyStandardFilter(true);

            var rolesPager = new FastPager<CustomerRole>(query, 1000);
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
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Customer.Role.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> RoleList(GridCommand command)
        {
            var customerRoles = await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = customerRoles.Select(x =>
            {
                var model = MiniMapper.Map<CustomerRole, CustomerRoleModel>(x);
                model.EditUrl = Url.Action(nameof(Edit), "CustomerRole", new { id = x.Id, area = "Admin" });
                return model;
            })
            .ToList();

            return Json(new GridModel<CustomerRoleModel>
            {
                Rows = rows,
                Total = customerRoles.TotalCount
            });
        }

        [Permission(Permissions.Customer.Role.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerRoleModel
            {
                Active = true
            };

            await PrepareRoleModel(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Customer.Role.Create)]
        public async Task<IActionResult> Create(CustomerRoleModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var role = MiniMapper.Map<CustomerRoleModel, CustomerRole>(model);
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewCustomerRole, T("ActivityLog.AddNewCustomerRole"), role.Name);
                    NotifySuccess(T("Admin.Customers.CustomerRoles.Added"));

                    return continueEditing
                        ? RedirectToAction(nameof(Edit), new { id = role.Id })
                        : RedirectToAction(nameof(List));
                }
                else
                {
                    AddModelErrors(result, string.Empty);
                }
            }

            await PrepareRoleModel(model, null);

            return View(model);
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            var model = MiniMapper.Map<CustomerRole, CustomerRoleModel>(role);

            await PrepareRoleModel(model, role);

            return View(model);
        }

        public async Task<IActionResult> RoleUpdate(CustomerRoleModel model)
        {
            var success = false;

            if (await Services.Permissions.AuthorizeAsync(Permissions.Customer.Role.Update))
            {
                var role = await _roleManager.FindByIdAsync(model.Id.ToString());
                if (role != null)
                {
                    Validate(model, role);

                    if (ModelState.IsValid)
                    {
                        MiniMapper.Map(model, role);

                        var result = await _roleManager.UpdateAsync(role);
                        success = result.Succeeded;

                        if (!result.Succeeded)
                        {
                            AddModelErrors(result, string.Empty);
                        }
                    }
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, await Services.Permissions.GetUnauthorizedMessageAsync(Permissions.Customer.Role.Update));
            }

            if (!ModelState.IsValid)
            {
                ModelState.Values.SelectMany(x => x.Errors).Each(x => NotifyError(x.ErrorMessage));
            }

            return Json(new { success });
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Customer.Role.Update)]
        public async Task<IActionResult> Edit(CustomerRoleModel model, bool continueEditing, IFormCollection form)
        {
            var role = await _roleManager.FindByIdAsync(model.Id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            await _db.LoadCollectionAsync(role, x => x.PermissionRoleMappings, true);

            Validate(model, role);

            if (ModelState.IsValid)
            {
                try
                {
                    MiniMapper.Map(model, role);
                    await _ruleService.ApplyRuleSetMappingsAsync(role, model.SelectedRuleSetIds);

                    // INFO: cached permission tree removed by PermissionRoleMappingHook.
                    ApplyPermissionRoleMappings(role, form);

                    var result = await _roleManager.UpdateAsync(role);
                    if (result.Succeeded)
                    {
                        Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditCustomerRole, T("ActivityLog.EditCustomerRole"), role.Name);
                        NotifySuccess(T("Admin.Customers.CustomerRoles.Updated"));

                        return continueEditing
                            ? RedirectToAction(nameof(Edit), new { id = role.Id })
                            : RedirectToAction(nameof(List));
                    }
                    else
                    {
                        AddModelErrors(result, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            await PrepareRoleModel(model, role);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteCustomerRole, T("ActivityLog.DeleteCustomerRole"), role.Name);
                    NotifySuccess(T("Admin.Customers.CustomerRoles.Deleted"));

                    return RedirectToAction(nameof(List));
                }
                else
                {
                    result.Errors.Select(x => x.Description).Distinct().Each(x => NotifyError(x));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
            }

            return RedirectToAction(nameof(Edit), new { id = role.Id });
        }

        [Permission(Permissions.Customer.Role.Read)]
        public async Task<IActionResult> CustomerRoleMappingList(GridCommand command, int id)
        {
            var query = _db.CustomerRoleMappings
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x => x.CustomerRoleId == id && x.Customer != null)
                .OrderBy(x => x.IsSystemMapping)
                .Select(x => new CustomerRoleMappingModel
                {
                    Id = x.Id,
                    Active = x.Customer.Active,
                    CustomerId = x.CustomerId,
                    Email = x.Customer.Email,
                    Username = x.Customer.Username,
                    FullName = x.Customer.FullName,
                    CreatedOn = x.Customer.CreatedOnUtc,
                    LastActivityDate = x.Customer.LastActivityDateUtc,
                    IsSystemMapping = x.IsSystemMapping
                });

            var rows = await query
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var role = await _roleManager.FindByIdAsync(id.ToString());
            var isGuestRole = role.SystemName.EqualsNoCase(SystemCustomerRoleNames.Guests);
            var emailFallbackStr = isGuestRole ? T("Admin.Customers.Guest").Value : string.Empty;

            foreach (var row in rows)
            {
                row.Email = row.Email.NullEmpty() ?? emailFallbackStr;
                row.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(row.CreatedOn, DateTimeKind.Utc);
                row.LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(row.LastActivityDate, DateTimeKind.Utc);
                row.EditUrl = Url.Action("Edit", "Customer", new { id = row.CustomerId, area = "Admin" });
            }

            var gridModel = new GridModel<CustomerRoleMappingModel>
            {
                Rows = rows,
                Total = rows.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Update)]
        public async Task<IActionResult> ApplyRules(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            var task = await _taskStore.Value.GetTaskByTypeAsync<TargetGroupEvaluatorTask>();
            if (task != null)
            {
                _ = _taskScheduler.Value.RunSingleTaskAsync(task.Id, new Dictionary<string, string>
                {
                    { "CustomerRoleIds", role.Id.ToString() }
                });

                NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress"));
            }
            else
            {
                NotifyError(T("Admin.System.ScheduleTasks.TaskNotFound", nameof(TargetGroupEvaluatorTask)));
            }

            return RedirectToAction(nameof(Edit), new { id = role.Id });
        }

        private async Task PrepareRoleModel(CustomerRoleModel model, CustomerRole role)
        {
            Guard.NotNull(model, nameof(model));

            if (role != null)
            {
                model.SelectedRuleSetIds = role.RuleSets.Select(x => x.Id).ToArray();

                var showRuleApplyButton = model.SelectedRuleSetIds.Any();

                if (!showRuleApplyButton)
                {
                    // Ignore deleted customers.
                    showRuleApplyButton = await _db.CustomerRoleMappings
                        .AsNoTracking()
                        .Include(x => x.Customer)
                        .Where(x => x.CustomerRoleId == role.Id && x.IsSystemMapping && x.Customer != null)
                        .AnyAsync();
                }

                ViewBag.ShowRuleApplyButton = showRuleApplyButton;
                ViewBag.PermissionTree = await Services.Permissions.GetPermissionTreeAsync(role, true);
                ViewBag.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            }

            ViewBag.TaxDisplayTypes = model.TaxDisplayType.HasValue
                ? ((TaxDisplayType)model.TaxDisplayType.Value).ToSelectList()
                : TaxDisplayType.IncludingTax.ToSelectList(false);

            ViewBag.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
        }

        private void ApplyPermissionRoleMappings(CustomerRole role, IFormCollection form)
        {
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
                        }
                    }
                    else
                    {
                        _db.PermissionRoleMappings.Remove(mapping);
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
                }
            }
        }

        private void Validate(CustomerRoleModel model, CustomerRole role)
        {
            if (role.IsSystemRole)
            {
                if (!model.Active)
                {
                    ModelState.AddModelError(nameof(model.Active), T("Admin.Customers.CustomerRoles.Fields.Active.CantEditSystem"));
                }
                if (!role.SystemName.EqualsNoCase(model.SystemName))
                {
                    ModelState.AddModelError(nameof(model.SystemName), T("Admin.Customers.CustomerRoles.Fields.SystemName.CantEditSystem"));
                }
            }
        }

        private void AddModelErrors(IdentityResult result, string key)
        {
            if (!result.Succeeded)
            {
                result.Errors
                    .Select(x => x.Description)
                    .Distinct()
                    .Each(x => ModelState.AddModelError(key, x));
            }
        }
    }
}
