using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin
{
    public static class HtmlHelperExtensions
    {
        public static Task<IHtmlContent> ProviderList<TModel>(this IHtmlHelper<IEnumerable<TModel>> html,
            IEnumerable<TModel> model,
            Func<TModel, object> buttonTemplate = null,
            Func<TModel, object> infoTemplate = null) where TModel : ProviderModel
        {
            var list = new ProviderModelCollection<TModel>(model);
            list.SetTemplates(buttonTemplate, infoTemplate);
            
            return html.PartialAsync("_Providers", list);
        }
    }
}
