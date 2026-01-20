using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Smartstore.Collections;
using Smartstore.Collections.JsonConverters;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Domain;
using Smartstore.Json;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Json;

[TestFixture]
public class MultiMapConverterTests
{
    private MultiMapConverterFactory _converterFactory;
    private JsonSerializerOptions _options;

    private JsonConverter CreateConverter(Type converterType)
    {
        return _converterFactory.CreateConverter(converterType, _options);
    }

    [SetUp]
    public void Setup()
    {
        _converterFactory = new MultiMapConverterFactory();
        _options = SmartJsonOptions.Default;
    }

    [Test]
    public void CanConvert_Should_Return_False_For_ConcurrentMultimap()
    {
        var converter = CreateConverter(typeof(Multimap<string, int>));
        var result = converter.CanConvert(typeof(ConcurrentMultimap<string, int>));
        result.ShouldBeFalse();
    }

    [Test]
    public void CanConvert_Should_Return_False_For_Other_Types()
    {
        var converter = CreateConverter(typeof(Multimap<string, int>));
        converter.CanConvert(typeof(Dictionary<string, int>)).ShouldBeFalse();
        converter.CanConvert(typeof(List<string>)).ShouldBeFalse();
        converter.CanConvert(typeof(string)).ShouldBeFalse();
    }

