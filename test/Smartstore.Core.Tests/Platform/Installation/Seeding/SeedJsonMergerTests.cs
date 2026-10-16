using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Smartstore.Core.Installation.Seeding;

namespace Smartstore.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedJsonMergerTests
{
    private static readonly SeedCollectionSpec CountrySpec = new SeedCollectionSpec("twoLetterIsoCode")
        .WithChild("stateProvinces", new SeedCollectionSpec("abbreviation"));

    private static JsonObject Obj(string json)
        => JsonNode.Parse(json).AsObject();

    private static List<JsonObject> Countries() =>
    [
        Obj("""{ "twoLetterIsoCode": "US", "name": "United States", "published": true, "displayOrder": 1, "stateProvinces": [ { "abbreviation": "NY", "name": "New York" } ] }"""),
        Obj("""{ "twoLetterIsoCode": "DE", "name": "Germany", "published": true, "displayOrder": 50, "format": { "a": 1, "b": 2 } }"""),
        Obj("""{ "twoLetterIsoCode": "FR", "name": "France", "published": true, "displayOrder": 60 }""")
    ];

    private static List<JsonObject> Apply(string overlay, Dictionary<string, string> aliases = null)
        => SeedJsonMerger.ApplyOverlay(Countries(), Obj(overlay), CountrySpec, "test", aliases ?? []);

    private static JsonObject Find(List<JsonObject> records, string key)
        => records.Single(x => (string)x["twoLetterIsoCode"] == key);

    [Test]
    public void Keyed_patch_merges_fields_and_keeps_others()
    {
        var result = Apply("""{ "de": { "name": "Deutschland", "displayOrder": -10 } }""");

        var de = Find(result, "DE");
        Assert.That((string)de["name"], Is.EqualTo("Deutschland"));
        Assert.That((int)de["displayOrder"], Is.EqualTo(-10));
        Assert.That((bool)de["published"], Is.True);
        Assert.That(result.Select(x => (string)x["twoLetterIsoCode"]), Is.EqualTo(new[] { "US", "DE", "FR" }));
    }

