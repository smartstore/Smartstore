using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Web.Modelling.Validation
{
    /// <summary>
    /// An abstract validator that is capable of ignoring rules for unchecked setting properties
    /// in a store-specific edit session.
    /// </summary>
    /// <typeparam name="TModel">Type of setting model</typeparam>
    /// <typeparam name="TSetting">Type of actual setting class that is being configured</typeparam>
    public abstract class SettingModelValidator<TModel, TSetting> : AbstractValidator<TModel>, IValidatorInterceptor
        where TSetting : ISettings
    {
        IValidationContext IValidatorInterceptor.BeforeMvcValidation(ControllerContext controllerContext, IValidationContext commonContext)
        {
            var httpContext = controllerContext.HttpContext;
            if (!httpContext.Request.HasFormContentType || httpContext.Request.Form == null)
            {
                return commonContext;
            }

            var activeStoreScope = GetActiveStoreScopeConfiguration(httpContext);
            if (activeStoreScope == 0)
            {
                // Unnecessary to continue customizing validation. Just do it out-of-the-box.
                return commonContext;
            }

            var selector = new MultiStoreSettingValidatorSelector(
                typeof(TSetting),
                controllerContext.HttpContext.Request.Form);

            var validationContext = new ValidationContext<TModel>(
                (TModel)commonContext.InstanceToValidate,
                commonContext.PropertyChain,
                selector);

            return validationContext;
        }

        ValidationResult IValidatorInterceptor.AfterMvcValidation(ControllerContext controllerContext, IValidationContext commonContext, ValidationResult result)
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

            public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context)
            {
                // By default we ignore any rules part of a RuleSet.
                if (rule.RuleSets.Length > 0 && !rule.RuleSets.Contains(RulesetValidatorSelector.DefaultRuleSetName, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Reject property validator if property checkbox is unchecked.
                // There is nothing to validate in that case, because only a patch of setting data is sent to the server.
                var overridden = StoreDependingSettingHelper.IsOverrideChecked(_settingType, propertyPath, _form);
                return overridden;
            }
        }
    }
}
