using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Engine;
using Pomelo.EntityFrameworkCore.MySql.Extensions;

namespace Smartstore.Data.SqlServer
{
    //internal class ProductSpecificationAttributeMap2 : IEntityTypeConfiguration<ProductSpecificationAttribute>
    //{
    //    public void Configure(EntityTypeBuilder<ProductSpecificationAttribute> builder)
    //    {
    //        builder.HasOne(c => c.SpecificationAttributeOption)
    //            .WithMany(c => c.ProductSpecificationAttributes)
    //            .HasForeignKey(c => c.SpecificationAttributeOptionId);

    //        builder.HasOne(c => c.Product)
    //            .WithMany(c => c.ProductSpecificationAttributes)
    //            .HasForeignKey(c => c.ProductId)
    //            .IsRequired(false);

    //        builder
    //            .HasIndex(x => x.AllowFiltering, "IX_PSAM_AllowFiltering")
    //            .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId), nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId));

    //        builder
    //            .HasIndex(new[] { nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId), nameof(ProductSpecificationAttribute.AllowFiltering) }, "IX_PSAM_SpecificationAttributeOptionId_AllowFiltering")
    //            .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId));
    //    }
    //}


    internal class MySqlDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.MySql;

        public override Type SmartDbContextType => typeof(MySqlSmartDbContext);

        public override DataProvider CreateDataProvider(DatabaseFacade database)
        {
            return new MySqlDataProvider(database);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString, IApplicationContext appContext)
        {
            //// Add-Migration Initial -Context MySqlSmartDbContext -Project Smartstore.Data.MySql
            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySql =>
            {
                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }
    }
}
