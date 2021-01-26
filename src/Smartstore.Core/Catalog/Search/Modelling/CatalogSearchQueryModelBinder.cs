using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Core.Catalog.Search.Modelling
{
    public class CatalogSearchQueryModelBinder : IModelBinder
    {
        private readonly ICatalogSearchQueryFactory _factory;

        public CatalogSearchQueryModelBinder(ICatalogSearchQueryFactory factory)
        {
            _factory = factory;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            if (_factory.Current != null)
            {
                bindingContext.Result = ModelBindingResult.Success(_factory.Current);
                return;
            }

            var modelType = bindingContext.ModelType;
            if (modelType != typeof(CatalogSearchQuery))
            {
                bindingContext.Result = ModelBindingResult.Success(new CatalogSearchQuery());
                return;
            }

            var query = await _factory.CreateFromQueryAsync();
            bindingContext.Result = ModelBindingResult.Success(query);
        }
    }
}
