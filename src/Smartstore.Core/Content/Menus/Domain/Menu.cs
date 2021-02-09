using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Domain;

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
    public class Menu : BaseEntity, ILocalizedEntity, IStoreRestricted, IAclRestricted
    {
        private readonly ILazyLoader _lazyLoader;

        public Menu()
        {
        }

        public Menu(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

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

        private ICollection<MenuItem> _items;
        /// <summary>
        /// /// Gets or sets the menu items.
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<MenuItem> Items
        {
            get => _lazyLoader?.Load(this, ref _items) ?? (_items ??= new HashSet<MenuItem>());
            protected set => _items = value;
        }
    }
}
