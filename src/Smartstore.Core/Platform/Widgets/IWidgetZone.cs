#nullable enable

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
        /// The view model.
        /// </summary>
        object? Model { get; }

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
        /// The css class(es) to apply to the widget zone preview.
        /// </summary>
        string? PreviewCssClass { get; }

        /// <summary>
        /// The css style(s) to apply to the widget zone preview.
        /// </summary>
        string? PreviewCssStyle { get; }
    }

    public class PlainWidgetZone : IWidgetZone
    {
        public PlainWidgetZone(string name)
        {
            Guard.NotEmpty(name);
            Name = name;
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public object? Model { get; set; }

        /// <inheritdoc />
        public bool ReplaceContent { get; set; }

        /// <inheritdoc />
        public bool RemoveIfEmpty { get; set; }

        /// <inheritdoc />
        public bool PreviewDisabled { get; set; }

        /// <inheritdoc />
        public string? PreviewCssClass { get; set; }

        /// <inheritdoc />
        public string? PreviewCssStyle { get; set; }
    }
}
