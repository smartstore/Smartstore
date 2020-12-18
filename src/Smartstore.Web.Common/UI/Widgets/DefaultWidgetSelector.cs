using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Web.UI
{
    public class DefaultWidgetSelector : IWidgetSelector
    {
        private readonly IWidgetProvider _widgetProvider;

        public DefaultWidgetSelector(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        public IEnumerable<WidgetInvoker> GetWidgets(string zone, object model = null)
        {
            Guard.NotEmpty(zone, nameof(zone));
            
            #region Plugin Widgets

            // TODO: (core) DefaultWidgetSelector > Determine static plugin widgets
            var widgets = Enumerable.Empty<WidgetInvoker>();

            #endregion

            #region Topic Widgets

            // TODO: (core) DefaultWidgetSelector > Determine topic widgets
            widgets = widgets.Concat(Enumerable.Empty<WidgetInvoker>());

            #endregion

            #region Request scoped widgets (provided by IWidgetProvider)

            widgets = widgets.Concat(_widgetProvider.GetWidgets(zone));

            #endregion

            return widgets.OrderBy(x => x.Order);
        }
    }
}
