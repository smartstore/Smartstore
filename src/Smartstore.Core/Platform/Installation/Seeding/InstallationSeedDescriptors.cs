using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Stores;
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
    /// Topics. The seed-only field "slug" overrides the slug derived from the system name and is collected in <paramref name="slugs"/>.
    /// </summary>
    /// <param name="slugs">Receives slug overrides. Must compare by reference (<see cref="ReferenceEqualityComparer"/>).</param>
    public static SeedEntityDescriptor<Topic> Topics(IDictionary<Topic, string> slugs)
    {
        Guard.NotNull(slugs);

        return new SeedEntityDescriptor<Topic>("systemName")
            .FromFiles("core/topics.json")
            .Bind("slug", (topic, node, _) => slugs[topic] = node.GetValue<string>());
    }

    /// <summary>
    /// The default store. URL and SSL are not seed data; they are taken from the current request.
    /// </summary>
    /// <param name="mediaFileId">Gets the ID of a media file by its path below "media/".</param>
    public static SeedEntityDescriptor<Store> Stores(Func<string, int> mediaFileId)
    {
        Guard.NotNull(mediaFileId);

        return new SeedEntityDescriptor<Store>("key")
            .FromFiles("core/stores.json")
            .Reference<Currency>("defaultCurrency", (store, currency) => store.DefaultCurrencyId = currency.Id)
            .Reference<Currency>("primaryExchangeRateCurrency", (store, currency) => store.PrimaryExchangeRateCurrencyId = currency.Id)
            .Bind("logoMediaFile", (store, node, _) => store.LogoMediaFileId = mediaFileId(node.GetValue<string>()));
    }

    /// <summary>
    /// Built-in system customers (search engine, background task, PDF converter, webhook client).
    /// </summary>
    public static SeedEntityDescriptor<Customer> SystemCustomers()
        => new SeedEntityDescriptor<Customer>("systemName")
            .FromFiles("core/system-customers.json")
            .OnCreated((customer, _) =>
            {
                customer.CustomerGuid = Guid.NewGuid();
                customer.CreatedOnUtc = DateTime.UtcNow;
                customer.LastActivityDateUtc = DateTime.UtcNow;
            });

    /// <summary>
    /// Address of the admin user.
    /// </summary>
    public static SeedEntityDescriptor<Address> AdminAddress()
        => new SeedEntityDescriptor<Address>("key")
            .FromFiles("core/admin-address.json")
            .Reference<Country>("country", (address, country) => address.Country = country)
            .Reference<StateProvince>("stateProvince", (address, state) => address.StateProvince = state)
            .OnCreated((address, _) => address.CreatedOnUtc = DateTime.UtcNow);

    /// <summary>
    /// Menus with their items. Titles may reference locale resources. The seed-only field "publishedWhen"
    /// of an item sets "published" from a condition evaluated at installation time.
    /// </summary>
    /// <param name="condition">Evaluates a condition like "manufacturers" or "setting:CatalogSettings.CompareProductsEnabled".</param>
    public static SeedEntityDescriptor<MenuEntity> Menus(Func<string, bool> condition)
    {
        Guard.NotNull(condition);

        var items = new SeedEntityDescriptor<MenuItemEntity>("key")
            .Bind("publishedWhen", (item, node, _) => item.Published = condition(node.GetValue<string>()));

        return new SeedEntityDescriptor<MenuEntity>("systemName")
            .FromFiles("core/menus.json")
            .Child("items", x => x.Items, items);
    }

    /// <summary>
    /// The core file, plus the samples file when sample data is installed.
    /// </summary>
    private static string[] WithSamples(string fileName, bool includeSamples)
        => includeSamples ? [$"core/{fileName}", $"samples/{fileName}"] : [$"core/{fileName}"];
}
