using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Categories
{
    public interface ICategoryNode : ILocalizedEntity, ISlugSupported, IAclRestricted, IStoreRestricted
    {
        new int Id { get; }
        int ParentCategoryId { get; }
        string Name { get; }
        string ExternalLink { get; }
        string Alias { get; }
        int? MediaFileId { get; }
        bool Published { get; }
        int DisplayOrder { get; }
        DateTime UpdatedOnUtc { get; }
        string BadgeText { get; }
        int BadgeStyle { get; }
    }

    [Serializable]
    public class CategoryNode : ICategoryNode
    {
        public int Id { get; set; }
        public int ParentCategoryId { get; set; }
        public string Name { get; set; }
        public string ExternalLink { get; set; }
        public string Alias { get; set; }
        public int? MediaFileId { get; set; }
        public bool Published { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public string BadgeText { get; set; }
        public int BadgeStyle { get; set; }
        public bool SubjectToAcl { get; set; }
        public bool LimitedToStores { get; set; }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Name;
        }

        /// <inheritdoc/>
        public string[] GetDisplayNameMemberNames()
        {
            return new[] { nameof(Name) };
        }

        /// <inheritdoc/>
        public string GetEntityName()
        {
            return nameof(Category);
        }
    }
}
