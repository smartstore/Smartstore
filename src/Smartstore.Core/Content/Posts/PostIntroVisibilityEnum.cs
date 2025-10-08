namespace Smartstore.Core.Content.Posts
{
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
