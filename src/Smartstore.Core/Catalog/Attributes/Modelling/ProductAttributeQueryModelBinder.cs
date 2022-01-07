using Autofac;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Core.Catalog.Attributes.Modelling
{
    public class ProductAttributeQueryModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            var factory = bindingContext.HttpContext.GetServiceScope().ResolveOptional<IProductVariantQueryFactory>();
            if (factory == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else if (factory.Current != null)
            {
                bindingContext.Result = ModelBindingResult.Success(factory.Current);
            }
            else if (bindingContext.ModelType != typeof(ProductVariantQuery))
            {
                bindingContext.Result = ModelBindingResult.Success(new ProductVariantQuery());
            }
            else
            {
                var query = factory.CreateFromQuery();
                bindingContext.Result = ModelBindingResult.Success(query);
            }

            return Task.CompletedTask;
        }
    }
}
