using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// Represents a menu item.
    /// </summary>
    [Table("MenuItemRecord")]
    [Index(nameof(ParentItemId), Name = "IX_MenuItem_ParentItemId")]
    [Index(nameof(Published), Name = "IX_MenuItem_Published")]
    [Index(nameof(DisplayOrder), Name = "IX_MenuItem_DisplayOrder")]
    [Index(nameof(LimitedToStores), Name = "IX_MenuItem_LimitedToStores")]
    [Index(nameof(SubjectToAcl), Name = "IX_MenuItem_SubjectToAcl")]
    [LocalizedEntity("Published", KeyGroup = EntityName)]
    public class MenuItemEntity : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IAclRestricted
    {
        const string EntityName = "MenuItemRecord";

        public MenuItemEntity()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private MenuItemEntity(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        public override string GetEntityName()
            => EntityName;

        /// <summary>
        /// Gets or sets the menu identifier.
        /// </summary>
        [Required]
        public int MenuId { get; set; }

        private MenuEntity _menu;
        /// <summary>
        /// Gets the menu.
        /// </summary>
        [IgnoreDataMember]
        public MenuEntity Menu
        {
            get => _menu ?? LazyLoader.Load(this, ref _menu);
            set => _menu = value;
        }

        /// <summary>
        /// Gets or sets the parent menu item identifier. 0 if the item has no parent.
        /// </summary>
        public int ParentItemId { get; set; }

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        [StringLength(100)]
        public string ProviderName { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [MaxLength]
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the short description. It is used for the link title attribute.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets permission names.
        /// </summary>
        [MaxLength]
        public string PermissionNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the menu item is published.
        /// </summary>
        public bool Published { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the menu item has a divider or a group header.
        /// </summary>
        public bool BeginGroup { get; set; }

        /// <summary>
        /// If selected and this menu item has children, the menu will initially appear expanded.
        /// </summary>
        public bool ShowExpanded { get; set; }

        /// <summary>
        /// Gets or sets the no-follow link attribute.
        /// </summary>
        public bool NoFollow { get; set; }

        /// <summary>
        /// Gets or sets the blank target link attribute.
        /// </summary>
        public bool NewWindow { get; set; }

        /// <summary>
        /// Gets or sets fontawesome icon class.
        /// </summary>
        [StringLength(100)]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets fontawesome icon style.
        /// </summary>
        [StringLength(10)]
        public string Style { get; set; }

        /// <summary>
        /// Gets or sets fontawesome icon color.
        /// </summary>
        [StringLength(100)]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets HTML id attribute.
        /// </summary>
        [StringLength(100)]
        public string HtmlId { get; set; }

        /// <summary>
        /// Gets or sets the CSS class.
        /// </summary>
        [StringLength(100)]
        public string CssClass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL.
        /// </summary>
        public bool SubjectToAcl { get; set; }
    }
}
