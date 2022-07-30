using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for binding <see cref="decimal"/>, <see cref="double"/>,
    /// <see cref="float"/>, and their <see cref="Nullable{T}"/> wrappers in invariant culture.
    /// </summary>
    public class InvariantFloatingPointTypeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            Guard.NotNull(context, nameof(context));

            var modelType = context.Metadata.UnderlyingOrModelType;

            if (modelType == typeof(decimal))
            {
                return new InvariantDecimalModelBinder();
            }

            if (modelType == typeof(double))
            {
                return new InvariantDoubleModelBinder();
            }

            if (modelType == typeof(float))
            {
                return new InvariantFloatModelBinder();
            }

            return null;
        }
    }

    internal abstract class InvariantFloatingPointModelBinder<T> : IModelBinder
        where T : struct
    {
        static readonly NumberStyles SupportedStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // No entry
                return Task.CompletedTask;
            }

            var modelState = bindingContext.ModelState;
            modelState.SetModelValue(modelName, valueProviderResult);

            var metadata = bindingContext.ModelMetadata;
            var type = metadata.UnderlyingOrModelType;

            try
            {
                var value = valueProviderResult.FirstValue;

                object model = null;
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Parse() method trims the value (with common NumberStyles) then throws if the result is empty.
                    model = null;
                }
                else if (type == typeof(T))
                {
                    if (TryParse(value, SupportedStyles, CultureInfo.InvariantCulture, out var result))
                    {
                        model = result;
                    }
                    else if (TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, valueProviderResult.Culture, out result))
                    {
                        // Try again with UI culture, because some controls may send formatted values (like range slider).
                        model = result;
                    }
                }
                else
                {
                    // Unreachable
                    throw new NotSupportedException();
                }

                // When converting value, a null model may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !metadata.IsReferenceOrNullableType)
                {
                    modelState.TryAddModelError(
                        modelName,
                        metadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    // Unlike TypeConverters, floating point types do not seem to wrap FormatExceptions. Preserve
                    // this code in case a cursory review of the CoreFx code missed something.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                modelState.TryAddModelError(modelName, exception, metadata);

                // Conversion failed.
            }

            return Task.CompletedTask;
        }

        protected abstract bool TryParse(string value, NumberStyles supportedStyles, CultureInfo culture, out T result);
    }

    internal class InvariantDecimalModelBinder : InvariantFloatingPointModelBinder<decimal>
    {
        protected override bool TryParse(string value, NumberStyles supportedStyles, CultureInfo culture, out decimal result)
            => decimal.TryParse(value, supportedStyles, culture, out result);
    }

    internal class InvariantDoubleModelBinder : InvariantFloatingPointModelBinder<double>
    {
        protected override bool TryParse(string value, NumberStyles supportedStyles, CultureInfo culture, out double result)
            => double.TryParse(value, supportedStyles, culture, out result);
    }

    internal class InvariantFloatModelBinder : InvariantFloatingPointModelBinder<float>
    {
        protected override bool TryParse(string value, NumberStyles supportedStyles, CultureInfo culture, out float result)
             => float.TryParse(value, supportedStyles, culture, out result);
    }
}
