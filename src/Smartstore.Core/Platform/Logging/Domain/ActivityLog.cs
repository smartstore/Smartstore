using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Identity;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Logging
{
    internal class ActivityLogMap : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder
                .HasOne(x => x.ActivityLogType)
                .WithOne(navigationName: null)
                .HasForeignKey<ActivityLog>(x => x.ActivityLogTypeId);

            builder
                .HasOne(x => x.Customer)
                .WithOne(navigationName: null)
                .IsRequired(false)
                .HasForeignKey<ActivityLog>(x => x.CustomerId);
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
        [JsonIgnore]
        public Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }
    }
}