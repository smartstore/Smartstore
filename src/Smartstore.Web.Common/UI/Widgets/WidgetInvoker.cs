using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.UI
{
    /// <summary>
    /// Base class for widgets.
    /// </summary>
    public abstract class WidgetInvoker
    {
        /// <summary>
        /// Order of widget within the zone.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the widget output should be inserted BEFORE existing content. 
        /// Defaults to <c>false</c>, which means the widget output comes AFTER any existing content.
        /// </summary>
        public bool Prepend { get; set; }

        /// <summary>
        /// Invokes (renders) the widget.
        /// </summary>
        /// <returns>The result HTML content.</returns>
        public abstract Task<IHtmlContent> InvokeAsync(ViewContext viewContext);
    }
}
