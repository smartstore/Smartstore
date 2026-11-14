using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing;

internal class ProductStorePriceMap : IEntityTypeConfiguration<ProductStorePrice>
{
    public void Configure(EntityTypeBuilder<ProductStorePrice> builder)
    {
        builder.HasIndex(x => new { x.ProductId, x.StoreId })
            .IsUnique()
            .HasDatabaseName("UX_ProductStorePrice_Product_Store");

        builder.Property(x => x.Price).HasPrecision(18, 4);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// An optional product price entered directly in a store's default currency.
/// The currency identity is persisted to prevent reinterpreting a saved price
/// when the store's default currency changes.
/// </summary>
public class ProductStorePrice : BaseEntity
{
    /// <summary>The product to which this override applies.</summary>
    public int ProductId { get; set; }

    /// <summary>The store to which this override applies.</summary>
    public int StoreId { get; set; }

    /// <summary>Currency in which the price was entered.</summary>
    public int CurrencyId { get; set; }

    /// <summary>The exact merchant-entered price in the named currency, with the same tax-inclusion convention as Product.Price.</summary>
    public decimal Price { get; set; }

    /// <summary>The currency entity associated with the saved price.</summary>
    public Currency Currency { get; set; }
}
