using System.Threading.Tasks;

namespace Smartstore.Web.Common.Theming
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, bool isMainPage = false);
    }
}
