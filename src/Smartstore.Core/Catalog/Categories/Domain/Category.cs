using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Categories
{
    internal class CategoryMap : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasMany(c => c.RuleSets)
                .WithMany(c => c.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "RuleSet_Category_Mapping",
                    c => c
                        .HasOne<RuleSetEntity>()
                        .WithMany()
                        .HasForeignKey("RuleSetEntity_Id")
                        .HasConstraintName("FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Category>()
                        .WithMany()
                        .HasForeignKey("Category_Id")
                        .HasConstraintName("FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Category_Id");
                        c.HasKey("Category_Id", "RuleSetEntity_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a category of products.
    /// </summary>
    [DebuggerDisplay("{Id}: {Name} (Parent: {ParentCategoryId})")]
    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(DisplayOrder), Name = "IX_Category_DisplayOrder")]
    [Index(nameof(LimitedToStores), Name = "IX_Category_LimitedToStores")]
    [Index(nameof(ParentCategoryId), Name = "IX_Category_ParentCategoryId")]
    [Index(nameof(SubjectToAcl), Name = "IX_Category_SubjectToAcl")]
    [LocalizedEntity("Published and !Deleted")]
    public partial class Category : EntityWithDiscounts, ICategoryNode, IAuditable, ISoftDeletable, IPagingOptions, IDisplayOrder, IRulesContainer
    {
        public Category()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Category(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the category name.
        /// </summary>
        [Required, StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full name (category page title).
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a description displayed at the bottom of the category page.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string BottomDescription { get; set; }

        /// <summary>
        /// Gets or sets the external link expression. If set, any category menu item will navigate to the specified link.
        /// </summary>
        [StringLength(255)]
        public string ExternalLink { get; set; }

        /// <summary>
		/// Gets or sets a text displayed in a badge next to the category within menus.
		/// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string BadgeText { get; set; }

        /// <summary>
		/// Gets or sets the type of the badge within menus.
		/// </summary>
        public int BadgeStyle { get; set; }

        /// <summary>
        /// Gets or sets the category alias.
        /// It's an optional key intended for advanced customization.
        /// </summary>
        [StringLength(100)]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the category template identifier.
        /// </summary>
        public int CategoryTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        [StringLength(4000)]
        [LocalizedProperty]
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets the parent category identifier.
        /// </summary>
        public int ParentCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int? MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader?.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <inheritdoc/>
        public int? PageSize { get; set; }

        /// <inheritdoc/>
        public bool? AllowCustomersToSelectPageSize { get; set; }

        /// <inheritdoc/>
        [StringLength(200)]
        public string PageSizeOptions { get; set; }

        /// <summary>
        /// Gets or sets the available price ranges.
        /// </summary>
        [IgnoreDataMember, Obsolete("Price ranges are calculated automatically since version 3.")]
        [StringLength(400)]
        public string PriceRanges { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the category on home page.
        /// </summary>
        public bool ShowOnHomePage { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        /// <inheritdoc/>
        public bool SubjectToAcl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted.
        /// </summary>
        [IgnoreDataMember]
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the default view mode.
        /// </summary>
        [MaxLength]
        public string DefaultViewMode { get; set; }

        private ICollection<RuleSetEntity> _ruleSets;
        /// <summary>
        /// Gets or sets assigned rule sets.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<RuleSetEntity> RuleSets
        {
            get => LazyLoader?.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
            protected set => _ruleSets = value;
        }

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
    }
}
