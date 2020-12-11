using System.Threading.Tasks;

namespace Smartstore.Web.Common.Mvc.Razor
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, bool isMainPage = false);
    }
}
