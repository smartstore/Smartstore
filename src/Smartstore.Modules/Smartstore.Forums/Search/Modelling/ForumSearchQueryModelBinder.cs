using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Forums.Search.Modelling
{
    public class ForumSearchQueryModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            var factory = bindingContext.HttpContext.GetServiceScope().ResolveOptional<IForumSearchQueryFactory>();
            if (factory == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else if (factory.Current != null)
            {
                bindingContext.Result = ModelBindingResult.Success(factory.Current);
            }
            else if (bindingContext.ModelType != typeof(ForumSearchQuery))
            {
                bindingContext.Result = ModelBindingResult.Success(new ForumSearchQuery());
            }
            else
            {
                var query = await factory.CreateFromQueryAsync();
                bindingContext.Result = ModelBindingResult.Success(query);
            }
        }
    }
}
