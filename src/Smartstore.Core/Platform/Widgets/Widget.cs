#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    // TODO: (core) Make Json converters for widget implementations.

    /// <summary>
    /// Base class for widgets.
    /// </summary>
    public abstract class Widget : IEquatable<Widget>, IComparable<Widget>
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
        public string? Key { get; set; }

        /// <summary>
        /// Whether the widget is valid in the given <paramref name="zone"/>.
        /// An invalid widget will not be listed in widget zone enumerations.
        /// </summary>
        /// <param name="zone">The widget zone</param>
        public virtual bool IsValid(IWidgetZone zone) 
            => true;

        /// <summary>
        /// Invokes the widget and returns its content.
        /// </summary>
        /// <returns>The result HTML content.</returns>
        public Task<IHtmlContent> InvokeAsync(ViewContext viewContext) 
            => InvokeAsync(new WidgetContext(viewContext));

        /// <summary>
        /// Invokes the widget and returns its content.
        /// </summary>
        /// <param name="context">The widget context</param>
        /// <returns>The result HTML content.</returns>
        public abstract Task<IHtmlContent> InvokeAsync(WidgetContext context);

        /// <inheritdoc />
        int IComparable<Widget>.CompareTo(Widget? other)
        {
            if (other is null) return 1;

            int result = Prepend.CompareTo(other.Prepend);
            if (result == 0)
            {
                result = Order.CompareTo(other.Order);
            }

            return result;
        }

        #region Equatable

        public static bool operator ==(Widget x, Widget y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(Widget x, Widget y)
        {
            return !Equals(x, y);
        }

        public override bool Equals(object? obj)
        {
            return ((IEquatable<Widget>)this).Equals(obj as Widget);
        }

        bool IEquatable<Widget>.Equals(Widget? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || Key == null || other.Key == null)
            {
                return false;
            }

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
