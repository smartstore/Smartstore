#nullable enable

namespace Smartstore.Core.Content.Media.Editing
{
    /// <summary>
    /// Represents a factory for retrieving media editors.
    /// </summary>
    public interface IMediaEditorFactory
    {
        /// <summary>
        /// Gets a list of all registered media editors.
        /// </summary>
        IMediaEditor[] GetEditors();
    }

    public class MediaEditorFactory(IEnumerable<IMediaEditor> mediaEditors) : IMediaEditorFactory
    {
        private readonly IEnumerable<IMediaEditor> _mediaEditors = mediaEditors;

        public virtual IMediaEditor[] GetEditors()
        {
            return [.. _mediaEditors
                .Where(x => x.IsActive())
                .OrderBy(x => x.Order)];
        }
    }
}
