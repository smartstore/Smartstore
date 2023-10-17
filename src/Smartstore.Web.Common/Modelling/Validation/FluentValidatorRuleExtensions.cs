using Smartstore;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace FluentValidation
{
    public static class FluentValidatorRuleExtensions
    {
        private const string InvalidNameChars = "<>@#$€?!";

        public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> ruleBuilder, Localizer localizer)
        {
            var invalidNameChars = string.Join(" ", InvalidNameChars.ToCharArray());

            return ruleBuilder.Matches(expression: @"^[^" + InvalidNameChars + "0-9]+$")
                .WithMessage(localizer("Admin.Address.Fields.Name.InvalidChars", invalidNameChars));
        }

        public static IRuleBuilderOptions<T, string> CreditCardCvvNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Matches(RegularExpressions.IsCvv);
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
            this IRuleBuilderOptions<T, TProperty> ruleBuilder,
            Func<T, ValidationContext<T>, bool> predicate,
            ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
            => WhenSettingOverridenInternal(ruleBuilder, predicate, applyConditionTo, false);

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
            this IRuleBuilderOptions<T, TProperty> ruleBuilder,
            Func<T, ValidationContext<T>, bool> predicate,
            ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
            => WhenSettingOverridenInternal(ruleBuilder, predicate, applyConditionTo, true);

        public static IRuleBuilderOptions<T, TProperty> WhenSettingOverridenInternal<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> ruleBuilder,
            Func<T, ValidationContext<T>, bool> predicate,
            ApplyConditionTo applyConditionTo,
            bool negate)
        {
            Guard.NotNull(predicate, nameof(predicate));

            var validatorProperty = ruleBuilder.GetType().GetProperty("ParentValidator");
            var validator = validatorProperty?.GetValue(ruleBuilder, null) as ISettingModelValidator
                ?? throw new ArgumentException($"The validator must derive from {nameof(SettingModelValidator<T, ISettings>)} when using 'WhenSettingOverriden'.", nameof(ruleBuilder));

            return ruleBuilder.Configure(rule =>
            {
                rule.ApplyCondition(ctx =>
                {
                    var condition =
                        (validator.StoreScope == 0 && predicate(ctx.InstanceToValidate, ctx)) ||
                        (validator.StoreScope > 0 && validator.IsOverridenSetting(ctx.PropertyName));

                    return negate ? !condition : condition;
                },
                applyConditionTo);
            });
        }
    }
}
