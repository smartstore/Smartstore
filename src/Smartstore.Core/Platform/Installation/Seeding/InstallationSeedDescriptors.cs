using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using System.Text.Json.Nodes;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules;
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
    /// <param name="media">Creates a saved media file for a media reference.</param>
    public static SeedEntityDescriptor<Store> Stores(Func<JsonNode, MediaFile> media)
    {
        Guard.NotNull(media);

        return new SeedEntityDescriptor<Store>("key")
            .FromFiles("core/stores.json")
            .Reference<Currency>("defaultCurrency", (store, currency) => store.DefaultCurrencyId = currency.Id)
            .Reference<Currency>("primaryExchangeRateCurrency", (store, currency) => store.PrimaryExchangeRateCurrencyId = currency.Id)
            .Bind("logoMediaFile", (store, node, _) => store.LogoMediaFileId = media(node).Id);
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

    #region Samples

    /// <summary>
    /// Specification attributes with their options. Options can be referenced as "{attributeKey}/{optionKey}".
    /// </summary>
    public static SeedEntityDescriptor<SpecificationAttribute> SpecificationAttributes()
        => new SeedEntityDescriptor<SpecificationAttribute>("key")
            .FromFiles("samples/catalog/specification-attributes.json")
            .Child("options", x => x.SpecificationAttributeOptions, new SeedEntityDescriptor<SpecificationAttributeOption>("key"));

    public static SeedEntityDescriptor<ProductAttribute> ProductAttributes()
        => new SeedEntityDescriptor<ProductAttribute>("alias").FromFiles("samples/catalog/product-attributes.json");

    /// <summary>
    /// Product attribute option sets with their options.
    /// </summary>
    public static SeedEntityDescriptor<ProductAttributeOptionsSet> ProductAttributeOptionsSets()
        => new SeedEntityDescriptor<ProductAttributeOptionsSet>("key")
            .FromFiles("samples/catalog/product-attribute-option-sets.json")
            .Reference<ProductAttribute>("productAttribute", (set, attribute) => set.ProductAttributeId = attribute.Id)
            .Child("options", x => x.ProductAttributeOptions, new SeedEntityDescriptor<ProductAttributeOption>("alias"));

    public static SeedEntityDescriptor<Campaign> Campaigns()
        => new SeedEntityDescriptor<Campaign>("key")
            .FromFiles("samples/campaigns.json")
            .OnCreated((campaign, _) => campaign.CreatedOnUtc = DateTime.UtcNow);

    /// <summary>
    /// Rule sets with their rules, and the shipping methods and customer roles they apply to.
    /// </summary>
    public static SeedEntityDescriptor<RuleSetEntity> RuleSets()
        => new SeedEntityDescriptor<RuleSetEntity>("key")
            .FromFiles("samples/rule-sets.json")
            .Child("rules", x => x.Rules, new SeedEntityDescriptor<RuleEntity>("ruleType"))
            .References<ShippingMethod>("shippingMethods", (ruleSet, methods) => methods.Each(ruleSet.ShippingMethods.Add))
            .References<CustomerRole>("customerRoles", (ruleSet, roles) => roles.Each(ruleSet.CustomerRoles.Add));

    public static SeedEntityDescriptor<Discount> Discounts()
        => new SeedEntityDescriptor<Discount>("key")
            .FromFiles("samples/discounts.json")
            .References<RuleSetEntity>("ruleSets", (discount, ruleSets) => ruleSets.Each(discount.RuleSets.Add));

    /// <summary>
    /// Categories with parent, picture, rule sets and applied discounts.
    /// </summary>
    /// <param name="media">Creates a media file from a media reference (path or { file, name }).</param>
    public static SeedEntityDescriptor<Category> Categories(Func<JsonNode, MediaFile> media)
    {
        Guard.NotNull(media);

        return new SeedEntityDescriptor<Category>("alias")
            .FromFiles("samples/catalog/categories.json")
            .Reference<Category>("parent", (category, parent) => category.Parent = parent)
            .Bind("mediaFile", (category, node, _) => category.MediaFile = media(node))
            .References<RuleSetEntity>("ruleSets", (category, ruleSets) => ruleSets.Each(category.RuleSets.Add))
            .References<Discount>("discounts", (category, discounts) => discounts.Each(category.AppliedDiscounts.Add));
    }

    /// <summary>
    /// Manufacturers with picture and applied discounts.
    /// </summary>
    /// <param name="media">Creates a media file from a media reference (path or { file, name }).</param>
    public static SeedEntityDescriptor<Manufacturer> Manufacturers(Func<JsonNode, MediaFile> media)
    {
        Guard.NotNull(media);

        return new SeedEntityDescriptor<Manufacturer>("key")
            .FromFiles("samples/catalog/manufacturers.json")
            .Bind("mediaFile", (manufacturer, node, _) => manufacturer.MediaFile = media(node))
            .References<Discount>("discounts", (manufacturer, discounts) => discounts.Each(manufacturer.AppliedDiscounts.Add));
    }

    public static SeedEntityDescriptor<ProductTag> ProductTags()
        => new SeedEntityDescriptor<ProductTag>("key").FromFiles("samples/catalog/product-tags.json");

    #endregion

    /// <summary>
    /// Parses a media reference: a path below the media folder, or { "file": path, "name": seoName }.
    /// </summary>
    public static (string Path, string Name) ParseMediaReference(JsonNode node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var path) && path.HasValue())
        {
            return (path, null);
        }

        if (node is JsonObject obj && obj["file"] is JsonValue file && file.TryGetValue<string>(out path) && path.HasValue())
        {
            return (path, obj["name"]?.GetValue<string>());
        }

        throw new SeedDataFormatException("A media reference must be a path or an object with \"file\" and optional \"name\".");
    }

    /// <summary>
    /// The core file, plus the samples file when sample data is installed.
    /// </summary>
    private static string[] WithSamples(string fileName, bool includeSamples)
        => includeSamples ? [$"core/{fileName}", $"samples/{fileName}"] : [$"core/{fileName}"];
}
