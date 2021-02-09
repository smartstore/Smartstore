namespace Smartstore.Core.Content.Blocks
{
    /// <summary>
    /// Enumeration for the current view mode of a PageBuilder block.
    /// </summary>
    public enum StoryViewMode
    {
        /// <summary>
        /// Final rendering result in public frontend.
        /// </summary>
        Public,
        /// <summary>
        /// Preview mode in backend
        /// </summary>
        Preview,
        /// <summary>
        /// Visual block editing in backend's story editor.
        /// </summary>
        GridEdit,
        /// <summary>
        /// Property dialog in backend.
        /// </summary>
        Edit
    }
}
