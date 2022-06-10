using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Rules
{
    [LocalizedDisplay("Admin.Rules.RuleSet.Fields.")]
    public partial class RuleSetModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Description")]
        [UIHint("Textarea")]
        public string Description { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*Scope")]
        public RuleScope Scope { get; set; }

        [LocalizedDisplay("*Scope")]
        public string ScopeName { get; set; }

        [LocalizedDisplay("*IsSubGroup")]
        public bool IsSubGroup { get; set; }

        [LocalizedDisplay("*LogicalOperator")]
        public LogicalRuleOperator LogicalOperator { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public DateTime? LastProcessedOnUtc { get; set; }

        public IRuleExpressionGroup ExpressionGroup { get; set; }
        public string RawRuleData { get; set; }
        public string EditUrl { get; set; }
    }

    public class RuleSetAssignedToEntityModel : EntityModelBase
    {
        public string Name { get; set; }
        public string SystemName { get; set; }
    }

    public partial class RuleSetValidator : AbstractValidator<RuleSetModel>
    {
        public RuleSetValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(400);
        }
    }
}
