using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Smartstore.Web.Modelling.Validation
{
    /// <summary>
    /// Replaces MVC's RequiredAttributeAdapter with a localizing client validator.
    /// </summary>
    internal class SmartClientModelValidatorProvider : IClientModelValidatorProvider
    {
        private readonly IApplicationContext _appContext;
        private readonly ValidatorLanguageManager _languageManager;

        public SmartClientModelValidatorProvider(IApplicationContext appContext, ValidatorLanguageManager languageManager)
        {
            _appContext = appContext;
            _languageManager = languageManager;
        }

        public void CreateValidators(ClientValidatorProviderContext context)
        {
            var requiredAdapter = context.Results.FirstOrDefault(x => x.Validator is RequiredAttributeAdapter);

            if (requiredAdapter != null)
            {
                // If the property is a non-nullable value type, then MVC will have already generated a Required rule.
                // We gonna provide our own localized Requried rule, then remove the MVC one.

                context.Results.Remove(requiredAdapter);
                context.Results.Add(new ClientValidatorItem
                {
                    IsReusable = false,
                    Validator = new RequiredValidator(_languageManager)
                });
            }
        }

        class RequiredValidator : IClientModelValidator
        {
            const string NotEmptyValidatorKey = "NotEmptyValidator";

            private readonly ValidatorLanguageManager _languageManager;

            public RequiredValidator(ValidatorLanguageManager languageManager)
            {
                _languageManager = languageManager;
            }

            public void AddValidation(ClientModelValidationContext context)
            {
                var propertyName = context.ModelMetadata.GetDisplayName();
                var errorMessage = _languageManager.GetErrorMessage(NotEmptyValidatorKey, propertyName);

                context.Attributes.Merge("data-val", "true", false);
                context.Attributes.Merge("data-val-required", errorMessage, false);
            }
        }
    }
}
