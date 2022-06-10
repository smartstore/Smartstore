using Autofac;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Core.Catalog.Search.Modelling
{
    public class CatalogSearchQueryModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            var factory = bindingContext.HttpContext.GetServiceScope().ResolveOptional<ICatalogSearchQueryFactory>();
            if (factory == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else if (factory.Current != null)
            {
                bindingContext.Result = ModelBindingResult.Success(factory.Current);
            }
            else if (bindingContext.ModelType != typeof(CatalogSearchQuery))
            {
                bindingContext.Result = ModelBindingResult.Success(new CatalogSearchQuery());
            }
            else
            {
                var query = await factory.CreateFromQueryAsync();
                bindingContext.Result = ModelBindingResult.Success(query);
            }
        }
    }
}
