using System.Globalization;
using System.Linq.Dynamic.Core;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Admin.Models.Customers;
using Smartstore.Admin.Models.Rules;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
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
        private readonly IRuleTemplateSelector _ruleTemplateSelector;
        private readonly Func<RuleScope, IRuleProvider> _ruleProvider;
        private readonly IEnumerable<IRuleOptionsProvider> _ruleOptionsProviders;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly MediaSettings _mediaSettings;

        public RuleController(
            SmartDbContext db,
            IRuleService ruleService,
            IRuleTemplateSelector ruleTemplateSelector,
            Func<RuleScope, IRuleProvider> ruleProvider,
            IEnumerable<IRuleOptionsProvider> ruleOptionsProviders,
            Lazy<IPaymentService> paymentService,
            Lazy<ModuleManager> moduleManager,
            AdminAreaSettings adminAreaSettings,
            CustomerSettings customerSettings,
            MediaSettings mediaSettings)
        {
            _db = db;
            _ruleService = ruleService;
            _ruleTemplateSelector = ruleTemplateSelector;
            _ruleProvider = ruleProvider;
            _ruleOptionsProviders = ruleOptionsProviders;
            _paymentService = paymentService;
            _moduleManager = moduleManager;
            _adminAreaSettings = adminAreaSettings;
            _customerSettings = customerSettings;
            _mediaSettings = mediaSettings;
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

            var rows = await ruleSets.SelectAwait(async x =>
            {
                var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(x);
                model.ScopeName = await Services.Localization.GetLocalizedEnumAsync(x.Scope);
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                model.EditUrl = Url.Action(nameof(Edit), "Rule", new { id = x.Id, area = "Admin" });

                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<RuleSetModel>
            {
                Rows = rows,
                Total = await ruleSets.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(Permissions.System.Rule.Create)]
        public async Task<IActionResult> Create(RuleScope? scope)
        {
            var model = new RuleSetModel();

            await PrepareModel(model, null, scope);
            PrepareTemplateViewBag(0);

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
                .AsNoTracking()
                .Include(x => x.Rules)
                .FindByIdAsync(id);

            if (ruleSet == null)
            {
                return NotFound();
            }

            var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(ruleSet);

            await PrepareModel(model, ruleSet);
            await PrepareExpressions(model.ExpressionGroup);
            PrepareTemplateViewBag(ruleSet.Id);

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

                    await ApplyRuleData(ruleData, model.Scope);
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
                var provider = _ruleProvider(ruleSet.Scope) as ITargetGroupService;
                var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as FilterExpression;
                var customers = provider.ProcessFilter(new[] { expression }, LogicalRuleOperator.And, command.Page - 1, command.PageSize);
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
                var provider = _ruleProvider(ruleSet.Scope) as IProductRuleProvider;
                var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as SearchFilterExpression;
                var searchResult = await provider.SearchAsync(new[] { expression }, command.Page - 1, command.PageSize);
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
        public async Task<IActionResult> AddRule(int ruleSetId, RuleScope scope, string ruleType)
        {
            var provider = _ruleProvider(scope);
            var descriptors = await provider.GetRuleDescriptorsAsync();
            var descriptor = descriptors.FindDescriptor(ruleType);

            RuleOperator op;
            if (descriptor.RuleType == RuleType.NullableInt || descriptor.RuleType == RuleType.NullableFloat)
            {
                op = descriptor.Operators.FirstOrDefault(x => x == RuleOperator.GreaterThanOrEqualTo);
            }
            else
            {
                op = descriptor.Operators.First();
            }

            var rule = new RuleEntity
            {
                RuleSetId = ruleSetId,
                RuleType = ruleType,
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

            PrepareTemplateViewBag(ruleSetId);

            return PartialView("_Rule", expression);
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Update)]
        public async Task<IActionResult> UpdateRules(RuleEditItem[] ruleData, RuleScope ruleScope)
        {
            try
            {
                await ApplyRuleData(ruleData, ruleScope);
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
        public async Task<IActionResult> DeleteRule(int ruleId)
        {
            var rule = await _db.Rules.FindByIdAsync(ruleId);
            if (rule == null)
            {
                NotifyError(T("Admin.Rules.NotFound", ruleId));
                return Json(new { Success = false });
            }

            _db.Rules.Remove(rule);
            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Update)]
        public async Task<IActionResult> ChangeOperator(int ruleSetId, string op)
        {
            var andOp = op.EqualsNoCase("and");
            var ruleSet = await _db.RuleSets.FindByIdAsync(ruleSetId);
            if (ruleSet == null)
            {
                NotifyError(T("Admin.Rules.GroupNotFound", ruleSetId));
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
        public async Task<IActionResult> AddGroup(int ruleSetId, RuleScope scope)
        {
            var provider = _ruleProvider(scope);

            var group = new RuleSetEntity
            {
                IsActive = true,
                IsSubGroup = true,
                Scope = scope
            };

            // RuleSet ID required.
            _db.RuleSets.Add(group);
            await _db.SaveChangesAsync();

            var groupRefRule = new RuleEntity
            {
                RuleSetId = ruleSetId,
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
        public async Task<IActionResult> DeleteGroup(int refRuleId)
        {
            var refRule = await _db.Rules.FindByIdAsync(refRuleId);
            var ruleSetId = refRule?.Value?.ToInt() ?? 0;

            var group = ruleSetId != 0
                ? await _db.RuleSets.FindByIdAsync(ruleSetId)
                : null;
            if (group == null)
            {
                NotifyError(T("Admin.Rules.GroupNotFound", ruleSetId));
                return Json(new { Success = false });
            }

            _db.Rules.Remove(refRule);
            _db.RuleSets.Remove(group);

            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.System.Rule.Execute)]
        public async Task<IActionResult> Execute(int ruleSetId)
        {
            var success = true;
            var message = string.Empty;

            try
            {
                var ruleSet = await _db.RuleSets
                    .Include(x => x.Rules)
                    .FindByIdAsync(ruleSetId);

                switch (ruleSet.Scope)
                {
                    case RuleScope.Cart:
                    {
                        var customer = Services.WorkContext.CurrentCustomer;
                        var provider = _ruleProvider(ruleSet.Scope) as ICartRuleProvider;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as RuleExpression;
                        var match = await provider.RuleMatchesAsync(new[] { expression }, LogicalRuleOperator.And);

                        message = T(match ? "Admin.Rules.Execute.MatchCart" : "Admin.Rules.Execute.DoesNotMatchCart", customer.Username.NullEmpty() ?? customer.Email);
                    }
                    break;
                    case RuleScope.Customer:
                    {
                        var provider = _ruleProvider(ruleSet.Scope) as ITargetGroupService;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as FilterExpression;
                        var result = provider.ProcessFilter(new[] { expression }, LogicalRuleOperator.And, 0, 1);

                        message = T("Admin.Rules.Execute.MatchCustomers", result.TotalCount.ToString("N0"));
                    }
                    break;
                    case RuleScope.Product:
                    {
                        var provider = _ruleProvider(ruleSet.Scope) as IProductRuleProvider;
                        var expression = await _ruleService.CreateExpressionGroupAsync(ruleSet, provider, true) as SearchFilterExpression;
                        var result = await provider.SearchAsync(new[] { expression }, 0, 1);

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
            var rule = await _db.Rules.FindByIdAsync(ruleId);
            if (rule == null)
            {
                throw new Exception(T("Admin.Rules.NotFound", ruleId));
            }

            var provider = _ruleProvider(rule.RuleSet.Scope);
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

                    if (list.DataSource == "CartRule" || list.DataSource == "TargetGroup")
                    {
                        optionsPredicate = x => x.Value != rootRuleSetId.ToString();
                    }
                }
            }

            if (options == null)
            {
                options = new RuleOptionsResult();
            }

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

        private async Task ApplyRuleData(RuleEditItem[] ruleData, RuleScope ruleScope)
        {
            var ruleIds = ruleData?.Select(x => x.RuleId)?.Distinct()?.ToArray() ?? Array.Empty<int>();

            if (!ruleIds.Any())
            {
                return;
            }

            var rules = await _db.Rules
                .Include(x => x.RuleSet)
                .Where(x => ruleIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var provider = _ruleProvider(ruleScope);
            var descriptors = await provider.GetRuleDescriptorsAsync();

            foreach (var data in ruleData)
            {
                if (rules.TryGetValue(data.RuleId, out var entity))
                {
                    if (data.Value.HasValue())
                    {
                        var descriptor = descriptors.FindDescriptor(entity.RuleType);

                        if (data.Op == RuleOperator.IsEmpty || data.Op == RuleOperator.IsNotEmpty ||
                            data.Op == RuleOperator.IsNull || data.Op == RuleOperator.IsNotNull)
                        {
                            data.Value = null;
                        }
                        else if (descriptor.RuleType == RuleType.DateTime || descriptor.RuleType == RuleType.NullableDateTime)
                        {
                            // Always store invariant formatted UTC values, otherwise database queries return inaccurate results.
                            var dt = data.Value.Convert<DateTime>(CultureInfo.CurrentCulture).ToUniversalTime();
                            data.Value = dt.ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    entity.Operator = data.Op;
                    entity.Value = data.Value;
                }
            }
        }

        private async Task PrepareModel(RuleSetModel model, RuleSetEntity entity, RuleScope? scope = null)
        {
            var scopes = (entity?.Scope ?? scope ?? RuleScope.Cart).ToSelectList();

            ViewBag.Scopes = await scopes
                .SelectAwait(async x =>
                {
                    var item = new ExtendedSelectListItem
                    {
                        Value = x.Value,
                        Text = x.Text,
                        Selected = x.Selected
                    };

                    var ruleScope = (RuleScope)x.Value.ToInt();
                    item.CustomProperties["Description"] = await Services.Localization.GetLocalizedEnumAsync(ruleScope, 0, true);

                    return item;
                })
                .AsyncToList();

            if ((entity?.Id ?? 0) != 0)
            {
                var provider = _ruleProvider(entity.Scope);

                model.ScopeName = await Services.Localization.GetLocalizedEnumAsync(entity.Scope);
                model.ExpressionGroup = await _ruleService.CreateExpressionGroupAsync(entity, provider, true);

                ViewBag.AssignedToDiscounts = entity.Discounts
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.Name.NullEmpty() ?? x.Id.ToString() })
                    .ToList();

                ViewBag.AssignedToShippingMethods = entity.ShippingMethods
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name) })
                    .ToList();

                ViewBag.AssignedToCustomerRoles = entity.CustomerRoles
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.Name })
                    .ToList();

                ViewBag.AssignedToCategories = entity.Categories
                    .Select(x => new RuleSetAssignedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name) })
                    .ToList();

                var paymentMethods = entity.PaymentMethods;
                if (paymentMethods.Any())
                {
                    var paymentProviders = (await _paymentService.Value.LoadAllPaymentMethodsAsync()).ToDictionarySafe(x => x.Metadata.SystemName);

                    ViewBag.AssignedToPaymentMethods = paymentMethods
                        .Select(x =>
                        {
                            string friendlyName = null;
                            if (paymentProviders.TryGetValue(x.PaymentMethodSystemName, out var paymentProvider))
                            {
                                friendlyName = _moduleManager.Value.GetLocalizedFriendlyName(paymentProvider.Metadata);
                            }

                            return new RuleSetAssignedToEntityModel
                            {
                                Id = x.Id,
                                Name = friendlyName.NullEmpty() ?? x.PaymentMethodSystemName,
                                SystemName = x.PaymentMethodSystemName
                            };
                        })
                        .ToList();
                }

                ViewBag.ConditionsOrButton = await InvokePartialViewAsync("_RuleSetConditionsButton", model, new { ruleOperator = LogicalRuleOperator.Or });
                ViewBag.ConditionsAndButton = await InvokePartialViewAsync("_RuleSetConditionsButton", model, new { ruleOperator = LogicalRuleOperator.And });
            }
        }

        private async Task PrepareExpressions(IRuleExpressionGroup group)
        {
            if (group == null)
            {
                return;
            }

            foreach (var expression in group.Expressions)
            {
                if (expression is IRuleExpressionGroup subGroup)
                {
                    await PrepareExpressions(subGroup);
                    continue;
                }

                if (!expression.Descriptor.IsValid)
                {
                    expression.Metadata["Error"] = T("Admin.Rules.InvalidDescriptor").Value;
                }

                // Load name and subtitle (e.g. SKU) for selected options.
                if (expression.Descriptor.SelectList is RemoteRuleValueSelectList list)
                {
                    var optionsProvider = _ruleOptionsProviders.FirstOrDefault(x => x.Matches(list.DataSource));
                    if (optionsProvider != null)
                    {
                        var options = await optionsProvider.GetOptionsAsync(new RuleOptionsContext(RuleOptionsRequestReason.SelectedDisplayNames, expression)
                        {
                            PageSize = int.MaxValue,
                            Language = Services.WorkContext.WorkingLanguage
                        });

                        expression.Metadata["SelectedItems"] = options.Options.ToDictionarySafe(
                            x => x.Value,
                            x => new RuleSelectItem { Text = x.Text, Hint = x.Hint });
                    }
                }
            }
        }

        private void PrepareTemplateViewBag(int rootRuleSetId)
        {
            ViewBag.RootRuleSetId = rootRuleSetId;
            ViewBag.TemplateSelector = _ruleTemplateSelector;
        }
    }
}
