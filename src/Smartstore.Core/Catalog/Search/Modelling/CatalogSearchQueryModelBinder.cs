using System.Threading.Tasks;
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

            if (factory.Current != null)
            {
                bindingContext.Result = ModelBindingResult.Success(factory.Current);
                return;
            }

            var modelType = bindingContext.ModelType;
            if (modelType != typeof(CatalogSearchQuery))
            {
                bindingContext.Result = ModelBindingResult.Success(new CatalogSearchQuery());
                return;
            }

            var query = await factory.CreateFromQueryAsync();
            bindingContext.Result = ModelBindingResult.Success(query);
        }
    }
}
