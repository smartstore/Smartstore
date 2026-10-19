using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Installation.Seeding;

namespace Smartstore.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedReferenceResolverTests
{
    [Test]
    public void Keys_are_case_insensitive_and_scoped_per_type()
    {
        var resolver = new SeedReferenceResolver();
        var category = new TaxCategory { Name = "Normal" };
        resolver.Add("standard", category);

        Assert.That(resolver.Get<TaxCategory>("STANDARD"), Is.SameAs(category));
        Assert.That(resolver.TryGet<Country>("standard", out _), Is.False);
    }

    [Test]
    public void Aliases_resolve_with_a_single_hop()
    {
        var resolver = new SeedReferenceResolver();
        var category = new TaxCategory { Name = "Normal" };
        resolver.Add("standard", category);
        resolver.AddAliases<TaxCategory>(new Dictionary<string, string> { ["apparel"] = "standard", ["shoes"] = "apparel" });

        Assert.That(resolver.Get<TaxCategory>("apparel"), Is.SameAs(category));
        Assert.That(resolver.TryGet<TaxCategory>("shoes", out _), Is.False);
    }

    [Test]
    public void Registered_keys_win_over_aliases()
    {
        var resolver = new SeedReferenceResolver();
        var apparel = new TaxCategory { Name = "Apparel" };
        resolver.Add("apparel", apparel);
        resolver.Add("standard", new TaxCategory { Name = "Normal" });
        resolver.AddAliases<TaxCategory>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.Get<TaxCategory>("apparel"), Is.SameAs(apparel));
    }

    [Test]
    public void Unknown_key_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() => new SeedReferenceResolver().Get<TaxCategory>("missing"));
        Assert.That(ex.Message, Does.Contain("TaxCategory key 'missing'"));
    }

    [Test]
    public void Duplicate_key_throws()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new TaxCategory());

        Assert.Throws<SeedDataFormatException>(() => resolver.Add("Standard", new TaxCategory()));
    }

    [Test]
    public void GetId_by_type_name_resolves_keys_and_aliases()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new TaxCategory { Id = 4 });
        resolver.AddAliases<TaxCategory>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.GetId("taxcategory", "STANDARD"), Is.EqualTo(4));
        Assert.That(resolver.GetId("TaxCategory", "apparel"), Is.EqualTo(4));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("Currency", "EUR"));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("TaxCategory", "missing"));
    }

    [Test]
    public void GetId_requires_a_saved_entity()
    {
        var resolver = new SeedReferenceResolver();
        var category = new TaxCategory();
        resolver.Add("standard", category);

        Assert.Throws<SeedDataFormatException>(() => resolver.GetId<TaxCategory>("standard"));

        category.Id = 3;
        Assert.That(resolver.GetId<TaxCategory>("standard"), Is.EqualTo(3));
    }
}