    [Test]
    public void Field_names_are_matched_case_insensitively()
    {
        var de = Find(Apply("""{ "DE": { "Name": "Deutschland" } }"""), "DE");

        Assert.That((string)de["name"], Is.EqualTo("Deutschland"));
        Assert.That(de.Count(x => x.Key.Equals("name", System.StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
    }

    [Test]
    public void Nested_objects_are_merged_field_by_field()
    {
        var de = Find(Apply("""{ "DE": { "format": { "b": 3 } } }"""), "DE");

        Assert.That((int)de["format"]["a"], Is.EqualTo(1));
        Assert.That((int)de["format"]["b"], Is.EqualTo(3));
    }

    [Test]
    public void All_applies_to_every_record_before_keyed_patches()
    {
        var result = Apply("""{ "$all": { "published": false }, "DE": { "published": true } }""");

        Assert.That((bool)Find(result, "US")["published"], Is.False);
        Assert.That((bool)Find(result, "FR")["published"], Is.False);
        Assert.That((bool)Find(result, "DE")["published"], Is.True);
    }

    [Test]
    public void Remove_and_add_change_the_record_list()
    {
        var result = Apply("""{ "$remove": [ "fr" ], "$add": [ { "twoLetterIsoCode": "AT", "name": "Österreich" } ] }""");

        Assert.That(result.Select(x => (string)x["twoLetterIsoCode"]), Is.EqualTo(new[] { "US", "DE", "AT" }));
    }

    [Test]
    public void Unknown_key_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() => Apply("""{ "XX": { "name": "Typo" } }"""));
        Assert.That(ex.Message, Does.Contain("unknown key 'XX'"));
    }

    [Test]
    public void Adding_an_existing_key_throws()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "$add": [ { "twoLetterIsoCode": "de" } ] }"""));

    [Test]
    public void Removing_an_unknown_key_throws()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "$remove": [ "XX" ] }"""));

    [Test]
    public void Unknown_directive_throws()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "$foo": 1 }"""));

    [Test]
    public void Directive_inside_a_record_patch_throws()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "DE": { "$all": {} } }"""));

    [Test]
    public void Replace_mode_drops_base_records()
    {
        var aliases = new Dictionary<string, string>();
        var result = Apply("""
            {
              "$mode": "replace",
              "$aliases": { "fr": "AT" },
              "$items": [ { "twoLetterIsoCode": "AT", "name": "Österreich" } ]
            }
            """, aliases);

        Assert.That(result.Select(x => (string)x["twoLetterIsoCode"]), Is.EqualTo(new[] { "AT" }));
        Assert.That(aliases["fr"], Is.EqualTo("AT"));
    }

    [Test]
    public void Replace_mode_rejects_keyed_patches()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "$mode": "replace", "$items": [], "DE": {} }"""));

    [Test]
    public void Items_without_replace_mode_throws()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "$items": [] }"""));

    [Test]
    public void Child_collection_overlay_patches_and_adds_by_child_key()
    {
        var result = Apply("""
            {
              "US": { "stateProvinces": { "ny": { "name": "New York State" }, "$add": [ { "abbreviation": "CA", "name": "California" } ] } },
              "DE": { "stateProvinces": { "$add": [ { "abbreviation": "BW", "name": "Baden-Württemberg" } ] } }
            }
            """);

        var usStates = Find(result, "US")["stateProvinces"].AsArray();
        Assert.That(usStates.Select(x => (string)x["name"]), Is.EqualTo(new[] { "New York State", "California" }));

        var deStates = Find(result, "DE")["stateProvinces"].AsArray();
        Assert.That(deStates.Select(x => (string)x["abbreviation"]), Is.EqualTo(new[] { "BW" }));
    }

    [Test]
    public void Child_collection_array_replaces_the_collection()
    {
        var us = Find(Apply("""{ "US": { "stateProvinces": [ { "abbreviation": "TX", "name": "Texas" } ] } }"""), "US");

        Assert.That(us["stateProvinces"].AsArray().Select(x => (string)x["abbreviation"]), Is.EqualTo(new[] { "TX" }));
    }

    [Test]
    public void Unknown_child_key_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() => Apply("""{ "US": { "stateProvinces": { "XX": {} } } }"""));
        Assert.That(ex.Message, Does.Contain("stateProvinces"));
    }

    [Test]
    public void Aliases_are_not_allowed_in_child_collections()
        => Assert.Throws<SeedDataFormatException>(() => Apply("""{ "US": { "stateProvinces": { "$aliases": { "a": "b" } } } }"""));

    [Test]
    public void Duplicate_base_keys_throw()
    {
        var records = Countries();
        records.Add(Obj("""{ "twoLetterIsoCode": "de" }"""));

        Assert.Throws<SeedDataFormatException>(() => SeedJsonMerger.ApplyOverlay(records, Obj("{}"), CountrySpec, "test", new Dictionary<string, string>()));
    }

    [Test]
    public void Defaults_are_overridden_by_record_values()
    {
        var defaults = Obj("""{ "published": true, "stockQuantity": 10000, "format": { "a": 0, "c": 3 } }""");
        var record = Obj("""{ "twoLetterIsoCode": "DE", "stockQuantity": 5, "format": { "a": 1 } }""");

        var result = SeedJsonMerger.ApplyDefaults(defaults, record, CountrySpec, "test");

        Assert.That((bool)result["published"], Is.True);
        Assert.That((int)result["stockQuantity"], Is.EqualTo(5));
        Assert.That((int)result["format"]["a"], Is.EqualTo(1));
        Assert.That((int)result["format"]["c"], Is.EqualTo(3));

        // Inputs stay untouched.
        Assert.That(defaults.ContainsKey("twoLetterIsoCode"), Is.False);
        Assert.That(record.ContainsKey("published"), Is.False);
    }
}
