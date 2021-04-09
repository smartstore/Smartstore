using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Data.SqlServer
{
    // TODO: (core) Make custom mappers for MySql (and Sqlite ??) also
    
    internal class StateProvinceMap : IEntityTypeConfiguration<StateProvince>
    {
        public void Configure(EntityTypeBuilder<StateProvince> builder)
        {
            builder
                .HasIndex(x => x.CountryId)
                .HasDatabaseName("IX_StateProvince_CountryId")
                .IncludeProperties(x => new { x.DisplayOrder });
        }
    }

    internal class ProductSpecificationAttributeMap : IEntityTypeConfiguration<ProductSpecificationAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductSpecificationAttribute> builder)
        {
            builder
                .HasIndex(x => x.AllowFiltering, "IX_PSAM_AllowFiltering")
                .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId), nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId));

            builder
                .HasIndex(new[] { nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId), nameof(ProductSpecificationAttribute.AllowFiltering) }, "IX_PSAM_SpecificationAttributeOptionId_AllowFiltering")
                .IncludeProperties(nameof(ProductSpecificationAttribute.ProductId));
        }
    }

    internal class LocalizedPropertyMap : IEntityTypeConfiguration<LocalizedProperty>
    {
        public void Configure(EntityTypeBuilder<LocalizedProperty> builder)
        {
            builder
                .HasIndex(x => x.Id)
                .HasDatabaseName("IX_LocalizedProperty_Key")
                .IncludeProperties(nameof(LocalizedProperty.EntityId), nameof(LocalizedProperty.LocaleKeyGroup), nameof(LocalizedProperty.LocaleKey));
        }
    }
}
