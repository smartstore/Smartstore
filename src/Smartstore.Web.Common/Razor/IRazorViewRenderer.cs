using System.Threading.Tasks;

namespace Smartstore.Web.Razor
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, bool isMainPage = false);
    }
}
