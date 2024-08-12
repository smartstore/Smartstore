#nullable enable

using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Core.Checkout.Payment
{
    public class PaymentValidationResult
    {
        private List<PaymentValidationFailure>? _errors;

        public PaymentValidationResult(params PaymentValidationFailure[] failures)
        {
            if (failures != null && failures.Length > 0) 
            {
                _errors = failures.Where(x => x != null).ToList();
            }
        }

        public PaymentValidationResult(ValidationResult result)
        {
            Guard.NotNull(result);
            _errors = result.Errors
                .Where(x => x != null)
                .Select(x => new PaymentValidationFailure(x.PropertyName, x.ErrorMessage))
                .ToList();
        }

        /// <summary>
        /// Whether validation succeeded
        /// </summary>
        public bool IsValid
        {
            get => _errors == null || _errors.Count == 0;
        }

        /// <summary>
        /// A collection of errors
        /// </summary>
        public List<PaymentValidationFailure> Errors
        {
            get => _errors ??= new List<PaymentValidationFailure>();
            set
            {
                Guard.NotNull(value);
                _errors = value.Where(x => x != null).ToList();
            }
        }

        /// <summary>
        /// Stores the errors to the specified modelstate dictionary.
        /// </summary>
        /// <param name="modelState">The ModelStateDictionary to store the errors in.</param>
        public void AddToModelState(ModelStateDictionary modelState)
        {
            if (!IsValid)
            {
                foreach (var error in Errors)
                {
                    modelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
        }

        /// <summary>
        /// Generates a string representation of the error messages separated by new lines.
        /// </summary>
        public override string ToString()
        {
            return ToString(Environment.NewLine);
        }

        /// <summary>
        /// Generates a string representation of the error messages separated by the specified character.
        /// </summary>
        /// <param name="separator">The character to separate the error messages.</param>
        public string ToString(string separator)
        {
            if (_errors == null || _errors.Count == 0)
            {
                return string.Empty;
            }
            
            return string.Join(separator, _errors.Select(failure => failure.ErrorMessage));
        }
    }

    public record PaymentValidationFailure
    {
        public PaymentValidationFailure()
        {
        }

        public PaymentValidationFailure(string propertyName, string errorMessage)
        {
            Guard.NotNull(propertyName);
            Guard.NotNull(errorMessage);

            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public string PropertyName { get; init; } = default!;

        /// <summary>
        /// The error message
        /// </summary>
        public string ErrorMessage { get; init; } = default!;
    }
}
