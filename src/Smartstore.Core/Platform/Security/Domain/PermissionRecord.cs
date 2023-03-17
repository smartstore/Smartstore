using System.ComponentModel.DataAnnotations;
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
