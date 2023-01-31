using Microsoft.AspNetCore.Antiforgery;

namespace Smartstore.Web.Components
{
    public class AntiForgeryTokenViewComponent : SmartViewComponent
    {
        private readonly IAntiforgery _antiforgery;

        public AntiForgeryTokenViewComponent(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public IViewComponentResult Invoke()
        {
            var hasStarted = HttpContext.Response.HasStarted;
            var tokenSet = hasStarted ? _antiforgery.GetTokens(HttpContext) : _antiforgery.GetAndStoreTokens(HttpContext);
            var token = tokenSet.RequestToken;
            var html = $"<meta name='__rvt' content='{token}' />";

            return HtmlContent(html);
        }
    }
}
