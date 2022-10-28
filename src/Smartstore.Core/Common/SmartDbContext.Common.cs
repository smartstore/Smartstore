using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<DeliveryTime> DeliveryTimes { get; set; }
        public DbSet<GenericAttribute> GenericAttributes { get; set; }
        public DbSet<MeasureDimension> MeasureDimensions { get; set; }
        public DbSet<MeasureWeight> MeasureWeights { get; set; }
        public DbSet<QuantityUnit> QuantityUnits { get; set; }
        public DbSet<StateProvince> StateProvinces { get; set; }
        public DbSet<PriceLabel> PriceLabels { get; set; }
    }
}
