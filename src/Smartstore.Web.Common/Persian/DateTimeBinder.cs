using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using SmartStore.Web.Framework.Persian;
using System.ServiceModel.Channels;

namespace SmartStore.Web.Framework.Persian
{
    public class PersianDateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (PersianCulture.IsPersianCulture())
            {
                if (context.Metadata.ModelType == typeof(DateTime) || context.Metadata.ModelType == typeof(DateTime?))
                {
                    return new BinderTypeModelBinder(typeof(DateTimeBinder));
                }
            }
            if (context.Metadata.ModelType == typeof(DateTime?))
            {
                return null;
            }
            if (context.Metadata.ModelType == typeof(DateTime))
            {
                return new BinderTypeModelBinder(typeof(DateTimeBinder));
            }

            return null;
        }
    }

    public class NullableDateTimeBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

            var attemptedValue = valueResult.FirstValue;

            if (string.IsNullOrWhiteSpace(attemptedValue))
            {
                return Task.CompletedTask;
            }

            object actualValue = null;
            bool success = false;

            if (PersianCulture.IsPersianCulture())
            {
                try
                {
                    actualValue = PersianDateExtensionMethods.ConvertDateTime(attemptedValue);
                    success = true;
                }
                catch (Exception e)
                {
                    bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, e.Message);
                }
            }
            else
            {
                success = DateTime.TryParse(attemptedValue, Thread.CurrentThread.CurrentUICulture, DateTimeStyles.None, out var dateTime);
                actualValue = dateTime;
            }

            if (success)
            {
                if (bindingContext.ModelMetadata.IsNullableValueType && actualValue == null)
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(actualValue);
                }
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "A valid Date or Date and Time must be entered.");
            }

            return Task.CompletedTask;
        }
    }
}


public class DateTimeBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var attemptedValue = valueResult.FirstValue;

        if (string.IsNullOrWhiteSpace(attemptedValue))
        {
            return Task.CompletedTask;
        }

        object actualValue = null;
        bool success = false;

        if (PersianCulture.IsPersianCulture())
        {
            try
            {
                actualValue = PersianDateExtensionMethods.ConvertDateTime(attemptedValue);
                success = true;
            }
            catch (Exception e)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, e.Message);
            }
        }
        else
        {
            success = DateTime.TryParse(attemptedValue, Thread.CurrentThread.CurrentUICulture, DateTimeStyles.None, out var dateTime);
            actualValue = dateTime;
        }

        if (success)
        {
            if (bindingContext.ModelMetadata.IsNullableValueType && actualValue == null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(actualValue);
            }
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "A valid Date or Date and Time must be entered.");
        }

        return Task.CompletedTask;
    }
}
