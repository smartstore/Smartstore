// TODO: (mg) (core) move forum string resources to module.
namespace Smartstore.Forum
{
    /// <summary>
    /// Represents a forum topic type.
    /// </summary>
    public enum ForumTopicType
    {
        /// <summary>
        /// Normal
        /// </summary>
        Normal = 10,

        /// <summary>
        /// Sticky
        /// </summary>
        
        Sticky = 15,
        /// <summary>
        /// Announcement
        /// </summary>
        Announcement = 20,
    }

    public enum ForumDateFilter
    {
        LastVisit = 0,
        Yesterday = 1,
        LastWeek = 7,
        LastTwoWeeks = 14,
        LastMonth = 30,
        LastThreeMonths = 92,
        LastSixMonths = 183,
        LastYear = 365
    }

    /// <summary>
    /// Represents a forum editor type.
    /// </summary>
    public enum EditorType
    {
        /// <summary>
        /// SimpleTextBox
        /// </summary>
        SimpleTextBox = 10,

        /// <summary>
        /// BBCode Editor
        /// </summary>
        BBCodeEditor = 20
    }

    /// <summary>
    /// Represents the sorting of forum topics.
    /// </summary>
    public enum ForumTopicSorting
    {
        /// <summary>
        /// Initial state
        /// </summary>
        Initial = 0,

        /// <summary>
        /// Relevance
        /// </summary>
        Relevance,

        /// <summary>
        /// Subject: A to Z
        /// </summary>
        SubjectAsc,

        /// <summary>
        /// Subject: Z to A
        /// </summary>
        SubjectDesc,

        /// <summary>
        /// User name: A to Z
        /// </summary>
        UserNameAsc,

        /// <summary>
        /// User name: Z to A
        /// </summary>
        UserNameDesc,

        /// <summary>
        /// Creation date: Oldest first
        /// </summary>
        CreatedOnAsc,

        /// <summary>
        /// Creation date: Newest first
        /// </summary>
        CreatedOnDesc,

        /// <summary>
        /// Number of posts: Low to High
        /// </summary>
        PostsAsc,

        /// <summary>
        /// Number of posts: High to Low
        /// </summary>
        PostsDesc
    }
}
