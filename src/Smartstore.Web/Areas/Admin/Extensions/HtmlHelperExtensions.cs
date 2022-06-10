using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin
{
    public static class HtmlHelperExtensions
    {
        public static Task<IHtmlContent> ProviderList<TModel>(this IHtmlHelper<IEnumerable<TModel>> html,
            IEnumerable<TModel> model,
            params Func<TModel, object>[] extraColumns) where TModel : ProviderModel
        {
            var list = new ProviderModelCollection<TModel>();
            list.SetData(model);
            list.SetColumns(extraColumns);

            return html.PartialAsync("_Providers", list);
        }
    }
}
