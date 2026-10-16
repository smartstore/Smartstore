using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Scheduling;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Seed descriptors of the installation data in "App_Data/Seed". See the README there for the file format.
/// </summary>
internal static class InstallationSeedDescriptors
{
    public static SeedEntityDescriptor<MeasureDimension> MeasureDimensions()
        => new SeedEntityDescriptor<MeasureDimension>("systemKeyword").FromFiles("core/measure-dimensions.json");

    public static SeedEntityDescriptor<MeasureWeight> MeasureWeights()
        => new SeedEntityDescriptor<MeasureWeight>("systemKeyword").FromFiles("core/measure-weights.json");

    public static SeedEntityDescriptor<Currency> Currencies()
        => new SeedEntityDescriptor<Currency>("currencyCode").FromFiles("core/currencies.json");

    public static SeedEntityDescriptor<DeliveryTime> DeliveryTimes()
        => new SeedEntityDescriptor<DeliveryTime>("key").FromFiles("core/delivery-times.json");

    public static SeedEntityDescriptor<QuantityUnit> QuantityUnits()
        => new SeedEntityDescriptor<QuantityUnit>("key").FromFiles("core/quantity-units.json");

    public static SeedEntityDescriptor<PriceLabel> PriceLabels()
        => new SeedEntityDescriptor<PriceLabel>("key").FromFiles("core/price-labels.json");

    public static SeedEntityDescriptor<ShippingMethod> ShippingMethods(bool includeSamples)
        => new SeedEntityDescriptor<ShippingMethod>("key").FromFiles(WithSamples("shipping-methods.json", includeSamples));

    public static SeedEntityDescriptor<CustomerRole> CustomerRoles(bool includeSamples)
        => new SeedEntityDescriptor<CustomerRole>("systemName").FromFiles(WithSamples("customer-roles.json", includeSamples));

    public static SeedEntityDescriptor<EmailAccount> EmailAccounts()
        => new SeedEntityDescriptor<EmailAccount>("key").FromFiles("core/email-accounts.json");

    public static SeedEntityDescriptor<ActivityLogType> ActivityLogTypes()
        => new SeedEntityDescriptor<ActivityLogType>("systemKeyword").FromFiles("core/activity-log-types.json");

    public static SeedEntityDescriptor<TaskDescriptor> TaskDescriptors()
        => new SeedEntityDescriptor<TaskDescriptor>("type").FromFiles("core/tasks.json");

    public static SeedEntityDescriptor<ProductTemplate> ProductTemplates()
        => new SeedEntityDescriptor<ProductTemplate>("viewPath").FromFiles("core/product-templates.json");

    public static SeedEntityDescriptor<CategoryTemplate> CategoryTemplates()
        => new SeedEntityDescriptor<CategoryTemplate>("viewPath").FromFiles("core/category-templates.json");

    public static SeedEntityDescriptor<ManufacturerTemplate> ManufacturerTemplates()
        => new SeedEntityDescriptor<ManufacturerTemplate>("viewPath").FromFiles("core/manufacturer-templates.json");

    /// <summary>
    /// Tax categories. The seed-only field "rate" (fixed tax rate, default 0) is collected in <paramref name="rates"/>.
    /// </summary>
    /// <param name="rates">
    /// Receives the rates. Must compare by reference (<see cref="ReferenceEqualityComparer"/>), because entity
    /// equality depends on the ID, which changes on save.
    /// </param>
    public static SeedEntityDescriptor<TaxCategory> TaxCategories(IDictionary<TaxCategory, decimal> rates)
    {
        Guard.NotNull(rates);

        return new SeedEntityDescriptor<TaxCategory>("key")
            .FromFiles("core/tax-categories.json")
            .Bind("rate", (category, node, _) => rates[category] = node.GetValue<decimal>());
    }

    /// <summary>
    /// Countries with their states. States can be referenced as "{countryKey}/{stateKey}", e.g. "DE/BW".
    /// </summary>
    public static SeedEntityDescriptor<Country> Countries()
        => new SeedEntityDescriptor<Country>("twoLetterIsoCode")
            .FromFiles("core/countries.json")
            .Child("stateProvinces", x => x.StateProvinces, new SeedEntityDescriptor<StateProvince>("abbreviation"));

    /// <summary>
    /// The core file, plus the samples file when sample data is installed.
    /// </summary>
    private static string[] WithSamples(string fileName, bool includeSamples)
        => includeSamples ? [$"core/{fileName}", $"samples/{fileName}"] : [$"core/{fileName}"];
}
