using Smartstore.Domain;

namespace Smartstore.Blog.Domain
{
    /// <summary>
    /// Represents a blog post tag.
    /// </summary>
    public partial class BlogPostTag : IDisplayedEntity, IEquatable<BlogPostTag>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tagged product count.
        /// </summary>
        public int BlogPostCount { get; set; }

        public override string ToString()
            => Name.EmptyNull();

        public override int GetHashCode()
            => Name?.ToLowerInvariant()?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
            => Equals(obj as BlogPostTag);

        public bool Equals(BlogPostTag other)
            => string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase);

        string IDisplayedEntity.GetDisplayNameMemberName()
            => nameof(Name);

        string IDisplayedEntity.GetDisplayName()
            => Name;

        string INamedEntity.GetEntityName()
            => nameof(BlogPostTag);
    }
}
