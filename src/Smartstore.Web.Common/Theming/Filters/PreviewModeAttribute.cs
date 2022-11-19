using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Web;

namespace Smartstore.Web.Theming
{
    /// <summary>
    /// Activates preview mode in public store if preview mode was enabled in backend.
    /// </summary>
    public sealed class PreviewModeAttribute : TypeFilterAttribute
    {
        public PreviewModeAttribute()
            : base(typeof(PreviewModeFilter))
        {
        }

        class PreviewModeFilter : IResultFilter
        {
            private readonly IPreviewModeCookie _previewCookie;
            private readonly ICommonServices _services;
            private readonly IWidgetProvider _widgetProvider;

            public PreviewModeFilter(
                IPreviewModeCookie previewCookie,
                ICommonServices services,
                IWidgetProvider widgetProvider)
            {
                _previewCookie = previewCookie;
                _services = services;
                _widgetProvider = widgetProvider;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (!context.Result.IsHtmlViewResult())
                    return;

                if (_previewCookie.AllOverrideKeys.Count == 0)
                    return;

                if (!_services.Permissions.Authorize(Permissions.Configuration.Theme.Read))
                    return;

                _widgetProvider.RegisterWidget("end", new ComponentWidget("PreviewTool", null));
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
                // Noop
            }
        }
    }
}
