using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Logging
{
    internal class ActivityLogMap : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder
                .HasOne(c => c.ActivityLogType)
                .WithMany()
                .HasForeignKey(c => c.ActivityLogTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an activity log record
    /// </summary>
    [Hookable(false)]
    [Index(nameof(CreatedOnUtc), Name = "IX_ActivityLog_CreatedOnUtc")]
    [CacheableEntity(NeverCache = true)]
    public partial class ActivityLog : BaseEntity
    {
        public ActivityLog()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ActivityLog(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the activity log type identifier
        /// </summary>
        public int ActivityLogTypeId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the activity comment
        /// </summary>
        [Required, MaxLength()]
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private ActivityLogType _activityLogType;
        /// <summary>
        /// Gets the activity log type
        /// </summary>
        public ActivityLogType ActivityLogType
        {
            get => _activityLogType ?? LazyLoader.Load(this, ref _activityLogType);
            set => _activityLogType = value;
        }

        private Customer _customer;
        /// <summary>
        /// Gets the customer
        /// </summary>
        [IgnoreDataMember]
        public Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }
    }
}