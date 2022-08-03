using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// Represents a menu.
    /// </summary>
    [Table("MenuRecord")]
    [Index(nameof(SystemName), nameof(IsSystemMenu), Name = "IX_Menu_SystemName_IsSystemMenu")]
    [Index(nameof(Published), Name = "IX_Menu_Published")]
    [Index(nameof(LimitedToStores), Name = "IX_Menu_LimitedToStores")]
    [Index(nameof(SubjectToAcl), Name = "IX_Menu_SubjectToAcl")]
    public class MenuEntity : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IAclRestricted
    {
        const string EntityName = "MenuRecord";

        public MenuEntity()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private MenuEntity(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        public override string GetEntityName()
            => EntityName;

        /// <summary>
        /// Gets or sets the system name. It identifies the menu.
        /// </summary>
        [Required, StringLength(400)]
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this menu is deleteable by a user.
        /// </summary>
        public bool IsSystemMenu { get; set; }

        /// <summary>
        /// Gets or sets the menu template name.
        /// </summary>
        [StringLength(400)]
        public string Template { get; set; }

        /// <summary>
        /// Gets or sets the widget zone name.
        /// </summary>
        [StringLength(4000)]
        public string WidgetZone { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [StringLength(400)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the menu is published.
        /// </summary>
        public bool Published { get; set; } = true;

        /// <summary>
        /// Gets or sets the order for widget registration.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL.
        /// </summary>
        public bool SubjectToAcl { get; set; }

        private ICollection<MenuItemEntity> _items;
        /// <summary>
        /// /// Gets or sets the menu items.
        /// </summary>
        public ICollection<MenuItemEntity> Items
        {
            get => _items ?? LazyLoader.Load(this, ref _items) ?? (_items ??= new HashSet<MenuItemEntity>());
            protected set => _items = value;
        }

        public IEnumerable<string> GetWidgetZones()
        {
            if (WidgetZone.IsEmpty())
            {
                return Enumerable.Empty<string>();
            }

            return WidgetZone.EmptyNull().Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
        }
    }
}
