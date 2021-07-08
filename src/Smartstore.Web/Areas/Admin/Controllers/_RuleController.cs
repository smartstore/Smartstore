using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Rule;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class RuleController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IRuleService _ruleService;
        private readonly IRuleTemplateSelector _ruleTemplateSelector;
        private readonly Func<RuleScope, IRuleProvider> _ruleProvider;
        private readonly IEnumerable<IRuleOptionsProvider> _ruleOptionsProviders;
        private readonly Lazy<IPaymentService> _paymentService;

        public RuleController(
            SmartDbContext db,
            IRuleService ruleService,
            IRuleTemplateSelector ruleTemplateSelector,
            Func<RuleScope, IRuleProvider> ruleProvider,
            IEnumerable<IRuleOptionsProvider> ruleOptionsProviders,
            Lazy<IPaymentService> paymentService)
        {
            _db = db;
            _ruleService = ruleService;
            _ruleTemplateSelector = ruleTemplateSelector;
            _ruleProvider = ruleProvider;
            _ruleOptionsProviders = ruleOptionsProviders;
            _paymentService = paymentService;
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
            return RedirectToAction("List");
        }

        [Permission(Permissions.System.Rule.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.System.Rule.Read)]
        public async Task<IActionResult> RuleList(GridCommand command)
        {
            var ruleSets = await _db.RuleSets
                .AsNoTracking()
                .ApplyStandardFilter(null, false, true)
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var rows = await ruleSets.SelectAsync(async x =>
            {
                var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(x);
                model.ScopeName = await Services.Localization.GetLocalizedEnumAsync(x.Scope);
                model.EditUrl = Url.Action("Edit", "Rule", new { id = x.Id, area = "Admin" });

                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<RuleSetModel>
            {
                Rows = rows,
                Total = ruleSets.TotalCount
            };

            return Json(gridModel);
        }

        [Permission(Permissions.System.Rule.Read)]
        public async Task<IActionResult> Edit(int id /* ruleSetId */)
        {
            var entity = await _db.RuleSets
                .AsNoTracking()
                .Include(x => x.Rules)
                .FindByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var model = MiniMapper.Map<RuleSetEntity, RuleSetModel>(entity);

            await PrepareModel(model, entity);
            await PrepareExpressions(model.ExpressionGroup);
            PrepareTemplateViewBag(entity.Id);

            return View(model);
        }

        private async Task PrepareModel(RuleSetModel model, RuleSetEntity entity, RuleScope? scope = null)
        {
            var scopes = (entity?.Scope ?? scope ?? RuleScope.Cart).ToSelectList();

            ViewBag.Scopes = await scopes
                .SelectAsync(async x =>
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
                                // TODO: (mg) (core) PluginMediator required in RuleController.
                                //friendlyName = _pluginMediator.Value.GetLocalizedFriendlyName(paymentProvider.Metadata);
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
                            PageSize = int.MaxValue
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
