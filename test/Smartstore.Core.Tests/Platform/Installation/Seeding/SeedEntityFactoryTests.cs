using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Installation.Seeding;

namespace Smartstore.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedEntityFactoryTests
{
    private SeedReferenceResolver _resolver;
    private SeedEntityFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _resolver = new SeedReferenceResolver();
        _factory = new SeedEntityFactory(_resolver);
    }

    private static SeedRecord Record(string key, string json)
        => new(key, JsonNode.Parse(json).AsObject(), "base/test.json");

    private static SeedEntityDescriptor<Country> CountryDescriptor()
        => new SeedEntityDescriptor<Country>("twoLetterIsoCode")
            .Child("stateProvinces", x => x.StateProvinces, new SeedEntityDescriptor<StateProvince>("abbreviation"));

    private static SeedEntityDescriptor<Product> ProductDescriptor()
        => new SeedEntityDescriptor<Product>("sku")
            .Reference<TaxCategory>("taxCategory", (p, t) => p.TaxCategoryId = t.Id)
            .References<Category>("categories", (p, categories) =>
            {
                for (var i = 0; i < categories.Count; i++)
                {
                    p.ProductCategories.Add(new ProductCategory { Category = categories[i], DisplayOrder = i + 1 });
                }
            });

    [Test]
    public void Deserializes_scalar_fields_and_registers_entity()
    {
        var record = Record("DE", """{ "twoLetterIsoCode": "DE", "name": "Deutschland", "numericIsoCode": 276, "published": true, "diallingCode": 49 }""");

        var country = _factory.Create(record, CountryDescriptor());

        Assert.That(country.Name, Is.EqualTo("Deutschland"));
        Assert.That(country.NumericIsoCode, Is.EqualTo(276));
        Assert.That(country.DiallingCode, Is.EqualTo(49));
        Assert.That(country.Published, Is.True);
        Assert.That(_resolver.Get<Country>("de"), Is.SameAs(country));
    }

    [Test]
    public void Enums_are_read_by_name()
    {
        var product = _factory.Create(Record("P-1", """{ "sku": "P-1", "productType": "GroupedProduct" }"""), new SeedEntityDescriptor<Product>("sku"));

        Assert.That(product.ProductType, Is.EqualTo(ProductType.GroupedProduct));
    }

    [Test]
    public void Enum_integers_are_rejected()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "sku": "P-1", "productType": 10 }"""), new SeedEntityDescriptor<Product>("sku")));

    [Test]
    public void Unknown_field_throws_with_context()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "nmae": "Typo" }"""), CountryDescriptor()));

        Assert.That(ex.Message, Does.Contain("base/test.json"));
        Assert.That(ex.Message, Does.Contain("Country 'DE'"));
        Assert.That(ex.Message, Does.Contain("nmae"));
    }

    [Test]
    public void Id_field_is_rejected()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "id": 5 }"""), CountryDescriptor()));

    [Test]
    public void Navigation_fields_are_rejected_unless_bound()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "sku": "P-1", "productCategories": [] }"""), new SeedEntityDescriptor<Product>("sku")));

    [Test]
    public void Child_collections_are_created_and_registered_with_composite_key()
    {
        var record = Record("DE", """
            {
              "twoLetterIsoCode": "DE",
              "name": "Deutschland",
              "stateProvinces": [ { "abbreviation": "BW", "name": "Baden-Württemberg" }, { "abbreviation": "BY", "name": "Bayern" } ]
            }
            """);

        var country = _factory.Create(record, CountryDescriptor());

        Assert.That(country.StateProvinces.Select(x => x.Name), Is.EquivalentTo(new[] { "Baden-Württemberg", "Bayern" }));
        Assert.That(_resolver.Get<StateProvince>("DE/BY").Name, Is.EqualTo("Bayern"));
    }

    [Test]
    public void Invalid_child_field_reports_the_child_path()
    {
        var record = Record("DE", """{ "twoLetterIsoCode": "DE", "stateProvinces": [ { "abbreviation": "BW", "foo": 1 } ] }""");

        var ex = Assert.Throws<SeedDataFormatException>(() => _factory.Create(record, CountryDescriptor()));
        Assert.That(ex.Message, Does.Contain("StateProvince 'DE/BW'"));
    }

    [Test]
    public void References_are_resolved_including_aliases()
    {
        _resolver.Add("standard", new TaxCategory { Id = 7, Name = "Normal" });
        _resolver.AddAliases<TaxCategory>([new("apparel", "standard")]);

        var golf = new Category { Name = "Golf" };
        var sports = new Category { Name = "Sports" };
        _resolver.Add("golf", golf);
        _resolver.Add("sports", sports);

        var product = _factory.Create(Record("P-1", """{ "sku": "P-1", "taxCategory": "apparel", "categories": [ "sports", "GOLF" ] }"""), ProductDescriptor());

        Assert.That(product.TaxCategoryId, Is.EqualTo(7));
        Assert.That(product.ProductCategories.OrderBy(x => x.DisplayOrder).Select(x => x.Category), Is.EqualTo(new[] { sports, golf }));
    }

    [Test]
    public void Unknown_reference_throws_with_field()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "sku": "P-1", "taxCategory": "missing" }"""), ProductDescriptor()));

        Assert.That(ex.Message, Does.Contain("taxCategory"));
        Assert.That(ex.Message, Does.Contain("missing"));
    }

    [Test]
    public void Null_bound_fields_are_skipped()
    {
        var product = _factory.Create(Record("P-1", """{ "sku": "P-1", "taxCategory": null }"""), ProductDescriptor());

        Assert.That(product.TaxCategoryId, Is.EqualTo(0));
    }

    [Test]
    public void Custom_binder_receives_value_and_context()
    {
        string boundKey = null;
        var descriptor = new SeedEntityDescriptor<Product>("sku")
            .Bind("pictures", (p, node, context) =>
            {
                boundKey = context.Key;
                p.Name = string.Join(",", node.AsArray().Select(x => (string)x));
            });

        var product = _factory.Create(Record("P-1", """{ "sku": "P-1", "pictures": [ "a.jpg", "b.jpg" ] }"""), descriptor);

        Assert.That(product.Name, Is.EqualTo("a.jpg,b.jpg"));
        Assert.That(boundKey, Is.EqualTo("P-1"));
    }

    [Test]
    public void Binding_a_field_twice_throws()
        => Assert.Throws<System.InvalidOperationException>(() =>
            new SeedEntityDescriptor<Product>("sku").Bind("x", (_, _, _) => { }).Bind("X", (_, _, _) => { }));

    [Test]
    public void CreateAll_strips_synthetic_key_and_registers_aliases()
    {
        var descriptor = new SeedEntityDescriptor<TaxCategory>("key");
        var set = new SeedRecordSet(
            descriptor,
            [Record("standard", """{ "key": "standard", "name": "Normal" }""")],
            new System.Collections.Generic.Dictionary<string, string> { ["apparel"] = "standard" });

        var categories = _factory.CreateAll<TaxCategory>(set);

        Assert.That(categories.Single().Name, Is.EqualTo("Normal"));
        Assert.That(_resolver.Get<TaxCategory>("apparel"), Is.SameAs(categories[0]));
    }

    [Test]
    public void CreateAll_rejects_record_sets_of_other_types()
    {
        var set = new SeedRecordSet(new SeedEntityDescriptor<TaxCategory>("key"), [], new System.Collections.Generic.Dictionary<string, string>());

        Assert.Throws<System.ArgumentException>(() => _factory.CreateAll<Country>(set));
    }
}
