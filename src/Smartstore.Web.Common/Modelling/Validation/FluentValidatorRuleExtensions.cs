using FluentValidation.Internal;
using Smartstore;
using Smartstore.Core.Configuration;

namespace FluentValidation
{
    public static class FluentValidatorRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> CreditCardCvvNumber<T>(this IRuleBuilder<T, string> rule)
        {
            return rule.Matches(RegularExpressions.IsCvv);
        }

        /// <summary>
        /// Specifies a condition limiting when the validator should run. 
        /// The validator will only be executed if...
        /// <list type="bullet">
        ///     <item>In case of store-agnostic setting mode: the result of the <paramref name="predicate"/> parameter returns true, OR</item>
        ///     <item>In case of store-specific setting mode: validated setting (TProperty) is overriden for current store</item>
        /// </list>
        /// </summary>
        /// <param name="rule">The current rule</param>
        /// <param name="predicate">A lambda expression that specifies a condition for when the validator should run</param>
        /// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain.</param>
        /// <exception cref="ArgumentException">Raised when validator class does not derive from <see cref="SettingModelValidator{TModel, TSetting}"/></exception>
        public static IRuleBuilderOptions<T, TProperty> WhenSettingOverriden<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> rule, 
            Func<T, TProperty, bool> predicate, 
            ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
            => WhenSettingOverridenInternal(rule, predicate, applyConditionTo, false);

        /// <summary>
        /// Specifies a condition limiting when the validator should NOT run. 
        /// The validator will only be executed if...
        /// <list type="bullet">
        ///     <item>In case of store-agnostic setting mode: the result of the <paramref name="predicate"/> parameter returns FALSE, OR</item>
        ///     <item>In case of store-specific setting mode: validated setting (TProperty) is NOT overriden for current store</item>
        /// </list>
        /// </summary>
        /// <param name="rule">The current rule</param>
        /// <param name="predicate">A lambda expression that specifies a condition for when the validator should NOT run</param>
        /// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain.</param>
        /// <exception cref="ArgumentException">Raised when validator class does not derive from <see cref="SettingModelValidator{TModel, TSetting}"/></exception>
        public static IRuleBuilderOptions<T, TProperty> UnlessSettingOverriden<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> rule,
            Func<T, TProperty, bool> predicate,
            ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
            => WhenSettingOverridenInternal(rule, predicate, applyConditionTo, true);

        public static IRuleBuilderOptions<T, TProperty> WhenSettingOverridenInternal<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> rule,
            Func<T, TProperty, bool> predicate,
            ApplyConditionTo applyConditionTo,
            bool negate)
        {
            Guard.NotNull(predicate, nameof(predicate));

            return rule.Configure(config =>
            {
                config.ApplyCondition(
                    ctx =>
                    {
                        var builder = rule as RuleBuilder<T, TProperty>;
                        var validator = builder.ParentValidator as ISettingModelValidator 
                            ?? throw new ArgumentException($"The validator must derive from {nameof(SettingModelValidator<T, ISettings>)} when using {nameof(WhenSettingOverriden)}.", nameof(rule));

                        var condition =
                            (validator.StoreScope == 0 && predicate((T)ctx.InstanceToValidate, (TProperty)ctx.PropertyValue)) ||
                            (validator.StoreScope > 0 && validator.IsOverridenSetting(ctx.PropertyName));

                        return negate ? !condition : condition;
                    },
                    applyConditionTo);
            });
        }
    }
}
