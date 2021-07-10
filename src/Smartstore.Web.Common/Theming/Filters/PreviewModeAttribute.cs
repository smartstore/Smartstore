using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;

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
            private readonly IThemeContext _themeContext;
            private readonly ICommonServices _services;
            private readonly IWidgetProvider _widgetProvider;

            public PreviewModeFilter(
                IThemeContext themeContext,
                ICommonServices services,
                IWidgetProvider widgetProvider)
            {
                _themeContext = themeContext;
                _services = services;
                _widgetProvider = widgetProvider;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (!context.Result.IsHtmlViewResult())
                    return;

                var theme = _themeContext.GetPreviewTheme();
                var storeId = _services.StoreContext.GetPreviewStore();

                if (theme == null && storeId == null)
                    return;

                if (!_services.Permissions.Authorize(Permissions.Configuration.Theme.Read))
                    return;

                _widgetProvider.RegisterWidget("end", new ComponentWidgetInvoker("PreviewTool", null));
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
                // Noop
            }
        }
    }
}
