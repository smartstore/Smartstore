using System.Text.Json;
using System.Text.Json.Nodes;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Installation.Seeding;
using Smartstore.Core.Seo;
using Smartstore.Json;

namespace Smartstore.Core.Installation;

public partial class InstallationDataSeeder
{
    private const string ProductFiles = "samples/catalog/products/*.json";
    private const string ProductDefaultsFile = "samples/catalog/products/_defaults.json";
    private const string AttributeValueNamesFile = "samples/catalog/attribute-value-names.json";

    /// <summary>
    /// Parts of product records that can only be created after all products have been saved,
    /// because they reference other products or need product IDs.
    /// </summary>
    private sealed class ProductSeedState
    {
        // By reference: entity equality depends on the ID, which changes on save.
        public Dictionary<Product, Dictionary<string, MediaFile>> Pictures { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<string, MediaFile> SeparateMedia { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(Product Product, JsonObject Picture)> SharedPictures { get; } = [];
        public List<(Product Product, string Path)> SampleDownloads { get; } = [];
        public List<(Product Product, string ParentSku)> GroupedParents { get; } = [];
        public List<(Product Product, JsonArray Items, string Source)> BundleItems { get; } = [];
        public List<(Product Product, JsonArray Attributes, string Source)> VariantAttributes { get; } = [];
        public List<(Product Product, JsonArray Combinations, string Source)> Combinations { get; } = [];
    }

    /// <summary>
    /// Seeds tags and products in the same phases as the former C# seed data:
    /// products, downloads and grouped products, bundle items, variant attributes, combinations.
    /// </summary>
    private async Task PopulateProductsFromSeedAsync()
    {
        var state = new ProductSeedState();

        await PopulateFromSeedAsync("PopulateProductTags", InstallationSeedDescriptors.ProductTags());

        await PopulateAsync("PopulateProducts", async () =>
        {
            var products = CreateFromSeed(ProductDescriptor(state));
            await SaveRangeAsync(products);

            // Sample downloads.
            foreach (var (product, path) in state.SampleDownloads)
            {
                product.SampleDownload = new Download
                {
                    EntityId = product.Id,
                    EntityName = nameof(Product),
                    DownloadGuid = Guid.NewGuid(),
                    MediaFile = CreateSampleMedia(path, null),
                    UpdatedOnUtc = DateTime.UtcNow
                };
            }

            await Context.SaveChangesAsync();

            await PopulateUrlRecordsFor(products, x => CreateUrlRecord(x, null, x.Name));

            // Grouped products and pictures shared with another product.
            foreach (var (product, parentSku) in state.GroupedParents)
            {
                product.ParentGroupedProductId = _seedResolver.GetId<Product>(parentSku);
            }

            foreach (var (product, picture) in state.SharedPictures)
            {
                var from = _seedResolver.Get<Product>(picture["from"].GetValue<string>());
                var path = picture["file"].GetValue<string>();

                if (!state.Pictures.TryGetValue(from, out var pictures) || !pictures.TryGetValue(path, out var mediaFile))
                {
                    throw new SeedDataFormatException($"Product '{product.Sku}': product '{from.Sku}' has no picture '{path}'.");
                }

                product.ProductMediaFiles.Add(new ProductMediaFile
                {
                    MediaFileId = mediaFile.Id,
                    DisplayOrder = picture["displayOrder"]?.GetValue<int>() ?? 1
                });
            }

            await Context.SaveChangesAsync();

            // Fix MainPictureId
            await ProductPictureHelper.FixProductMainPictureIds(Context);
        });

        await PopulateAsync("PopulateProductBundleItems", async () =>
        {
            var items = new List<ProductBundleItem>();
            foreach (var (bundle, records, source) in state.BundleItems)
            {
                var descriptor = new SeedEntityDescriptor<ProductBundleItem>("product")
                    .Reference<Product>("product", (item, product) => item.Product = product)
                    .OnCreated((item, _) => item.BundleProduct = bundle);

                items.AddRange(records.Select(x => _seedFactory.Create(ChildRecord(bundle, x, "product", source), descriptor)));
            }

            await SaveRangeAsync(items);
        });

        await PopulateAsync("PopulateProductVariantAttributes", async () =>
        {
            var attributes = new List<ProductVariantAttribute>();
            var visibilityRules = new List<(Product Product, ProductVariantAttribute Attribute, JsonObject Conditions)>();
            foreach (var (product, records, source) in state.VariantAttributes)
            {
                var values = new SeedEntityDescriptor<ProductVariantAttributeValue>("alias")
                    .Bind("mediaFile", (value, node, _) => value.MediaFileId = ResolveProductMedia(product, node, state).Id)
                    .Bind("additionalColors", (value, node, _) => value.AdditionalColors = node.Deserialize<string[]>());

                var descriptor = new SeedEntityDescriptor<ProductVariantAttribute>("productAttribute")
                    .Reference<ProductAttribute>("productAttribute", (pva, attribute) => pva.ProductAttribute = attribute)
                    .Child("values", x => x.ProductVariantAttributeValues, values)
                    .Bind("visibleWhen", (pva, node, _) => visibilityRules.Add((product, pva, node.AsObject())))
                    .OnCreated((pva, _) => pva.Product = product);

                attributes.AddRange(records.Select(x => _seedFactory.Create(ChildRecord(product, x, "productAttribute", source), descriptor)));
            }

            await SaveRangeAsync(attributes);

            if (visibilityRules.Count > 0)
            {
                Context.RuleSets.AddRange(visibilityRules.Select(x => CreateVisibilityRuleSet(x.Product, x.Attribute, x.Conditions)));
                await Context.SaveChangesAsync();
            }
        });

        await PopulateAsync("ProductVariantAttributeCombinations", async () =>
        {
            var combinations = new List<ProductVariantAttributeCombination>();
            foreach (var (product, records, source) in state.Combinations)
            {
                var descriptor = new SeedEntityDescriptor<ProductVariantAttributeCombination>("sku")
                    .Reference<DeliveryTime>("deliveryTime", (combination, deliveryTime) => combination.DeliveryTimeId = deliveryTime.Id)
                    .Bind("attributes", (combination, node, _) => combination.RawAttributes = CreateAttributeSelection(product, node))
                    .Bind("pictures", (combination, node, _) => combination.AssignedMediaFileIds = string.Join(",",
                        node.AsArray().Select(x => ResolveProductMedia(product, x, state).Id)))
                    .OnCreated((combination, _) => combination.Product = product);

                // Not registered: combinations are never referenced by other seed records.
                combinations.AddRange(records.Select(x => _seedFactory.Create(new SeedRecord(x["sku"].GetValue<string>(), x.AsObject(), source), descriptor, register: false)));
            }

            await SaveRangeAsync(combinations);

            // Derived product property, maintained by a hook at runtime that does not run during installation.
            foreach (var group in combinations.Where(x => x.IsActive && x.Price.HasValue).GroupBy(x => x.Product, ReferenceEqualityComparer.Instance))
            {
                ((Product)group.Key).LowestAttributeCombinationPrice = group.Min(x => x.Price);
            }

            await Context.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Creates the rule set that only shows an attribute if other attributes of the product have certain values,
    /// e.g. <c>"visibleWhen": { "print": [ "print-text" ] }</c>. The rules are evaluated by the AttributeRules module
    /// and follow its naming ("Variant{ProductAttributeId}"). Without the module they have no effect.
    /// </summary>
    private RuleSetEntity CreateVisibilityRuleSet(Product product, ProductVariantAttribute attribute, JsonObject conditions)
    {
        var ruleSet = new RuleSetEntity
        {
            Scope = RuleScope.ProductAttribute,
            IsActive = true,
            LogicalOperator = LogicalRuleOperator.And,
            ProductVariantAttributeId = attribute.Id
        };

        var displayOrder = 0;
        foreach (var (attributeKey, valueKeys) in conditions)
        {
            var valueIds = valueKeys.AsArray()
                .Select(x => _seedResolver.GetId<ProductVariantAttributeValue>($"{product.Sku}/{attributeKey}/{x.GetValue<string>()}"));

            ruleSet.Rules.Add(new RuleEntity
            {
                RuleType = $"Variant{_seedResolver.GetId<ProductAttribute>(attributeKey)}",
                Operator = RuleOperator.In.Operator,
                Value = string.Join(',', valueIds),
                DisplayOrder = displayOrder++
            });
        }

        return ruleSet;
    }

    private SeedEntityDescriptor<Product> ProductDescriptor(ProductSeedState state)
    {
        var valueNames = _seedSource.LoadCultureObject(AttributeValueNamesFile);

        var descriptor = new SeedEntityDescriptor<Product>("sku")
            .FromFiles(ProductFiles)
            .WithDefaults(ProductDefaultsFile)
            .Reference<ProductTemplate>("productTemplate", (product, template) => product.ProductTemplateId = template.Id)
            .Reference<TaxCategory>("taxCategory", (product, taxCategory) => product.TaxCategoryId = taxCategory.Id)
            .Reference<DeliveryTime>("deliveryTime", (product, deliveryTime) => product.DeliveryTimeId = deliveryTime.Id)
            .Reference<QuantityUnit>("quantityUnit", (product, unit) => product.QuantityUnitId = unit.Id)
            .Reference<PriceLabel>("comparePriceLabel", (product, label) => product.ComparePriceLabelId = label.Id)
            .Bind("categories", (product, node, _) => ForEachAssignment(node, "category", (key, assignment) =>
                product.ProductCategories.Add(new ProductCategory
                {
                    Category = _seedResolver.Get<Category>(key),
                    DisplayOrder = assignment?["displayOrder"]?.GetValue<int>() ?? 1,
                    IsFeaturedProduct = assignment?["isFeaturedProduct"]?.GetValue<bool>() ?? false,
                    IsSystemMapping = assignment?["isSystemMapping"]?.GetValue<bool>() ?? false
                })))
            .Bind("manufacturers", (product, node, _) => ForEachAssignment(node, "manufacturer", (key, assignment) =>
                product.ProductManufacturers.Add(new ProductManufacturer
                {
                    Manufacturer = _seedResolver.Get<Manufacturer>(key),
                    DisplayOrder = assignment?["displayOrder"]?.GetValue<int>() ?? 1,
                    IsFeaturedProduct = assignment?["isFeaturedProduct"]?.GetValue<bool>() ?? false
                })))
            .Bind("groupedProductConfiguration", (product, node, _) =>
                product.GroupedProductConfiguration = node.Deserialize<GroupedProductConfiguration>(SmartJsonOptions.CamelCased))
            .Bind("pictures", (product, node, _) => AddProductPictures(product, node.AsArray(), state))
            .References<ProductTag>("tags", (product, tags) => tags.Each(product.ProductTags.Add))
            .References<Discount>("discounts", (product, discounts) => discounts.Each(product.AppliedDiscounts.Add))
            // Keyless list: an overlay replaces the specifications of a product as a whole.
            .ChildList("specifications", x => x.ProductSpecificationAttributes, new SeedEntityDescriptor<ProductSpecificationAttribute>("specificationAttributeOption")
                .Reference<SpecificationAttributeOption>("specificationAttributeOption", (psa, option) => psa.SpecificationAttributeOptionId = option.Id))
            .ChildList("reviews", x => x.ProductReviews, new SeedEntityDescriptor<ProductReview>("title")
                .Reference<Customer>("customer", (review, customer) => review.CustomerId = customer.Id))
            .Child("tierPrices", x => x.TierPrices, new SeedEntityDescriptor<TierPrice>("quantity"))
            .Bind("sampleDownload", (product, node, _) =>
            {
                product.HasSampleDownload = true;
                state.SampleDownloads.Add((product, node.GetValue<string>()));
            })
            .Bind("parentGroupedProduct", (product, node, _) => state.GroupedParents.Add((product, node.GetValue<string>())))
            .Bind("bundleItems", (product, node, context) => state.BundleItems.Add((product, node.AsArray(), context.Source)))
            .Bind("variantAttributes", (product, node, context) => state.VariantAttributes.Add((product, node.AsArray(), context.Source)))
            .Bind("combinations", (product, node, context) => state.Combinations.Add((product, node.AsArray(), context.Source)))
            // Derived product properties are maintained by hooks at runtime, which do not run during installation.
            .OnCreated((product, _) =>
            {
                var approved = product.ProductReviews.Where(x => x.IsApproved).ToList();
                product.ApprovedTotalReviews = approved.Count;
                product.ApprovedRatingSum = approved.Sum(x => x.Rating);
                product.HasTierPrices = product.TierPrices.Count > 0;
                product.HasDiscountsApplied = product.AppliedDiscounts.Count > 0;
            });

        // Child collections created after the products: declared for merging only.
        // Combinations are not keyed (SKUs are not unique); an overlay replaces them as a whole.
        descriptor.Spec
            .WithChild("bundleItems", new SeedCollectionSpec("product"))
            .WithChild("variantAttributes", new SeedCollectionSpec("productAttribute").WithChild("values", new SeedCollectionSpec("alias")));

        // Culture-wide variant value names, before product overlays so that those can still override them.
        if (valueNames != null)
        {
            descriptor.BeforeOverlay = record => ApplyAttributeValueNames(record, valueNames);
        }

        return descriptor;
    }

    private void AddProductPictures(Product product, JsonArray pictures, ProductSeedState state)
    {
        foreach (var picture in pictures)
        {
            if (picture is JsonObject obj && obj.ContainsKey("from"))
            {
                // Same media file as a picture of another product. Resolved after saving.
                state.SharedPictures.Add((product, obj));
                continue;
            }

            var (path, name) = InstallationSeedDescriptors.ParseMediaReference(picture);
            var displayOrder = (picture as JsonObject)?["displayOrder"]?.GetValue<int>() ?? 1;

            // Same naming as the former AddProductPicture(): slug of the file name unless a name is given.
            var mediaFile = CreateSampleMedia(path, name ?? SlugUtility.Slugify(Path.GetFileNameWithoutExtension(path), SeedSlugifyOptions));

            product.ProductMediaFiles.Add(new ProductMediaFile { MediaFile = mediaFile, DisplayOrder = displayOrder });

            if (!state.Pictures.TryGetValue(product, out var map))
            {
                state.Pictures[product] = map = new(StringComparer.OrdinalIgnoreCase);
            }

            map.TryAdd(path, mediaFile);
        }
    }

    /// <summary>
    /// Media of variant values and combinations: a picture of the product with that path,
    /// otherwise a separate media file that is created once per path.
    /// </summary>
    private MediaFile ResolveProductMedia(Product product, JsonNode reference, ProductSeedState state)
    {
        var (path, name) = InstallationSeedDescriptors.ParseMediaReference(reference);

        if (state.Pictures.TryGetValue(product, out var pictures) && pictures.TryGetValue(path, out var picture))
        {
            return picture;
        }

        if (!state.SeparateMedia.TryGetValue(path, out var mediaFile))
        {
            mediaFile = CreateSampleMedia(path, name);
            state.SeparateMedia[path] = mediaFile;
        }

        return mediaFile;
    }

    /// <summary>
    /// Converts { "attributeAlias": "valueAlias" | ["valueAlias", …] } to a raw attribute selection.
    /// </summary>
    private string CreateAttributeSelection(Product product, JsonNode node)
    {
        if (node is not JsonObject attributes)
        {
            throw new SeedDataFormatException("\"attributes\" must be an object of attribute alias → value alias(es).");
        }

        var selection = new ProductVariantAttributeSelection(string.Empty);
        foreach (var (attributeKey, valueNode) in attributes)
        {
            var pva = _seedResolver.Get<ProductVariantAttribute>($"{product.Sku}/{attributeKey}");
            var valueKeys = valueNode is JsonArray array
                ? array.Select(x => SeedJson.GetKeyValue(x, attributeKey))
                : [SeedJson.GetKeyValue(valueNode, attributeKey)];

            var valueIds = valueKeys
                .Select(x => (object)_seedResolver.GetId<ProductVariantAttributeValue>($"{product.Sku}/{attributeKey}/{x}"))
                .ToList();

            selection.AddAttribute(pva.Id, valueIds);
        }

        return selection.AsJson();
    }

    /// <summary>
    /// Assignment list: a key (display order 1) or an object with the key in <paramref name="keyField"/> and further
    /// options ("displayOrder", "isFeaturedProduct", "isSystemMapping"). The action gets the key and the object (or null).
    /// </summary>
    private static void ForEachAssignment(JsonNode node, string keyField, Action<string, JsonObject> action)
    {
        foreach (var item in node.AsArray())
        {
            if (item is JsonObject obj)
            {
                action(SeedJson.GetKeyValue(obj[keyField], keyField), obj);
            }
            else
            {
                action(SeedJson.GetKeyValue(item, keyField), null);
            }
        }
    }

    private static void ApplyAttributeValueNames(JsonObject product, JsonObject names)
    {
        if (product["variantAttributes"] is not JsonArray attributes)
        {
            return;
        }

        foreach (var value in attributes.OfType<JsonObject>().Select(x => x["values"]).OfType<JsonArray>().SelectMany(x => x.OfType<JsonObject>()))
        {
            if (value["alias"] is JsonValue alias && SeedJson.TryGet(names, alias.ToString(), out var name))
            {
                value["name"] = name?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Seed record of a product child created after saving, registered as "{sku}/{childKey}".
    /// </summary>
    private static SeedRecord ChildRecord(Product product, JsonNode record, string keyField, string source)
    {
        var obj = record.AsObject();
        var key = SeedJson.GetKey(obj, keyField, source)
            ?? throw new SeedDataFormatException($"{source} → {product.Sku}: record without key field '{keyField}'.");

        return new SeedRecord($"{product.Sku}/{key}", obj, source);
    }
}
