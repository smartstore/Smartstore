using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Checkout.Attributes;

internal class CheckoutAttributeMap : IEntityTypeConfiguration<CheckoutAttribute>
{
    public void Configure(EntityTypeBuilder<CheckoutAttribute> builder)
    {
        builder
            .HasMany(x => x.RuleSets)
            .WithMany(x => x.CheckoutAttributes)
            .UsingEntity<Dictionary<string, object>>(
                "RuleSet_CheckoutAttribute_Mapping",
                c => c
                    .HasOne<RuleSetEntity>()
                    .WithMany()
                    .HasForeignKey("RuleSetEntity_Id")
                    .HasConstraintName("FK_RuleSet_CheckoutAttribute_RuleSetEntity_Id")
                    .OnDelete(DeleteBehavior.Cascade),
                c => c
                    .HasOne<CheckoutAttribute>()
                    .WithMany()
                    .HasForeignKey("CheckoutAttribute_Id")
                    .HasConstraintName("FK_RuleSet_CheckoutAttribute_CheckoutAttribute_Id")
                    .OnDelete(DeleteBehavior.Cascade),
                c =>
                {
                    c.HasIndex("CheckoutAttribute_Id");
                    c.HasKey("CheckoutAttribute_Id", "RuleSetEntity_Id");
                });
    }
}

/// <summary>
/// Represents a checkout attribute
/// </summary>
[CacheableEntity]
[LocalizedEntity("IsActive")]
public partial class CheckoutAttribute : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IRulesContainer
{
    /// <summary>
    /// Gets or sets a value indicating whether the checkout attribute is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    [Required, MaxLength(400)]
    [LocalizedProperty]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the text prompt
    /// </summary>
    [LocalizedProperty]
    public string TextPrompt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shippable products are required in order to display this attribute
    /// </summary>
    public bool ShippableProductRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the attribute is marked as tax exempt
    /// </summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>
    /// Gets or sets the tax category identifier
    /// </summary>
    public int TaxCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    /// <summary>
    /// Gets or sets the attribute control type identifier
    /// </summary>
    public int AttributeControlTypeId { get; set; }

    /// <summary>
    /// Gets the attribute control type
    /// </summary>
    [NotMapped]
    public AttributeControlType AttributeControlType
    {
        get => (AttributeControlType)AttributeControlTypeId;
        set => AttributeControlTypeId = (int)value;
    }

    /// <summary>
    /// Gets a value indicating whether the attribute has a list of values.
    /// </summary>
    public bool IsListTypeAttribute => AttributeControlType switch
    {
        AttributeControlType.Checkboxes
        or AttributeControlType.Boxes
        or AttributeControlType.DropdownList
        or AttributeControlType.RadioList => true,
        _ => false
    };

    /// <summary>
    /// Gets or sets the swatch size.
    /// <c>null</c> means "use the global default" which defaults to <see cref="SwatchSize.Medium"/>.
    /// </summary>
    public SwatchSize? SwatchSize { get; set; }

    /// <summary>
    /// Gets or sets the swatch aspect ratio.
    /// Controls the rendered swatch width-to-height ratio.
    /// Default is 1.0 (square).
    /// </summary>
    public decimal SwatchAspectRatio { get; set; } = 1m;

    /// <summary>
    /// Gets or sets the swatch shape.
    /// <c>null</c> means "use the global default" which defaults to <see cref="SwatchShape.Rounded"/>.
    /// </summary>
    public SwatchShape? SwatchShape { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value name is shown in the swatch.
    /// If <c>true</c>, shows the option text together with the rendered swatch. Default is <c>false</c>.
    /// </summary>
    public bool ShowValueNameInSwatch { get; set; }

    /// <summary>
    /// Gets or sets which price information is displayed in the swatch.
    /// Default is <see cref="SwatchPriceDisplayMode.None"/>. <see cref="SwatchPriceDisplayMode.FinalPrice"/> is not applicable.
    /// </summary>
    public SwatchPriceDisplayMode SwatchPriceDisplay { get; set; }

    private ICollection<RuleSetEntity> _ruleSets;
    /// <summary>
    /// Gets or sets assigned rule sets.
    /// </summary>
    [IgnoreDataMember]
    public ICollection<RuleSetEntity> RuleSets
    {
        get => _ruleSets ?? LazyLoader.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
        protected set => _ruleSets = value;
    }

    private ICollection<CheckoutAttributeValue> _checkoutAttributeValues;
    /// <summary>
    /// Gets or sets the checkout attributes collection
    /// </summary>
    public ICollection<CheckoutAttributeValue> CheckoutAttributeValues
    {
        get => _checkoutAttributeValues ?? LazyLoader.Load(this, ref _checkoutAttributeValues) ?? (_checkoutAttributeValues ??= new HashSet<CheckoutAttributeValue>());
        protected set => _checkoutAttributeValues = value;
    }
}