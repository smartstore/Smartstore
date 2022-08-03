using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Data.SqlServer
{
    internal class StateProvinceMap : IEntityTypeConfiguration<StateProvince>
    {
        public void Configure(EntityTypeBuilder<StateProvince> builder)
        {
            builder.HasIndex(x => x.CountryId).HasDatabaseName("IX_StateProvince_CountryId");
        }
    }

    internal class ProductSpecificationAttributeMap : IEntityTypeConfiguration<ProductSpecificationAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductSpecificationAttribute> builder)
        {
            builder.HasIndex(x => x.AllowFiltering, "IX_PSAM_AllowFiltering");
            builder.HasIndex(x => new { x.SpecificationAttributeOptionId, x.AllowFiltering }, "IX_PSAM_SpecificationAttributeOptionId_AllowFiltering");
        }
    }

    internal class LocalizedPropertyMap : IEntityTypeConfiguration<LocalizedProperty>
    {
        public void Configure(EntityTypeBuilder<LocalizedProperty> builder)
        {
            builder.HasIndex(x => x.Id).HasDatabaseName("IX_LocalizedProperty_Key");
        }
    }
}
