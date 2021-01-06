using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Data.Caching;
using Smartstore.Domain;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a permission record.
    /// </summary>
    [Index(nameof(SystemName), Name = "IX_SystemName")]
    [CacheableEntity]
    public partial class PermissionRecord : EntityWithAttributes
    {
        private readonly ILazyLoader _lazyLoader;

        public PermissionRecord()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PermissionRecord(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the permission system name.
        /// </summary>
        [Required, StringLength(255)]
        public string SystemName { get; set; }

        private ICollection<PermissionRoleMapping> _permissionRoleMappings;
        /// <summary>
        /// Gets or sets permission role mappings.
        /// </summary>
        public ICollection<PermissionRoleMapping> PermissionRoleMappings
        {
            get => _lazyLoader?.Load(this, ref _permissionRoleMappings) ?? (_permissionRoleMappings ??= new HashSet<PermissionRoleMapping>());
            protected set => _permissionRoleMappings = value;
        }
    }
}
