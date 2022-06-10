using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a permission to role mapping.
    /// </summary>
    public partial class PermissionRoleMapping : BaseEntity
    {
        public PermissionRoleMapping()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PermissionRoleMapping(ILazyLoader lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the permission is granted.
        /// </summary>
        public bool Allow { get; set; }

        /// <summary>
        /// Gets or sets the permission record identifier.
        /// </summary>
        public int PermissionRecordId { get; set; }

        private PermissionRecord _permissionRecord;
        /// <summary>
        /// Gets or sets the permission record.
        /// </summary>
        [ForeignKey("PermissionRecordId")]
        public PermissionRecord PermissionRecord
        {
            get => _permissionRecord ?? LazyLoader.Load(this, ref _permissionRecord);
            set => _permissionRecord = value;
        }

        /// <summary>
        /// Gets or sets the customer role identifier.
        /// </summary>
        public int CustomerRoleId { get; set; }

        private CustomerRole _customerRole;
        /// <summary>
        /// Gets or sets the customer role.
        /// </summary>
        [ForeignKey("CustomerRoleId")]
        public CustomerRole CustomerRole
        {
            get => _customerRole ?? LazyLoader.Load(this, ref _customerRole);
            set => _customerRole = value;
        }
    }
}
