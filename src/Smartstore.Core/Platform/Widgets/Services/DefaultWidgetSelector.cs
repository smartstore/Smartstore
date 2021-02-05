using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetSelector : IWidgetSelector
    {
        private readonly IWidgetProvider _widgetProvider;
        private readonly IWidgetService _widgetService;
        private readonly IStoreContext _storeContext;

        public DefaultWidgetSelector(
            IWidgetService widgetService, 
            IWidgetProvider widgetProvider,
            IStoreContext storeContext)
        {
            _widgetService = widgetService;
            _widgetProvider = widgetProvider;
            _storeContext = storeContext;
        }

        public Task<IEnumerable<WidgetInvoker>> GetWidgetsAsync(string zone, object model = null)
        {
            Guard.NotEmpty(zone, nameof(zone));

            var storeId = _storeContext.CurrentStore.Id;

            #region Module Widgets

            var widgets = _widgetService.LoadActiveWidgetsByWidgetZone(zone, storeId)
                .Select(x => x.Value.GetDisplayWidget(zone, model, storeId))
                .Where(x => x != null);

            #endregion

            #region Topic Widgets

            // TODO: (core) DefaultWidgetSelector > Determine topic widgets
            widgets = widgets.Concat(Enumerable.Empty<WidgetInvoker>());

            #endregion

            #region Request scoped widgets (provided by IWidgetProvider)

            widgets = widgets.Concat(_widgetProvider.GetWidgets(zone));

            #endregion

            widgets = widgets
                .Distinct()
                .OrderBy(x => x.Prepend)
                .ThenBy(x => x.Order);

            return Task.FromResult(widgets);
        }
    }
}
