using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Data;
using Smartstore.Core.Installation.Seeding;
using Smartstore.IO;

namespace Smartstore.Core.Tests.Platform.Installation;

/// <summary>
/// Runs the real installation seeder with the real seed files for every installable culture.
/// The loader fails on unknown fields, unknown keys, broken references and missing files, so a successful
/// run already proves a lot. The rules below hold for any catalog: never assert concrete values or
/// counts here, otherwise editing the seed data would break the tests.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Seeding")]
public partial class InstallationSeedingTests
{
    [GeneratedRegex("""href="/([^"/?#]+)/?["?#]""", RegexOptions.IgnoreCase)]
    private static partial Regex InternalLinkRegex();

    private static IEnumerable<TestCaseData> Cultures()
        => SeedDataSource.DiscoverCultures(new LocalFileSystem(SeedingHarness.SeedSourceDir))
            .Select(x => new TestCaseData(x).SetArgDisplayNames(x.Culture));

    [TestCaseSource(nameof(Cultures))]
    public async Task Mandatory_data_is_seeded(SeedCulture culture)
    {
        using var harness = await SeedingHarness.SeedAsync(culture, seedSampleData: false);
        var db = harness.Db;

        Assert.Multiple(() =>
        {
            Assert.That(db.Stores.Any(), Is.True, "Stores");
            Assert.That(db.Currencies.Any(), Is.True, "Currencies");
            Assert.That(db.Countries.Any(), Is.True, "Countries");
            Assert.That(db.Settings.Any(), Is.True, "Settings");
            Assert.That(db.Products.Any(), Is.False, "Sample products must not be installed.");
        });
    }

    [TestCaseSource(nameof(Cultures))]
    public async Task Sample_data_is_seeded_and_consistent(SeedCulture culture)
    {
        using var harness = await SeedingHarness.SeedAsync(culture, seedSampleData: true);
        var db = harness.Db;

        Assert.Multiple(() =>
        {
            Assert.That(db.Products.Any(), Is.True, "Products");
            Assert.That(db.Categories.Any(), Is.True, "Categories");
            Assert.That(DuplicateSkus(db), Is.Empty, "SKUs must be unique across products and attribute combinations.");
            Assert.That(VisibleProductsWithoutCategory(db), Is.Empty, "Visible products need a category.");
            Assert.That(BrokenInternalLinks(db), Is.Empty, "Links to other records must point to an existing slug.");
        });
    }

    private static List<string> DuplicateSkus(SmartDbContext db)
    {
        var productSkus = db.Products.AsNoTracking().Select(x => x.Sku).ToList();
        var combinationSkus = db.ProductVariantAttributeCombinations.AsNoTracking().Select(x => x.Sku).ToList();

        return productSkus.Concat(combinationSkus)
            .Where(x => x != null)
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
    }

    private static List<string> VisibleProductsWithoutCategory(SmartDbContext db)
        => db.Products.AsNoTracking()
            .Where(x => x.Visibility == Core.Catalog.Products.ProductVisibility.Full && !x.ProductCategories.Any())
            .Select(x => x.Sku)
            .ToList();

    private static List<string> BrokenInternalLinks(SmartDbContext db)
    {
        var slugs = db.UrlRecords.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var products = db.Products.AsNoTracking().Select(x => new { Owner = x.Sku, Text = x.FullDescription }).ToList();
        var categories = db.Categories.AsNoTracking().Select(x => new { Owner = x.Name, Text = x.Description }).ToList();

        return products.Concat(categories)
            .Where(x => x.Text != null)
            .SelectMany(x => InternalLinkRegex().Matches(x.Text).Select(m => (x.Owner, Slug: m.Groups[1].Value)))
            .Where(x => !slugs.Contains(x.Slug))
            .Select(x => $"{x.Owner}: /{x.Slug}/")
            .ToList();
    }
}
