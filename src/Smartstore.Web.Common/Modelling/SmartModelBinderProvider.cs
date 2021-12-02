using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for models which specify an <see cref="ISmartModelBinder"/>
    /// using <see cref="BindingInfo.BinderType"/>.
    /// </summary>
    public class SmartModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            Guard.NotNull(context, nameof(context));

            if (context.BindingInfo.BinderType is Type binderType)
            {
                if (typeof(ISmartModelBinder).IsAssignableFrom(binderType))
                {
                    return new BinderTypeModelBinder(binderType);
                }
            }

            return null;
        }
    }
}
