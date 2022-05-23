using FluentValidation.Internal;
using Smartstore;
using Smartstore.Core.Configuration;

namespace FluentValidation
{
    public static class FluentValidatorRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> CreditCardCvvNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Matches(RegularExpressions.IsCvv);
        }

        public static IRuleBuilderOptions<T, TProperty> WhenOverrideChecked<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, 
            Func<T, TProperty, bool> predicate, 
            ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
        {
            return rule.Configure(config =>
            {
                config.ApplyCondition(
                    ctx =>
                    {
                        var builder = rule as RuleBuilder<T, TProperty>;
                        var validator = builder.ParentValidator as ISettingModelValidator;
                        Guard.NotNull(validator, $"The validator must be inherited from {nameof(SettingModelValidator<T, ISettings>)} when using {nameof(WhenOverrideChecked)}.");

                        var condition = (validator.StoreScope == 0 && predicate((T)ctx.InstanceToValidate, (TProperty)ctx.PropertyValue))
                            || (validator.StoreScope > 0 && validator.IsOverrideChecked(ctx.PropertyName));

                        return condition;
                    },
                    applyConditionTo);
            });
        }
    }
}
