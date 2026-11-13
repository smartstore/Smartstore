using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Core.Installation.Seeding;
using Smartstore.IO;

namespace Smartstore.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedDataSourceTests
{
    private string _root;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SmartstoreSeedDataSourceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
        }
    }

    private void WriteFile(string subpath, string content)
    {
        var path = Path.Combine(_root, subpath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    private SeedDataSource CreateSource(string culture = "de-DE")
        => new(new LocalFileSystem(_root), culture);

    private static SeedEntityDescriptor<Country> CountryDescriptor(params string[] files)
        => new SeedEntityDescriptor<Country>("twoLetterIsoCode").FromFiles(files.Length > 0 ? files : ["core/countries.json"]);

    [Test]
    public void Loads_base_records_with_defaults_and_overlay()
    {
        WriteFile("base/core/_defaults.json", """{ "published": true, "allowsBilling": true }""");
        WriteFile("base/core/countries.json", """
            // Comments and trailing commas are allowed.
            [
              { "twoLetterIsoCode": "US", "name": "United States" },
              { "twoLetterIsoCode": "DE", "name": "Germany", "published": false },
            ]
            """);
        WriteFile("cultures/de-DE/core/countries.json", """{ "$all": { "allowsBilling": false }, "DE": { "name": "Deutschland", "published": true } }""");

        var descriptor = CountryDescriptor().WithDefaults("core/_defaults.json");
        var set = CreateSource().Load(descriptor);

        Assert.That(set.Records.Select(x => x.Key), Is.EqualTo(new[] { "US", "DE" }));
        Assert.That(set.Records.All(x => x.Source == "base/core/countries.json"), Is.True);

        var us = set.Records[0].Data;
        Assert.That((bool)us["published"], Is.True);
        Assert.That((bool)us["allowsBilling"], Is.False);

        var de = set.Records[1].Data;
        Assert.That((string)de["name"], Is.EqualTo("Deutschland"));
        Assert.That((bool)de["published"], Is.True);
    }

    [Test]
    public void Base_only_when_culture_is_null()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE", "name": "Germany" } ]""");
        WriteFile("cultures/de-DE/core/countries.json", """{ "DE": { "name": "Deutschland" } }""");

        var set = CreateSource(culture: null).Load(CountryDescriptor());

        Assert.That((string)set.Records[0].Data["name"], Is.EqualTo("Germany"));
    }

    [Test]
    public void Defaults_overlay_is_applied()
    {
        WriteFile("base/core/_defaults.json", """{ "published": true }""");
        WriteFile("cultures/de-DE/core/_defaults.json", """{ "published": false }""");
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE" } ]""");

        var set = CreateSource().Load(CountryDescriptor().WithDefaults("core/_defaults.json"));

        Assert.That((bool)set.Records[0].Data["published"], Is.False);
    }

    [Test]
    public void Wildcard_reads_files_in_ordinal_order_and_skips_underscore_files()
    {
        WriteFile("base/samples/countries/b.json", """[ { "twoLetterIsoCode": "B1" } ]""");
        WriteFile("base/samples/countries/a.json", """[ { "twoLetterIsoCode": "A1" }, { "twoLetterIsoCode": "A2" } ]""");
        WriteFile("base/samples/countries/_defaults.json", """{ "published": true }""");
        WriteFile("cultures/de-DE/samples/countries/b.json", """{ "B1": { "name": "Bee" } }""");

        var set = CreateSource().Load(CountryDescriptor("samples/countries/*.json"));

        Assert.That(set.Records.Select(x => x.Key), Is.EqualTo(new[] { "A1", "A2", "B1" }));
        Assert.That((string)set.Records[2].Data["name"], Is.EqualTo("Bee"));
        Assert.That(set.Records[2].Source, Is.EqualTo("base/samples/countries/b.json"));
    }

    [Test]
    public void Duplicate_keys_across_files_throw()
    {
        WriteFile("base/samples/countries/a.json", """[ { "twoLetterIsoCode": "DE" } ]""");
        WriteFile("base/samples/countries/b.json", """[ { "twoLetterIsoCode": "de" } ]""");

        var ex = Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor("samples/countries/*.json")));
        Assert.That(ex.Message, Does.Contain("Duplicate"));
    }

    [Test]
    public void Missing_base_file_throws()
        => Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor()));

    [Test]
    public void Wildcard_without_matches_throws()
        => Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor("samples/countries/*.json")));

    [Test]
    public void Invalid_json_throws_with_file_name()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE" """);

        var ex = Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor()));
        Assert.That(ex.Message, Does.Contain("base/core/countries.json"));
    }

    [Test]
    public void File_directive_loads_html_next_to_base_and_overlay_files()
    {
        WriteFile("base/core/countries.json", """
            [
              { "twoLetterIsoCode": "DE", "addressFormat": "@file" },
              { "twoLetterIsoCode": "US", "addressFormat": "@file:formats/us.txt" },
              { "twoLetterIsoCode": "LC 2/3", "addressFormat": "@file" }
            ]
            """);
        WriteFile("base/core/countries/DE.addressFormat.html", "<p>base de</p>");
        WriteFile("base/core/formats/us.txt", "us format");
        WriteFile("base/core/countries/LC 2_3.addressFormat.html", "sanitized");
        WriteFile("cultures/de-DE/core/countries.json", """{ "DE": { "addressFormat": "@file" } }""");
        WriteFile("cultures/de-DE/core/countries/DE.addressFormat.html", "<p>overlay de</p>");

        var records = CreateSource().Load(CountryDescriptor()).Records;

        Assert.That((string)records[0].Data["addressFormat"], Is.EqualTo("<p>overlay de</p>"));
        Assert.That((string)records[1].Data["addressFormat"], Is.EqualTo("us format"));
        Assert.That((string)records[2].Data["addressFormat"], Is.EqualTo("sanitized"));

        var baseOnly = CreateSource(culture: null).Load(CountryDescriptor()).Records;
        Assert.That((string)baseOnly[0].Data["addressFormat"], Is.EqualTo("<p>base de</p>"));
    }

    [Test]
    public void File_directive_with_missing_file_throws()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE", "addressFormat": "@file" } ]""");

        var ex = Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor()));
        Assert.That(ex.Message, Does.Contain("DE.addressFormat.html"));
    }

    [Test]
    public void File_directive_in_nested_fields_throws()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE", "stateProvinces": [ { "abbreviation": "BW", "name": "@file" } ] } ]""");

        Assert.Throws<SeedDataFormatException>(() => CreateSource().Load(CountryDescriptor()));
    }

    [Test]
    public void Overlay_aliases_are_returned()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE" } ]""");
        WriteFile("cultures/de-DE/core/countries.json", """{ "$aliases": { "germany": "DE" } }""");

        var set = CreateSource().Load(CountryDescriptor());

        Assert.That(set.Aliases["GERMANY"], Is.EqualTo("DE"));
    }

    [Test]
    public void LoadObject_merges_culture_overlay_field_by_field()
    {
        WriteFile("base/core/settings.json", """{ "TaxSettings": { "TaxBasedOn": "BillingAddress", "EuVatEnabled": false }, "SeoSettings": { "MetaTitle": "Shop" } }""");
        WriteFile("cultures/de-DE/core/settings.json", """{ "TaxSettings": { "EuVatEnabled": true }, "OrderSettings": { "NumberOfDaysReturnRequestAvailable": 14 } }""");

        var settings = CreateSource().LoadObject("core/settings.json");

        Assert.That((string)settings["TaxSettings"]["TaxBasedOn"], Is.EqualTo("BillingAddress"));
        Assert.That((bool)settings["TaxSettings"]["EuVatEnabled"], Is.True);
        Assert.That((string)settings["SeoSettings"]["MetaTitle"], Is.EqualTo("Shop"));
        Assert.That((int)settings["OrderSettings"]["NumberOfDaysReturnRequestAvailable"], Is.EqualTo(14));
    }

    [Test]
    public void Sample_overlays_of_core_files_only_apply_with_sample_data()
    {
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "US", "name": "United States" }, { "twoLetterIsoCode": "DE", "name": "Germany" } ]""");
        WriteFile("cultures/de-DE/core/countries.json", """{ "DE": { "name": "Deutschland" } }""");
        WriteFile("base/samples/core/countries.json", """{ "US": { "published": false } }""");
        WriteFile("cultures/de-DE/samples/core/countries.json", """{ "DE": { "name": "Deutschland (Demo)" } }""");

        var withSamples = new SeedDataSource(new LocalFileSystem(_root), "de-DE") { IncludeSamples = true }.Load(CountryDescriptor());
        Assert.That((bool)withSamples.Records[0].Data["published"], Is.False);
        Assert.That((string)withSamples.Records[1].Data["name"], Is.EqualTo("Deutschland (Demo)"));

        var withoutSamples = CreateSource().Load(CountryDescriptor());
        Assert.That(withoutSamples.Records[0].Data["published"], Is.Null);
        Assert.That((string)withoutSamples.Records[1].Data["name"], Is.EqualTo("Deutschland"));
    }

    [Test]
    public void LoadObject_applies_sample_overlay_only_with_sample_data()
    {
        WriteFile("base/core/settings.json", """{ "PriceSettings": { "OfferBadgeLabel": "Deal" } }""");
        WriteFile("base/samples/core/settings.json", """{ "PriceSettings": { "PriceDisplayType": "PreSelectedPrice" } }""");

        var withSamples = new SeedDataSource(new LocalFileSystem(_root), "de-DE") { IncludeSamples = true }.LoadObject("core/settings.json");
        Assert.That((string)withSamples["PriceSettings"]["PriceDisplayType"], Is.EqualTo("PreSelectedPrice"));
        Assert.That((string)withSamples["PriceSettings"]["OfferBadgeLabel"], Is.EqualTo("Deal"));

        var withoutSamples = CreateSource().LoadObject("core/settings.json");
        Assert.That(withoutSamples["PriceSettings"]["PriceDisplayType"], Is.Null);
    }

    [Test]
    public void BeforeOverlay_runs_between_defaults_and_overlay()
    {
        WriteFile("base/core/_defaults.json", """{ "published": true }""");
        WriteFile("base/core/countries.json", """[ { "twoLetterIsoCode": "DE", "name": "Germany" }, { "twoLetterIsoCode": "AT", "name": "Austria" } ]""");
        WriteFile("cultures/de-DE/core/countries.json", """{ "AT": { "name": "Österreich" } }""");

        var descriptor = CountryDescriptor().WithDefaults("core/_defaults.json");
        descriptor.BeforeOverlay = record =>
        {
            Assert.That((bool)record["published"], Is.True, "Defaults must already be applied.");
            record["name"] = "Translated " + record["name"];
        };

        var records = CreateSource().Load(descriptor).Records;

        Assert.That((string)records[0].Data["name"], Is.EqualTo("Translated Germany"));
        Assert.That((string)records[1].Data["name"], Is.EqualTo("Österreich"), "The overlay wins over the transformation.");
    }

    [Test]
    public void LoadCultureObject_returns_null_without_culture_file()
    {
        WriteFile("cultures/de-DE/samples/names.json", """{ "black": "Schwarz" }""");

        Assert.That((string)CreateSource().LoadCultureObject("samples/names.json")["black"], Is.EqualTo("Schwarz"));
        Assert.That(CreateSource().LoadCultureObject("samples/missing.json"), Is.Null);
        Assert.That(CreateSource(culture: null).LoadCultureObject("samples/names.json"), Is.Null);
    }

    [Test]
    public void LoadObject_requires_an_object()
    {
        WriteFile("base/core/settings.json", "[]");

        Assert.Throws<SeedDataFormatException>(() => CreateSource().LoadObject("core/settings.json"));
    }

    [Test]
    public void Discovers_cultures_with_culture_file()
    {
        WriteFile("cultures/de-DE/culture.json", """{ "name": "Deutsch", "uniqueSeoCode": "de", "flagImageFileName": "de.png" }""");
        WriteFile("cultures/en-US/culture.json", """{ "Name": "English", "UniqueSeoCode": "en", "FlagImageFileName": "us.png" }""");
        WriteFile("cultures/xx-XX/core/countries.json", "{}");

        var cultures = SeedDataSource.DiscoverCultures(new LocalFileSystem(_root));

        Assert.That(cultures.Select(x => x.Culture), Is.EqualTo(new[] { "de-DE", "en-US" }));
        Assert.That(cultures[0].Name, Is.EqualTo("Deutsch"));
        Assert.That(cultures[1].FlagImageFileName, Is.EqualTo("us.png"));
    }
}
