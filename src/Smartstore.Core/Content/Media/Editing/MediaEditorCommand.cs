#nullable enable

namespace Smartstore.Core.Content.Media.Editing
{
    /// <summary>
    /// Represents a media editing command like a toolbar button or file context menu item.
    /// </summary>
    public partial class MediaEditorCommand
    {
        /// <summary>
        /// Gets or sets the value of the HTML id attribute. Must be unique.
        /// </summary>
        /// <example>btn-myeditor-crop</example>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the displayed command name.
        /// </summary>
        /// <example>Edit with abc</example>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the HTML title attribute.
        /// </summary>
        /// <example>Enables to crop an image</example>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets one or more CSS classes.
        /// </summary>
        public string? ItemClass { get; set; }

        /// <summary>
        /// Gets or sets one or more CSS classes for the command icon (if any).
        /// </summary>
        public string? IconClass { get; set; }

        /// <summary>
        /// Gets or sets the name of the icon library used for rendering icons (fa or bi). Default: fa.
        /// </summary>
        public string IconLibrary { get; set; } = "fa";

        /// <summary>
        /// Gets or sets the URL for the command icon.
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// Gets the supported file type.
        /// </summary>
        /// <remarks>
        /// One of two conditions must be fulfilled (or-combined) if <see cref="SupportedFileType"/> and <see cref="SupportedFileExtensions"/> are specified.
        /// </remarks>
        /// <example>image</example>
        public string? SupportedFileType { get; set; }

        /// <summary>
        /// Gets the supported, dot-less file extensions.
        /// </summary>
        /// <remarks>
        /// One of two conditions must be fulfilled (or-combined) if <see cref="SupportedFileType"/> and <see cref="SupportedFileExtensions"/> are specified.
        /// </remarks>
        /// <example>["jpg", "png"]</example>
        public string[] SupportedFileExtensions { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether this command supports execution when multiple items are selected.
        /// If <c>false</c> (default), the command is disabled when more than one file is selected.
        /// </summary>
        public bool SupportsMultiSelection { get; set; }

        /// <summary>
        /// Gets or sets HTML attributes.
        /// </summary>
        public IDictionary<string, object?>? HtmlAttributes { get; set; }

        /// <summary>
        /// Gets or sets child commands displayed in a dropdown for a toolbar command.
        /// </summary>
        /// <remarks>
        /// Context menus (bootstrap dropdown) do not support submenus or child commands.
        /// </remarks>
        public IList<MediaEditorCommand>? Commands { get; set; }
    }
}
