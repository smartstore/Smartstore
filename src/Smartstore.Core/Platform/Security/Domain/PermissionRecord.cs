using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a permission record.
    /// </summary>
    [Index(nameof(SystemName), Name = "IX_SystemName")]
    [CacheableEntity]
    public partial class PermissionRecord : EntityWithAttributes
    {
        public PermissionRecord()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PermissionRecord(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
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
            get => _permissionRoleMappings ?? LazyLoader.Load(this, ref _permissionRoleMappings) ?? (_permissionRoleMappings ??= new HashSet<PermissionRoleMapping>());
            protected set => _permissionRoleMappings = value;
        }
    }
}
