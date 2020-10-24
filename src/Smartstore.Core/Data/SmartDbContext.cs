using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Customers;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Data
{
    public abstract class AsyncDbSaveHook<TEntity> : AsyncDbSaveHook<SmartDbContext, TEntity>
        where TEntity : class
    {
    }

    public abstract class DbSaveHook<TEntity> : DbSaveHook<SmartDbContext, TEntity>
        where TEntity : class
    {
    }

    public class SmartDbContext : HookingDbContext
    {
        public SmartDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<DeliveryTime> DeliveryTimes { get; set; }
        public DbSet<GenericAttribute> GenericAttributes { get; set; }
        public DbSet<MeasureDimension> MeasureDimensions { get; set; }
        public DbSet<MeasureWeight> MeasureWeights { get; set; }
        public DbSet<QuantityUnit> QuantityUnits { get; set; }
        public DbSet<StateProvince> StateProvinces { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreMapping> StoreMappings { get; set; }
        public DbSet<UrlRecord> UrlRecords { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ActivityLogType> ActivityLogTypes { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LocaleStringResource> LocaleStringResources { get; set; }

        //public IDbQueryFilters<SmartDbContext> QueryFilters
        //{
        //    get => new DbQueryFilters<SmartDbContext>(this);
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ???
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CreateModel(modelBuilder, typeof(SmartDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
