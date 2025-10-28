#nullable enable

using Smartstore.Core.Localization;

namespace Smartstore.Core.Content.Media.Editing
{
    /// <summary>
    /// Represents an editor for editing media files.
    /// </summary>
    public interface IMediaEditor
    {
        /// <summary>
        /// The name of the editor.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The order in which the editors are loaded and integrated into the menus.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the editor is active.
        /// </summary>
        bool IsActive();

        /// <summary>
        /// Gets a list of commands to be added to the Media Manager.
        /// </summary>
        /// <param name="forToolbar">
        /// A value indicating whether to get the commands for the toolbar.
        /// If <c>false</c> then the commands for the file's context menu are returned.
        /// </param>
        Task<IEnumerable<MediaEditorCommand>> GetCommandsAsync(bool forToolbar);
    }

    /// <inheritdoc />
    public abstract class MediaEditorBase : IMediaEditor
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual int Order { get; } = 0;

        /// <inheritdoc />
        public abstract bool IsActive();

        /// <inheritdoc />
        public abstract Task<IEnumerable<MediaEditorCommand>> GetCommandsAsync(bool forToolbar);
    }
}
