using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Invokes (renders) the widget.
        /// </summary>
        /// <returns>The result HTML content.</returns>
        public abstract Task<IHtmlContent> InvokeAsync(ViewContext viewContext);
    }
}
