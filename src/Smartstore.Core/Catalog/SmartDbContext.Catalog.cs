using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }

        public DbSet<ProductAttribute> Variants { get; set; }
        public DbSet<ProductVariantAttribute> ProductVariants { get; set; }
        public DbSet<ProductVariantAttributeValue> ProductVariantValues { get; set; }
        public DbSet<ProductVariantAttributeCombination> ProductVariantCombinations { get; set; }

        public DbSet<ProductAttributeOptionsSet> VariantSets { get; set; }
        public DbSet<ProductAttributeOption> VariantSetOptions { get; set; }

        public DbSet<SpecificationAttribute> Attributes { get; set; }
        public DbSet<SpecificationAttributeOption> AttributeOptions { get; set; }
        public DbSet<ProductSpecificationAttribute> ProductAttributes { get; set; }

        public DbSet<Discount> Discounts { get; set; }
    }
}