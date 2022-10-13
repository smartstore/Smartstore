using FluentValidation.AspNetCore;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling.Settings;

namespace FluentValidation
{
    public interface ISettingModelValidator
    {
        int StoreScope { get; }
        bool IsOverridenSetting(string propertyPath);
    }

    /// <summary>
    /// An abstract validator that is capable of ignoring rules for unchecked setting properties
    /// in a store-specific edit session.
    /// Does not support manually validation by calling <see cref="AbstractValidator.Validate"/> directly.
    /// </summary>
    /// <typeparam name="TModel">Type of setting model</typeparam>
    /// <typeparam name="TSetting">Type of actual setting class that is being configured</typeparam>
    public abstract class SettingModelValidator<TModel, TSetting> : AbstractValidator<TModel>, IValidatorInterceptor, ISettingModelValidator
        where TSetting : ISettings
    {
        private MultiStoreSettingValidatorSelector _validatorSelector;

        IValidationContext IValidatorInterceptor.BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
        {
            var httpContext = actionContext.HttpContext;
            if (!httpContext.Request.HasFormContentType || httpContext.Request.Form == null)
            {
                return commonContext;
            }

            StoreScope = GetActiveStoreScopeConfiguration(httpContext);
            if (StoreScope == 0)
            {
                // Unnecessary to continue customizing validation. Just do out-of-the-box stuff.
                return commonContext;
            }

            _validatorSelector = new MultiStoreSettingValidatorSelector(
                typeof(TSetting),
                httpContext.Request.Form);

            var validationContext = new ValidationContext<TModel>(
                (TModel)commonContext.InstanceToValidate,
                commonContext.PropertyChain,
                _validatorSelector);

            return validationContext;
        }

        public int StoreScope { get; private set; }

        public bool IsOverridenSetting(string propertyPath)
            => _validatorSelector?.IsOverrideChecked(propertyPath) ?? false;

        ValidationResult IValidatorInterceptor.AfterAspNetValidation(ActionContext actionContext, IValidationContext commonContext, ValidationResult result)
            => result;

        private static int GetActiveStoreScopeConfiguration(HttpContext httpContext)
        {
            var services = httpContext.RequestServices.GetRequiredService<ICommonServices>();
            var storeId = services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var store = services.StoreContext.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }

        class MultiStoreSettingValidatorSelector : IValidatorSelector
        {
            private readonly Type _settingType;
            private readonly IFormCollection _form;

            public MultiStoreSettingValidatorSelector(Type settingType, IFormCollection form)
            {
                _settingType = settingType;
                _form = form;
            }

            public bool IsOverrideChecked(string propertyPath)
                => MultiStoreSettingHelper.IsOverrideChecked(_settingType, propertyPath, _form);

            public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context)
            {
                // By default we ignore any rules part of a RuleSet.
                if (rule.RuleSets.Length > 0 && !rule.RuleSets.Contains(RulesetValidatorSelector.DefaultRuleSetName, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Reject property validator if property checkbox is unchecked.
                // There is nothing to validate in that case, because only a patch of setting data is sent to the server.
                var overridden = IsOverrideChecked(propertyPath);
                return overridden;
            }
        }
    }
}
