using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Web;

namespace Smartstore.Web.Models.Search
{
    // TODO: (core) Implement IForumSearchResultModel in external module

    public interface ISearchResultModel
    {
        CatalogSearchResult SearchResult { get; }
    }

    [ModelBinder(typeof(ISearchResultModel))]
    public class SearchResultModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var rootModel = bindingContext.HttpContext.RequestServices.GetService<IViewDataAccessor>().ViewData?.Model;
            
            if (rootModel is ISearchResultModel searchResultModel)
            {
                bindingContext.Result = ModelBindingResult.Success(searchResultModel);
            }

            return Task.CompletedTask;
        }
    }
}
