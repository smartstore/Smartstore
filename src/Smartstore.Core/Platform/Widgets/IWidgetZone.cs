#nullable enable

using System.Diagnostics;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Represents a widget zone.
    /// </summary>
    public interface IWidgetZone
    {
        /// <summary>
        /// The name of the zone which is rendered.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Specifies whether any default zone content should be removed if at least one 
        /// widget is rendered in the zone. Default: false.
        /// </summary>
        bool ReplaceContent { get; }

        /// <summary>
        /// Whether to remove the root zone tag when it has no content. 
        /// Only applies to HTML tags like div, span, section etc..
        /// <c>zone</c> tags are always removed. Default: false.
        /// </summary>
        bool RemoveIfEmpty { get; }

        /// <summary>
        /// Whether to disable preview mode.
        /// </summary>
        bool PreviewDisabled { get; }

        /// <summary>
        /// The tag name for the widget zone preview element. Default: span.
        /// </summary>
        string? PreviewTagName { get; }

        /// <summary>
        /// The CSS class(es) to apply to the widget zone preview.
        /// </summary>
        string? PreviewCssClass { get; }

        /// <summary>
        /// The CSS style(s) to apply to the widget zone preview.
        /// </summary>
        string? PreviewCssStyle { get; }
    }

    [DebuggerDisplay("Zone: {Name}")]
    public class PlainWidgetZone : IWidgetZone
    {
        public PlainWidgetZone(string name)
        {
            Guard.NotEmpty(name);
            Name = name;
        }

        public PlainWidgetZone(IWidgetZone zone)
        {
            Guard.NotNull(zone);

            Name = zone.Name;
            ReplaceContent = zone.ReplaceContent;
            RemoveIfEmpty = zone.RemoveIfEmpty;
            PreviewDisabled = zone.PreviewDisabled;
            PreviewTagName = zone.PreviewTagName;
            PreviewCssClass = zone.PreviewCssClass;
            PreviewCssStyle = zone.PreviewCssStyle;
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public bool ReplaceContent { get; set; }

        /// <inheritdoc />
        public bool RemoveIfEmpty { get; set; }

        /// <inheritdoc />
        public bool PreviewDisabled { get; set; }

        /// <inheritdoc />
        public string? PreviewTagName { get; set; }

        /// <inheritdoc />
        public string? PreviewCssClass { get; set; }

        /// <inheritdoc />
        public string? PreviewCssStyle { get; set; }
    }
}
