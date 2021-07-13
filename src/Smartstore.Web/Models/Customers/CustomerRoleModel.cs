using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.CustomerRoles.Fields.")]
    public class CustomerRoleModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*FreeShipping")]
        public bool FreeShipping { get; set; }

        [LocalizedDisplay("*TaxExempt")]
        public bool TaxExempt { get; set; }

        [LocalizedDisplay("*TaxDisplayType")]
        public int? TaxDisplayType { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*IsSystemRole")]
        public bool IsSystemRole { get; set; }

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("*MinOrderTotal")]
        public decimal? OrderTotalMinimum { get; set; }

        [LocalizedDisplay("*MaxOrderTotal")]
        public decimal? OrderTotalMaximum { get; set; }

        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Customer)]
        [LocalizedDisplay("Admin.Customers.CustomerRoles.AutomatedAssignmentRules")]
        public int[] SelectedRuleSetIds { get; set; }

        public string EditUrl { get; set; }
    }

    public partial class CustomerRoleValidator : AbstractValidator<CustomerRoleModel>
    {
        public CustomerRoleValidator(Localizer _)
        {
            RuleFor(x => x.Name).NotNull();

            RuleFor(x => x.OrderTotalMaximum)
                .GreaterThan(x => x.OrderTotalMinimum ?? 0);
        }
    }

    public class CustomerRoleMapper : IMapper<CustomerRole, CustomerRoleModel>, IMapper<CustomerRoleModel, CustomerRole>
    {
        private readonly IRuleService _ruleService;
        private readonly IUrlHelper _urlHelper;

        public CustomerRoleMapper(IRuleService ruleService, IUrlHelper urlHelper)
        {
            _ruleService = ruleService;
            _urlHelper = urlHelper;
        }

        public Task MapAsync(CustomerRole from, CustomerRoleModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            to.SelectedRuleSetIds = from.RuleSets.Select(x => x.Id).ToArray();

            if (from.Id != 0)
            {
                to.EditUrl = _urlHelper.Action("Edit", "CustomerRole", new { id = from.Id, area = "Admin" });
            }

            return Task.CompletedTask;
        }

        public async Task MapAsync(CustomerRoleModel from, CustomerRole to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            // TODO: (mg) (core) test mapping CustomerRoleModel > CustomerRole.
            if (from.SelectedRuleSetIds?.Any() ?? false)
            {
                await _ruleService.ApplyRuleSetMappingsAsync(to, from.SelectedRuleSetIds);
            }
        }
    }
}
