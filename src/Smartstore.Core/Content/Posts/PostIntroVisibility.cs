namespace Smartstore.Core.Content.Posts
{
    /// <summary>
    /// Specifies the visibility options for displaying the introductory text of a post.
    /// </summary>
    /// <remarks>This enumeration defines the levels of visibility for the introductory text, ranging from
    /// hidden to fully visible. Use these values to control how much of the intro text is displayed in the user
    /// interface.</remarks>
    public enum PostIntroVisibility
    {
        /// <summary>
        /// No description will be displayed.
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Two lines of the intro will be displayed.
        /// </summary>
        TwoLines = 2,

        /// <summary>
        /// Three lines of the intro will be displayed.
        /// </summary>
        ThreeLines = 3,

        /// <summary>
        /// The full intro text will be displayed.
        /// </summary>
        FullText = 99
    }
}
