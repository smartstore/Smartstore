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
