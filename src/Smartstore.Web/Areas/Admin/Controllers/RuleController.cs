using System.Globalization;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Admin.Models.Customers;
using Smartstore.Admin.Models.Rules;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class RuleController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IRuleService _ruleService;
        private readonly IRuleProviderFactory _ruleProviderFactory;
        private readonly IEnumerable<IRuleOptionsProvider> _ruleOptionsProviders;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CustomerSettings _customerSettings;

        public RuleController(
            SmartDbContext db,
            IRuleService ruleService,
            IRuleProviderFactory ruleProviderFactory,
            IEnumerable<IRuleOptionsProvider> ruleOptionsProviders,
            Lazy<IPaymentService> paymentService,
            Lazy<ModuleManager> moduleManager,
            AdminAreaSettings adminAreaSettings,
            CustomerSettings customerSettings)
        {
            _db = db;
            _ruleService = ruleService;
            _ruleProviderFactory = ruleProviderFactory;
            _ruleOptionsProviders = ruleOptionsProviders;
            _paymentService = paymentService;
            _moduleManager = moduleManager;
            _adminAreaSettings = adminAreaSettings;
            _customerSettings = customerSettings;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available rule sets. 
        /// </summary>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="scope">Specifies the <see cref="RuleScope"/>.</param>
        /// <returns>List of all rule sets as JSON.</returns>
        public async Task<IActionResult> AllRuleSets(string selectedIds, RuleScope? scope)
        {
            var ruleSets = await _db.RuleSets
                .AsNoTracking()
                .ApplyStandardFilter(scope, false, true)
                .ToListAsync();

            var selectedArr = selectedIds.ToIntArray();

            ruleSets.Add(new RuleSetEntity { Id = -1, Name = T("Admin.Rules.AddRule").Value + "…" });

            var data = ruleSets
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = selectedArr.Contains(x.Id),
                    UrlTitle = x.Id == -1 ? string.Empty : T("Admin.Rules.OpenRule").Value,
                    Url = x.Id == -1
                        ? Url.Action("Create", "Rule", new { scope, area = "Admin" })
                        : Url.Action("Edit", "Rule", new { id = x.Id, area = "Admin" })
                })
                .ToList();

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Rule.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.System.Rule.Read)]
        public async Task<IActionResult> RuleSetList(GridCommand command)
        {
            var ruleSets = await _db.RuleSets
                .AsNoTracking()
                .ApplyStandardFilter(null, false, true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = ruleSets.Select(x =>
            {
                var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(x);
                model.ScopeName = Services.Localization.GetLocalizedEnum(x.Scope);
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                model.EditUrl = Url.Action(nameof(Edit), "Rule", new { id = x.Id, area = "Admin" });

                return model;
            })
            .ToList();

            var gridModel = new GridModel<RuleSetModel>
            {
                Rows = rows,
                Total = await ruleSets.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Delete)]
        public async Task<IActionResult> RuleSetDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var ruleSets = await _db.RuleSets.GetManyAsync(ids, true);
                _db.RuleSets.RemoveRange(ruleSets);

                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success });
        }

        [Permission(Permissions.System.Rule.Create)]
        public async Task<IActionResult> Create(RuleScope? scope)
        {
            var model = new RuleSetModel();

            await PrepareModel(model, null, scope);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.System.Rule.Create)]
        public async Task<IActionResult> Create(RuleSetModel model, bool continueEditing)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ruleSet = MiniMapper.Map<RuleSetModel, RuleSetEntity>(model);
            _db.Add(ruleSet);

            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Rules.RuleSet.Added"));

            return continueEditing
                ? RedirectToAction(nameof(Edit), new { id = ruleSet.Id })
                : RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Rule.Read)]
        public async Task<IActionResult> Edit(int id /* ruleSetId */)
        {
            var ruleSet = await _db.RuleSets
                .Include(x => x.Rules)
                .FindByIdAsync(id);

            if (ruleSet == null)
            {
                return NotFound();
            }

            var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(ruleSet);

            await PrepareModel(model, ruleSet);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.System.Rule.Update)]
        public async Task<IActionResult> Edit(RuleSetModel model, bool continueEditing)
        {
            var ruleSet = await _db.RuleSets
                .Include(x => x.Rules)
                .FindByIdAsync(model.Id);

            if (ruleSet == null)
            {
                return NotFound();
            }

            MiniMapper.Map(model, ruleSet);

            if (model.RawRuleData.HasValue())
            {
                try
                {
                    var ruleData = JsonConvert.DeserializeObject<RuleEditItem[]>(model.RawRuleData);
                    var provider = GetProvider(ruleSet);

                    await _ruleService.ApplyRuleDataAsync(ruleData, provider);
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            await _db.SaveChangesAsync();

            return continueEditing
                ? RedirectToAction(nameof(Edit), new { id = ruleSet.Id })
                : RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var ruleSet = await _db.RuleSets.FindByIdAsync(id);
            if (ruleSet == null)
            {
                return NotFound();
            }

            _db.RuleSets.Remove(ruleSet);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Rules.RuleSet.Deleted"));

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Rule.Execute)]
        public async Task<IActionResult> Preview(int id)
        {
            var ruleSet = await _db.RuleSets
                .Include(x => x.Rules)
                .FindByIdAsync(id);
            if (ruleSet == null || ruleSet.IsSubGroup)
            {
                return NotFound();
            }

            var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(ruleSet);

            ViewData["IsSingleStoreMode"] = Services.StoreContext.IsSingleStoreMode();
            ViewData["UsernamesEnabled"] = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            ViewData["DisplayProductPictures"] = _adminAreaSettings.DisplayProductPictures;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Execute)]
        public async Task<IActionResult> PreviewList(GridCommand command, int id)
        {
            var ruleSet = await _db.RuleSets
                .Include(x => x.Rules)
                .FindByIdAsync(id);
            if (ruleSet == null || ruleSet.IsSubGroup)
            {
                return NotFound();
            }

            if (ruleSet.Scope == RuleScope.Customer)
            {
                var provider = GetProvider(ruleSet) as ITargetGroupService;
                var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as FilterExpression;
                var customers = provider.ProcessFilter([expression], LogicalRuleOperator.And, command.Page - 1, command.PageSize);
                var guestStr = T("Admin.Customers.Guest").Value;

                var model = new GridModel<CustomerModel>
                {
                    Total = customers.TotalCount,
                    Rows = customers.Select(x =>
                    {
                        var customerModel = new CustomerModel
                        {
                            Id = x.Id,
                            Active = x.Active,
                            Email = x.Email.NullEmpty() ?? (x.IsGuest() ? guestStr : string.Empty),
                            Username = x.Username,
                            FullName = x.GetFullName(),
                            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                            LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
                            EditUrl = Url.Action("Edit", "Customer", new { id = x.Id, area = "Admin" })
                        };

                        return customerModel;
                    })
                    .ToList()
                };

                return new JsonResult(model);
            }
            else if (ruleSet.Scope == RuleScope.Product)
            {
                var provider = GetProvider(ruleSet) as IProductRuleProvider;
                var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as SearchFilterExpression;
                var searchResult = await provider.SearchAsync([expression], command.Page - 1, command.PageSize);
                var hits = await searchResult.GetHitsAsync();
                var rows = await hits.MapAsync(Services.MediaService);

                return new JsonResult(new GridModel<ProductOverviewModel>
                {
                    Total = searchResult.TotalHitsCount,
                    Rows = rows
                });
            }

            return new JsonResult(null);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Create)]
        public async Task<IActionResult> AddRule(RuleCommand command)
        {
            await CreateRuleSetIfRequired(command);

            var provider = GetProvider(null, command);
            var descriptors = await provider.GetRuleDescriptorsAsync();
            var descriptor = descriptors.FindDescriptor(command.RuleType);

            var op = (descriptor.RuleType == RuleType.NullableInt || descriptor.RuleType == RuleType.NullableFloat)
                ? descriptor.Operators.FirstOrDefault(x => x == RuleOperator.GreaterThanOrEqualTo)
                : descriptor.Operators.First();

            var rule = new RuleEntity
            {
                RuleSetId = command.RuleSetId,
                RuleType = command.RuleType,
                Operator = op.Operator
            };

            if (descriptor.RuleType == RuleType.Boolean)
            {
                // Do not store NULL. Irritating because UI indicates 'yes'.
                var val = op == RuleOperator.IsEqualTo;
                rule.Value = val.ToString(CultureInfo.InvariantCulture).ToLower();
            }
            else if (op == RuleOperator.In || op == RuleOperator.NotIn)
            {
                // Avoid ArgumentException "The 'In' operator only supports non-null instances from types that implement 'ICollection<T>'."
                rule.Value = string.Empty;
            }

            _db.Rules.Add(rule);
            await _db.SaveChangesAsync();

            var expression = await provider.VisitRuleAsync(rule);

            return PartialView("_Rule", expression);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Update)]
        public async Task<IActionResult> UpdateRules(RuleCommand command)
        {
            try
            {
                var provider = GetProvider(null, command);

                await _ruleService.ApplyRuleDataAsync(command.RuleData, provider);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, ex.Message });
            }

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Delete)]
        public async Task<IActionResult> DeleteRule(RuleCommand command)
        {
            var rule = await _db.Rules.FindByIdAsync(command.RuleId);
            if (rule == null)
            {
                NotifyError(T("Admin.Rules.NotFound", command.RuleId));
                return Json(new { Success = false });
            }

            _db.Rules.Remove(rule);
            await _db.SaveChangesAsync();
            await DeleteRuleSetIfNotRequired(command);

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Update)]
        public async Task<IActionResult> ChangeOperator(RuleCommand command)
        {
            await CreateRuleSetIfRequired(command);

            var andOp = command.Op.EqualsNoCase("and");
            var ruleSet = await _db.RuleSets.FindByIdAsync(command.RuleSetId);
            if (ruleSet == null)
            {
                NotifyError(T("Admin.Rules.GroupNotFound", command.RuleSetId));
                return Json(new { Success = false });
            }

            if (ruleSet.Scope == RuleScope.Product && !andOp)
            {
                NotifyError(T("Admin.Rules.OperatorNotSupported"));
                return Json(new { Success = false });
            }

            ruleSet.LogicalOperator = andOp ? LogicalRuleOperator.And : LogicalRuleOperator.Or;

            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Create)]
        public async Task<IActionResult> AddGroup(RuleCommand command)
        {
            await CreateRuleSetIfRequired(command);

            var provider = GetProvider(null, command);

            var group = new RuleSetEntity
            {
                IsActive = true,
                IsSubGroup = true,
                Scope = command.Scope
            };

            // RuleSet ID required.
            _db.RuleSets.Add(group);
            await _db.SaveChangesAsync();

            var groupRefRule = new RuleEntity
            {
                RuleSetId = command.RuleSetId,
                RuleType = "Group",
                Operator = RuleOperator.IsEqualTo,
                Value = group.Id.ToString()
            };

            _db.Rules.Add(groupRefRule);
            await _db.SaveChangesAsync();

            var expression = provider.VisitRuleSet(group);
            expression.RefRuleId = groupRefRule.Id;

            return PartialView("_RuleSet", expression);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Delete)]
        public async Task<IActionResult> DeleteGroup(RuleCommand command)
        {
            var refRule = await _db.Rules.FindByIdAsync(command.RuleId);
            var ruleSetId = refRule?.Value?.ToInt() ?? 0;

            var group = await _db.RuleSets.FindByIdAsync(ruleSetId);
            if (group == null)
            {
                NotifyError(T("Admin.Rules.GroupNotFound", ruleSetId));
                return Json(new { Success = false });
            }

            // INFO: "refRule" must be deleted manually (a foreign key relationship does not exist).
            // Descendant sub rule sets are deleted by RuleSetHook.
            _db.Rules.Remove(refRule);
            _db.RuleSets.Remove(group);

            await _db.SaveChangesAsync();
            await DeleteRuleSetIfNotRequired(command);

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Execute)]
        public async Task<IActionResult> Execute(RuleCommand command)
        {
            var success = true;
            var message = string.Empty;

            try
            {
                var ruleSet = await _db.RuleSets
                    .Include(x => x.Rules)
                    .FindByIdAsync(command.RuleSetId);

                switch (ruleSet.Scope)
                {
                    case RuleScope.Cart:
                    {
                        var customer = Services.WorkContext.CurrentCustomer;
                        var provider = GetProvider(ruleSet) as ICartRuleProvider;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as RuleExpression;
                        var match = await provider.RuleMatchesAsync([expression], LogicalRuleOperator.And);

                        message = T(match ? "Admin.Rules.Execute.MatchCart" : "Admin.Rules.Execute.DoesNotMatchCart", customer.Username.NullEmpty() ?? customer.Email);
                    }
                    break;
                    case RuleScope.Customer:
                    {
                        var provider = GetProvider(ruleSet) as ITargetGroupService;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as FilterExpression;
                        var result = provider.ProcessFilter([expression], LogicalRuleOperator.And, 0, 1);

                        message = T("Admin.Rules.Execute.MatchCustomers", result.TotalCount.ToString("N0"));
                    }
                    break;
                    case RuleScope.Product:
                    {
                        var provider = GetProvider(ruleSet) as IProductRuleProvider;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as SearchFilterExpression;
                        var result = await provider.SearchAsync([expression], 0, 1);

                        message = T("Admin.Rules.Execute.MatchProducts", result.TotalHitsCount.ToString("N0"));
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                Logger.Error(ex);
            }

            return Json(new
            {
                Success = success,
                Message = message.NaIfEmpty()
            });
        }

        /// <summary>
        /// (AJAX) Gets rule options for a rule entity.
        /// </summary>
        public async Task<IActionResult> RuleOptions(
            RuleOptionsRequestReason reason,
            int ruleId,
            int rootRuleSetId,
            string term,
            int? page,
            string descriptorMetadataKey,
            string rawValue)
        {
            var rule = await _db.Rules.Include(x => x.RuleSet).FindByIdAsync(ruleId, false) ?? throw new Exception(T("Admin.Rules.NotFound", ruleId));
            var provider = GetProvider(rule.RuleSet);
            var expression = await provider.VisitRuleAsync(rule);

            Func<RuleValueSelectListOption, bool> optionsPredicate = x => true;
            RuleOptionsResult options = null;
            RuleDescriptor descriptor = null;

            if (descriptorMetadataKey.HasValue() && expression.Descriptor.Metadata.TryGetValue(descriptorMetadataKey, out var obj))
            {
                descriptor = obj as RuleDescriptor;
            }
            if (descriptor == null)
            {
                descriptor = expression.Descriptor;
                rawValue = expression.RawValue;
            }

            if (descriptor.SelectList is RemoteRuleValueSelectList list)
            {
                var optionsProvider = _ruleOptionsProviders.FirstOrDefault(x => x.Matches(list.DataSource));
                if (optionsProvider != null)
                {
                    options = await optionsProvider.GetOptionsAsync(new RuleOptionsContext(reason, descriptor)
                    {
                        Value = rawValue,
                        PageIndex = page ?? 0,
                        PageSize = 100,
                        SearchTerm = term,
                        Language = Services.WorkContext.WorkingLanguage
                    });

                    if (rule.RuleSet.IsSubGroup)
                    {
                        // The root rule set must not be selected as a subgroup.
                        optionsPredicate = x => x.Value != rootRuleSetId.ToString();
                    }
                }
            }

            options ??= new RuleOptionsResult();

            var data = options.Options
                .Where(optionsPredicate)
                .Select(x => new RuleSelectItem { Id = x.Value, Text = x.Text, Hint = x.Hint })
                .ToList();

            // Mark selected items.
            var selectedValues = rawValue.SplitSafe(',');

            data.Each(x => x.Selected = selectedValues.Contains(x.Id));

            return new JsonResult(new
            {
                hasMoreData = options.HasMoreData,
                results = data
            });
        }

        private async Task CreateRuleSetIfRequired(RuleCommand command)
        {
            if (command.Scope == RuleScope.ProductAttribute)
            {
                var attributeId = Guard.NotZero(command.EntityId ?? 0);

                if (!await _db.RuleSets.AnyAsync(x => x.ProductVariantAttributeId == attributeId && x.Scope == RuleScope.ProductAttribute && !x.IsSubGroup))
                {
                    var ruleSet = new RuleSetEntity
                    {
                        Scope = RuleScope.ProductAttribute,
                        IsActive = true,
                        IsSubGroup = false,
                        LogicalOperator = LogicalRuleOperator.And,
                        ProductVariantAttributeId = attributeId
                    };

                    _db.Add(ruleSet);
                    await _db.SaveChangesAsync();
                    
                    command.RuleSetId = ruleSet.Id;
                }
            }
        }

        private async Task DeleteRuleSetIfNotRequired(RuleCommand command)
        {
            if (command.Scope == RuleScope.ProductAttribute)
            {
                var attributeId = Guard.NotZero(command.EntityId ?? 0);
                var existingRuleSet = await _db.RuleSets
                    .FirstOrDefaultAsync(x => x.ProductVariantAttributeId == attributeId && x.Scope == RuleScope.ProductAttribute && !x.IsSubGroup && x.Rules.Count == 0);

                if (existingRuleSet != null)
                {
                    _db.RuleSets.Remove(existingRuleSet);
                    await _db.SaveChangesAsync();

                    command.RuleSetId = 0;
                }
            }
        }

        private async Task PrepareModel(RuleSetModel model, RuleSetEntity ruleSet, RuleScope? scope = null)
        {
            var scopes = (ruleSet?.Scope ?? scope ?? RuleScope.Cart).ToSelectList();

            ViewBag.Scopes = scopes
                .Select(x =>
                {
                    var ruleScope = (RuleScope)x.Value.ToInt();
                    if (ruleScope >= RuleScope.ProductAttribute)
                    {
                        return null;
                    }

                    var item = new ExtendedSelectListItem
                    {
                        Value = x.Value,
                        Text = x.Text,
                        Selected = x.Selected
                    };

                    item.CustomProperties["Description"] = Services.Localization.GetLocalizedEnum(ruleScope, 0, true);

                    return item;
                })
                .Where(x => x != null)
                .ToList();

            if (ruleSet != null && ruleSet.Id != 0)
            {
                var provider = GetProvider(ruleSet);

                model.ScopeName = Services.Localization.GetLocalizedEnum(ruleSet.Scope);
                model.ExpressionGroup = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true);

                await _ruleService.ApplyMetadataAsync(model.ExpressionGroup);

                ViewBag.AssignedToDiscounts = ruleSet.Discounts
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.Name.NullEmpty() ?? x.Id.ToString() })
                    .ToList();

                ViewBag.AssignedToShippingMethods = ruleSet.ShippingMethods
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name) })
                    .ToList();

                ViewBag.AssignedToCustomerRoles = ruleSet.CustomerRoles
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.Name })
                    .ToList();

                ViewBag.AssignedToCategories = ruleSet.Categories
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name) })
                    .ToList();

                var paymentMethods = ruleSet.PaymentMethods;
                if (paymentMethods.Count != 0)
                {
                    var paymentProviders = (await _paymentService.Value.LoadAllPaymentProvidersAsync()).ToDictionarySafe(x => x.Metadata.SystemName);

                    ViewBag.AssignedToPaymentMethods = paymentMethods
                        .Select(x =>
                        {
                            var friendlyName = paymentProviders.TryGetValue(x.PaymentMethodSystemName, out var paymentProvider)
                                ? _moduleManager.Value.GetLocalizedFriendlyName(paymentProvider.Metadata)
                                : null;

                            return new RuleSetAssignedToEntityModel
                            {
                                Id = x.Id,
                                Name = friendlyName.NullEmpty() ?? x.PaymentMethodSystemName,
                                SystemName = x.PaymentMethodSystemName
                            };
                        })
                        .ToList();
                }
            }
        }

        private IRuleProvider GetProvider(RuleSetEntity ruleSet, RuleCommand command = null)
        {
            var scope = ruleSet?.Scope ?? command?.Scope ?? RuleScope.Cart;

            if (scope == RuleScope.ProductAttribute)
            {
                return _ruleProviderFactory.GetProvider(scope, new AttributeRuleProviderContext(ruleSet?.ProductVariantAttributeId ?? command?.EntityId ?? 0));
            }

            return _ruleProviderFactory.GetProvider(scope);
        }
    }
}