    [Test]
    public void Write_Should_Serialize_Multimap_With_String_Keys()
    {
        var multimap = new Multimap<string, int>
        {
            { "Key1", 1 },
            { "Key1", 2 },
            { "Key2", 3 }
        };

        var json = JsonSerializer.Serialize(multimap, _options);
        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain("Key1"));
        Assert.That(json, Does.Contain("Key2"));

    }

    [Test]
    public void Read_Should_Deserialize_Multimap_With_String_Keys()
    {
        var original = new Multimap<string, int>
    {
        { "Key1", 1 },
        { "Key1", 2 },
        { "Key2", 3 }
    };

        // Act - Serialize then Deserialize
        var json = JsonSerializer.Serialize(original, _options);
        var multimap = JsonSerializer.Deserialize<Multimap<string, int>>(json, _options);

        multimap.ShouldNotBeNull();
        multimap.Count.ShouldEqual(2);

        Assert.That(multimap["Key1"].Count(), Is.EqualTo(2));
        Assert.That(multimap["Key1"], Does.Contain(1));
        Assert.That(multimap["Key1"], Does.Contain(2));
        Assert.That(multimap["Key2"].Count(), Is.EqualTo(1));
        Assert.That(multimap["Key2"], Does.Contain(3));
    }

    [Test]
    public void Read_Should_Use_Case_Insensitive_Comparer_For_String_Keys()
    {
        var original = new Multimap<string, int>
    {
        { "Key1", 1 },
        { "Key1", 2 },
        { "key1", 3 }
    };

        var json = JsonSerializer.Serialize(original, _options);
        var multimap = JsonSerializer.Deserialize<Multimap<string, int>>(json, _options);

        multimap.ShouldNotBeNull();
        // With case-insensitive comparer, "Key1" and "key1" should be treated as the same key
        // So we should have only 1 key with 3 values
        multimap.Count.ShouldEqual(1);
    }

    [Test]
    public void Write_Should_Serialize_Multimap_With_Int_Keys()
    {
        var multimap = new Multimap<int, string>
    {
        { 1, "Value1" },
        { 1, "Value2" },
        { 2, "Value3" }
    };

        var json = JsonSerializer.Serialize(multimap, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain("Value1"));
        Assert.That(json, Does.Contain("Value2"));
        Assert.That(json, Does.Contain("Value3"));
    }

    [Test]
    public void Read_Should_Deserialize_Multimap_With_Int_Keys()
    {
        var original = new Multimap<int, string>
    {
        { 1, "Value1" },
        { 1, "Value2" },
        { 2, "Value3" }
    };

        var json = JsonSerializer.Serialize(original, _options);
        var multimap = JsonSerializer.Deserialize<Multimap<int, string>>(json, _options);

        multimap.ShouldNotBeNull();
        multimap.Count.ShouldEqual(2);
        Assert.That(multimap[1].Count(), Is.EqualTo(2));
        Assert.That(multimap[1], Does.Contain("Value1"));
        Assert.That(multimap[1], Does.Contain("Value2"));
        Assert.That(multimap[2].Count(), Is.EqualTo(1));
        Assert.That(multimap[2], Does.Contain("Value3"));
    }

    [Test]
    public void RoundTrip_Should_Preserve_Data_With_String_Keys()
    {
        var original = new Multimap<string, int>
    {
        { "Alpha", 1 },
        { "Alpha", 2 },
        { "Beta", 3 },
        { "Gamma", 4 },
        { "Gamma", 5 },
        { "Gamma", 6 }
    };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Multimap<string, int>>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldEqual(original.Count);
        deserialized["Alpha"].Count().ShouldEqual(2);
        deserialized["Beta"].Count().ShouldEqual(1);
        deserialized["Gamma"].Count().ShouldEqual(3);
    }

    [Test]
    public void RoundTrip_Should_Preserve_Data_With_Int_Keys()
    {
        var original = new Multimap<int, string>
        {
            { 1, "A" },
            { 1, "B" },
            { 2, "C" },
            { 3, "D" },
            { 3, "E" }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Multimap<int, string>>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldEqual(original.Count);
        deserialized[1].Count().ShouldEqual(2);
        deserialized[2].Count().ShouldEqual(1);
        deserialized[3].Count().ShouldEqual(2);
    }

    [Test]
    public void Write_Should_Serialize_Empty_Multimap()
    {
        var multimap = new Multimap<string, int>();

        var json = JsonSerializer.Serialize(multimap, _options);

        json.ShouldNotBeNull();
        json.ShouldEqual("[]");
    }

    [Test]
    public void Read_Should_Deserialize_Empty_Multimap()
    {
        var json = "[]";

        var multimap = JsonSerializer.Deserialize<Multimap<string, int>>(json, _options);

        multimap.ShouldNotBeNull();
        multimap.Count.ShouldEqual(0);
    }

    [Test]
    public void RoundTrip_Should_Handle_Complex_Value_Types()
    {
        var original = new Multimap<string, DateTime>
        {
            { "Dates1", new DateTime(2025, 1, 1) },
            { "Dates1", new DateTime(2025, 1, 2) },
            { "Dates2", new DateTime(2025, 12, 31) }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Multimap<string, DateTime>>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldEqual(2);
        deserialized["Dates1"].Count().ShouldEqual(2);
        deserialized["Dates2"].Count().ShouldEqual(1);
    }

    [Test]
    public void Should_Handle_Polymorph_Types()
    {
        var multimap = new Multimap<int, BaseEntity>
        {
            { 1, new ProductAttribute { Name = "Attr" } },
            { 1, new Product { Name = "Product" } },
            { 2, new ProductReview { ReviewText = "Good" } },
            { 2, new ProductTag { Name = "Tag" } }
        };
        
        var json = JsonSerializer.Serialize(multimap, _options);
        json.ShouldNotBeNull();

        var multimap2 = JsonSerializer.Deserialize<Multimap<int, BaseEntity>>(json, _options);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(multimap2.TotalValueCount, Is.EqualTo(4));
            Assert.That(multimap2[1].ElementAt(0), Is.TypeOf<ProductAttribute>());
            Assert.That(multimap2[1].ElementAt(1), Is.TypeOf<Product>());
            Assert.That(multimap2[2].ElementAt(0), Is.TypeOf<ProductReview>());
            Assert.That(multimap2[2].ElementAt(1), Is.TypeOf<ProductTag>());
        }
    }

    [Test]
    public void Should_Handle_Concurrent_Polymorph_Types()
    {
        var multimap = new ConcurrentMultimap<int, BaseEntity>();
        multimap.TryAdd(1, new ProductAttribute { Name = "Attr" });
        multimap.TryAdd(1, new Product { Name = "Product" });
        multimap.TryAdd(2, new ProductReview { ReviewText = "Good" });
        multimap.TryAdd(2, new ProductTag { Name = "Tag" });

        var json = JsonSerializer.Serialize(multimap, _options);
        json.ShouldNotBeNull();

        var multimap2 = JsonSerializer.Deserialize<ConcurrentMultimap<int, BaseEntity>>(json, _options);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(multimap2.TotalValueCount, Is.EqualTo(4));
            Assert.That(multimap2[1].ElementAt(0), Is.TypeOf<ProductAttribute>());
            Assert.That(multimap2[1].ElementAt(1), Is.TypeOf<Product>());
            Assert.That(multimap2[2].ElementAt(0), Is.TypeOf<ProductReview>());
            Assert.That(multimap2[2].ElementAt(1), Is.TypeOf<ProductTag>());
        }
    }

    //[Test]
    //public void Should_Handle_ObjectContainer()
    //{
    //    var entry = new CacheEntry
    //    {
    //        Key = "TestEntry",
    //        Value = new DbCacheEntry 
    //        { 
    //            Key = new DbCacheKey { Key = "Yo" }, 
    //            Value = new List<string> { "yo" }
    //        },
    //        CachedOn = DateTimeOffset.UtcNow,
    //        Priority = CacheEntryPriority.High
    //    };

    //    var json = JsonSerializer.Serialize(entry, _options);
    //    json.ShouldNotBeNull();

    //    var entry2 = JsonSerializer.Deserialize<CacheEntry>(json, _options);
    //}
}