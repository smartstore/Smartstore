using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Utilities;

namespace Smartstore.Web.UI
{
    /// <summary>
    /// Base class for widgets.
    /// </summary>
    public abstract class WidgetInvoker : IEquatable<WidgetInvoker>
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
        /// When set, ensures uniqueness within a particular zone.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Invokes (renders) the widget.
        /// </summary>
        /// <returns>The result HTML content.</returns>
        public abstract Task<IHtmlContent> InvokeAsync(ViewContext viewContext);

        #region Equatable

        public static bool operator ==(WidgetInvoker x, WidgetInvoker y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(WidgetInvoker x, WidgetInvoker y)
        {
            return !Equals(x, y);
        }

        public override bool Equals(object obj)
        {
            return ((IEquatable<WidgetInvoker>)this).Equals(obj as WidgetInvoker);
        }

        bool IEquatable<WidgetInvoker>.Equals(WidgetInvoker other)
        {
            if (other == null || Key == null || other.Key == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Key == other.Key && GetType() == other.GetType();
        }

        public override int GetHashCode()
        {
            if (Key != null)
            {
                return HashCode.Combine(GetType(), Key);
            }

            return base.GetHashCode();
        }

        #endregion
    }
}
