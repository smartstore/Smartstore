using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text.Json;
using System.Xml.Linq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Core.Tests.Content.Media.Icons;

/// <summary>
/// Exercises local discovery, address resolution, SVG preparation and distributed payloads.
/// </summary>
[TestFixture]
public class IconServiceTests
{
    private const string _svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"-1 -2 24 25\" width=\"24\" height=\"25\" fill=\"none\"><path d=\"M0 0L1 1\" stroke=\"currentColor\" stroke-width=\"1\" stroke-linecap=\"butt\"/></svg>";
    private string _root;
    private PhysicalFileProvider _provider;
    private CancellationTokenSource _changes;
    private Mock<IApplicationContext> _context;
    private Mock<IIconCache> _cache;
    private ConcurrentDictionary<string, string> _entries;
    private IconService _service;
    private ConcurrentDictionary<string, int> _sourceLookups;

    /// <summary>
    /// Creates an isolated source tree and a serialized cache.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Smartstore.Full.sln")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null);
        _root = Path.Combine(directory.FullName, ".temp", "icon-api", "tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _provider = new PhysicalFileProvider(_root);
        _changes = new CancellationTokenSource();
        var files = new Mock<IFileSystem>();
        files.SetupGet(x => x.Root).Returns(_root);
        _sourceLookups = new ConcurrentDictionary<string, int>();
        files.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) =>
        {
            _sourceLookups.AddOrUpdate(path, 1, (_, count) => count + 1);
            return _provider.GetFileInfo(path);
        });
        files.Setup(x => x.GetDirectoryContents(It.IsAny<string>())).Returns((string path) => _provider.GetDirectoryContents(path));
        files.Setup(x => x.Watch(It.IsAny<string>())).Returns(() => new CancellationChangeToken(_changes.Token));
        _context = new Mock<IApplicationContext>();
        _context.SetupGet(x => x.AppDataRoot).Returns(files.Object);
        _entries = new ConcurrentDictionary<string, string>();
        _cache = new Mock<IIconCache>();
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken ct) => Task.FromResult(_entries.TryGetValue(key, out string value) ? JsonSerializer.Deserialize<IconSvg>(value) : null));
        _cache.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()))
            .Returns((string key, IconSvg svg, CancellationToken ct) =>
            {
                _entries[key] = JsonSerializer.Serialize(svg);
                return Task.CompletedTask;
            });
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        WriteLibrary("hugeicons", "hi", "sr");
        WriteZip("hugeicons", "rounded", ("cart-01", _svg), ("direct", _svg), ("package-add-01 ", _svg));
        WriteZip("hugeicons", "sharp", ("cart-01", _svg));
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","direct":"missing"}""");
        Write("Icons/hugeicons/metadata.json", """{"icons":{"cart-01":{"tags":["basket"]},"direct":{}}}""");
        _service = new IconService(_context.Object, _cache.Object);
    }

    /// <summary>
    /// Exports compact resolution data once, preserves PathBase and leaves individual SVG caching untouched.
    /// </summary>
    [Test]
    public async Task Browser_Manifest_Is_Compact_Immutable_And_PathBase_Independent()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01?flip=x&stroke-scale=1.1","same":"same"}""");
        var http = new DefaultHttpContext();
        http.Request.PathBase = "/shop";
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor { HttpContext = http });
        var url = kits.GetManifestUrl();
        Assert.That(url, Does.StartWith("/shop/icons/manifest/"));
        var revision = Path.GetFileNameWithoutExtension(url);
        var paths = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => kits.GetManifestFileAsync(revision)));
        Assert.That(paths.Distinct().Count(), Is.EqualTo(1));
        Assert.That(Path.GetDirectoryName(paths[0]), Is.EqualTo(Path.Combine(_root, ".cache", "IconKits")));
        Assert.That(Path.GetFileName(paths[0]), Is.EqualTo("manifest-" + revision + ".json"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(paths[0]));
        var library = json.RootElement.GetProperty("libraries").GetProperty("hugeicons");
        Assert.That(library.GetProperty("mapping").GetProperty("cart").GetString(), Is.EqualTo("cart-01?flip=x&stroke-scale=1.1"));
        Assert.That(library.GetProperty("mapping").TryGetProperty("same", out _), Is.False);
        Assert.That(json.RootElement.TryGetProperty("symbols", out _), Is.False);
        Assert.That(json.RootElement.GetProperty("urls").GetProperty("hi@sr").GetProperty("shared").GetString(), Does.StartWith("icons/shared-"));
        http.Request.PathBase = "/other";
        Assert.That(kits.GetManifestUrl(), Is.EqualTo("/other/icons/manifest/" + revision + ".json"));
        _cache.VerifyNoOtherCalls();

        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01?rotate=90"}""");
        SignalChanges();
        Assert.That(kits.GetManifestUrl(), Does.Not.EndWith(revision + ".json"));
        Assert.That(await kits.GetManifestFileAsync(revision), Is.EqualTo(paths[0]));
        Assert.That(await kits.GetManifestFileAsync(new string('0', 24)), Is.Null);
        Assert.That(await kits.GetManifestFileAsync("../escape"), Is.Null);
    }

    /// <summary>
    /// Removes only the fixture's generated directory.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _provider?.Dispose();
        _changes?.Dispose();
        if (_root != null && Path.GetFileName(_root).Length == 32 && Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    /// <summary>
    /// Parses short and qualified forms without losing filename characters.
    /// </summary>
    [TestCase("cart")]
    [TestCase("hi:cart")]
    [TestCase("cart@sr")]
    [TestCase("hi:cart@sr")]
    [TestCase("hi:package-add-01 @rounded")]
    [TestCase("c++")]
    [TestCase("cart!")]
    [TestCase("hi:cart!")]
    [TestCase("cart!@sr")]
    [TestCase("hi:cart!@sr")]
    public void Address_Roundtrips(string value)
    {
        Assert.That(IconAddress.Parse(value).ToString(), Is.EqualTo(value));
    }

    /// <summary>
    /// Rejects malformed addresses and paths.
    /// </summary>
    [TestCase("")]
    [TestCase(":cart")]
    [TestCase("cart@")]
    [TestCase("hi:cart:other")]
    [TestCase("cart@a@b")]
    [TestCase("../cart")]
    [TestCase("cart@a:hi")]
    [TestCase("!")]
    [TestCase("hi:!@sr")]
    [TestCase("cart!!")]
    [TestCase("ca!rt")]
    [TestCase("cart@sr!")]
    public void Address_Rejects_Malformed_Input(string value)
    {
        Assert.That(IconAddress.TryParse(value, out _), Is.False);
    }

    /// <summary>
    /// The bypass marker is address state rather than part of the source filename.
    /// </summary>
    [Test]
    public void Address_Preserves_Mapping_Bypass()
    {
        IconAddress address = "HI:cart!@SR";
        Assert.That(address.Name, Is.EqualTo("cart"));
        Assert.That(address.SkipMapping, Is.True);
        Assert.That(address, Is.EqualTo(new IconAddress("cart", "hi", "sr", skipMapping: true)));
        Assert.That(address, Is.Not.EqualTo(new IconAddress("cart", "hi", "sr")));
        Assert.That((string)address, Is.EqualTo("hi:cart!@sr"));
    }

    /// <summary>
    /// Direct lookup ignores mappings, including mappings whose target is missing.
    /// </summary>
    /// <param name="address">The direct address with optional qualifiers.</param>
    [TestCase("direct!")]
    [TestCase("hi:direct!@sr")]
    public async Task Direct_Address_Skips_Mapping(string address)
    {
        var icon = await _service.GetIconAsync(address);
        Assert.That(icon.Name, Is.EqualTo("direct"));
        Assert.That(icon.Address, Is.EqualTo("hi:direct@sr"));
        Assert.That(await _service.GetIconAsync("cart!"), Is.Null);
        Mock.Get(_context.Object.AppDataRoot).Verify(x => x.GetFileInfo("Icons/hugeicons/mapping.json"), Times.Never);
    }

    /// <summary>
    /// Mapped and direct addresses share the same prepared payload cache entry.
    /// </summary>
    [Test]
    public async Task Direct_Address_Uses_Canonical_Cache_Key()
    {
        var mapped = await _service.GetSvgAsync("cart");
        var direct = await _service.GetSvgAsync("hi:cart-01!@sr");
        Assert.That(direct.Address, Is.EqualTo(mapped.Address));
        Assert.That(direct.Revision, Is.EqualTo(mapped.Revision));
        Assert.That(_entries.Count, Is.EqualTo(1));
        Assert.That(_entries.Keys.Single(), Does.Not.Contain("!"));
    }

    /// <summary>
    /// Normalizes selectors while retaining the exact source filename.
    /// </summary>
    [Test]
    public void Address_Normalizes_Only_Selectors()
    {
        Assert.That(IconAddress.Parse("HI:Cart Name @Stroke-Rounded").ToString(), Is.EqualTo("hi:Cart Name @stroke-rounded"));
        var name = new string('x', 20);
        Assert.That(IconAddress.Parse(name).Name, Is.SameAs(name));
    }

    /// <summary>
    /// Keeps malformed input and the common unqualified-name path allocation-free after warmup.
    /// </summary>
    [Test]
    public void Address_Common_Parsing_Does_Not_Allocate()
    {
        for (var i = 0; i < 1000; i++)
        {
            IconAddress.TryParse("cart", out _);
            IconAddress.TryParse("hi:cart@bad/variant", out _);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            IconAddress.TryParse("cart", out _);
            IconAddress.TryParse("hi:cart@bad/variant", out _);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
    }

    /// <summary>
    /// Supports value equality and implicit syntax-only string conversions.
    /// </summary>
    [Test]
    public async Task Address_Is_A_Value_With_String_Conversions()
    {
        IconAddress address = "hi:cart@sr";
        string text = address;
        Assert.That(text, Is.EqualTo("hi:cart@sr"));
        Assert.That(address, Is.EqualTo(new IconAddress("cart", "hi", "sr")));
        Assert.That((await _service.GetIconAsync(address)).Name, Is.EqualTo("cart-01"));
        Assert.That(default(IconAddress).IsEmpty, Is.True);
        Assert.That(default(IconAddress).ToString(), Is.Empty);
        Assert.Throws<FormatException>(() => { IconAddress invalid = "hi:"; });
    }

    /// <summary>
    /// Defers all source access until the first operation.
    /// </summary>
    [Test]
    public void Constructor_Is_Lazy()
    {
        var context = new Mock<IApplicationContext>(MockBehavior.Strict);
        _ = new IconService(context.Object, _cache.Object);
        context.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Resolves both kinds of library and variant identifiers to a canonical address.
    /// </summary>
    [TestCase("cart", null, null)]
    [TestCase("hi:cart@sr", "hugeicons", "rounded")]
    [TestCase("hugeicons:cart@rounded", "hi", "sr")]
    public async Task Mapping_And_Short_Names_Are_Implicit(string address, string library, string variant)
    {
        var icon = await _service.GetIconAsync(address, library, variant);
        Assert.That(icon.Address, Is.EqualTo("hi:cart-01@sr"));
        Assert.That(icon.VariantName, Is.EqualTo("rounded"));
        Assert.That(_service.DefaultLibrary.SystemName, Is.EqualTo("hugeicons"));
        Assert.That(_service.GetLibrary("hi").Variants["rounded"].ShortName, Is.EqualTo("sr"));
    }

    /// <summary>
    /// Uses full names when short names are omitted.
    /// </summary>
    [Test]
    public async Task Short_Names_Are_Optional()
    {
        WriteLibrary("hugeicons", null, null);
        var icon = await _service.GetIconAsync("cart");
        Assert.That(icon.Address, Is.EqualTo("hugeicons:cart-01@rounded"));
    }

    /// <summary>
    /// Does not fall back to a real icon when its mapping points to a missing target.
    /// </summary>
    [Test]
    public async Task Missing_Mapping_Target_Does_Not_Fall_Back()
    {
        Assert.That(await _service.GetIconAsync("direct"), Is.Null);
        Assert.That(await _service.GetSvgAsync("unknown:cart"), Is.Null);
        Assert.That(await _service.GetIconAsync("cart@missing"), Is.Null);
        Assert.That(await _service.GetIconAsync("package-add-01 "), Is.Not.Null);
    }

    /// <summary>
    /// Rejects explicitly conflicting selections.
    /// </summary>
    [Test]
    public void Conflicting_Qualifiers_Are_Rejected()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _service.GetSvgAsync("hi:cart@sr", variant: "sharp"));
        Assert.ThrowsAsync<ArgumentException>(() => _service.GetSvgAsync("hi:cart", library: "missing"));
    }

    /// <summary>
    /// Applies the global variant only to the default library.
    /// </summary>
    [Test]
    public async Task Other_Library_Uses_Its_Own_Default()
    {
        WriteLibrary("bootstrap", "bi", null);
        WriteZip("bootstrap", "rounded", ("cart", _svg));
        WriteZip("bootstrap", "sharp", ("cart", _svg));
        var icon = await _service.GetIconAsync("bi:cart");
        Assert.That(icon.VariantName, Is.EqualTo("sharp"));
    }

    /// <summary>
    /// Reports ambiguous short names instead of choosing arbitrarily.
    /// </summary>
    [Test]
    public void Duplicate_Short_Names_Are_Configuration_Errors()
    {
        WriteLibrary("bootstrap", "hi", null);
        WriteZip("bootstrap", "rounded", ("cart", _svg));
        WriteZip("bootstrap", "sharp", ("cart", _svg));
        Assert.Throws<InvalidDataException>(() => _service.GetLibrary("hi"));
    }

    /// <summary>
    /// Rejects a variant short name that shadows another variant's directory name.
    /// </summary>
    [Test]
    public void Variant_Short_Names_Must_Be_Unambiguous()
    {
        WriteLibrary("hugeicons", "hi", "sharp");
        Assert.Throws<InvalidDataException>(() => _service.GetLibrary("hi"));
    }

    /// <summary>
    /// Validates every SVG in the locally installed HugeIcons package.
    /// </summary>
    [Test, Explicit("Validates the optional local icon package; not required on CI.")]
    public async Task Installed_HugeIcons_Package_Is_Supported()
    {
        var workspace = new DirectoryInfo(_root).Parent.Parent.Parent.Parent;
        using var files = new LocalFileSystem(Path.Combine(workspace.FullName, "Smartstore", "src", "Smartstore.Web", "App_Data"));
        var context = new Mock<IApplicationContext>();
        context.SetupGet(x => x.AppDataRoot).Returns(files);
        var service = new IconService(context.Object, _cache.Object);
        int count = 0;
        for (int skip = 0; ; skip += 500)
        {
            var page = await service.SearchAsync(new IconSearchQuery { Library = "hugeicons", Skip = skip, Take = 500 });
            foreach (var icon in page.Items)
            {
                Assert.That(await service.GetSvgAsync(icon.Address), Is.Not.Null, icon.Address);
                count++;
            }

            if (count >= page.TotalCount)
            {
                break;
            }
        }

        Assert.That(count, Is.GreaterThan(6000));
        TestContext.Out.WriteLine($"Validated {count} installed SVGs.");
    }

    /// <summary>
    /// Preserves coordinates while applying the temporary direct-attribute rendering experiment.
    /// </summary>
    [Test]
    public async Task Svg_Is_Prepared_And_Serializable()
    {
        var svg = await _service.GetSvgAsync("cart");
        Assert.That(svg.ViewBox, Is.EqualTo("-1 -2 24 25"));
        Assert.That(svg.RootAttributes.ContainsKey("width"), Is.False);
        Assert.That(svg.RootAttributes.ContainsKey("height"), Is.False);
        Assert.That(svg.RootAttributes.Keys.Any(x => x == "stroke" || x.StartsWith("stroke-", StringComparison.Ordinal)), Is.False);
        Assert.That(svg.RootAttributes["fill"], Is.EqualTo("none"));
        Assert.That(svg.Content, Does.Not.Contain("stroke-width="));
        Assert.That(svg.Content, Does.Contain("stroke-linecap=\"butt\""));
        Assert.That(svg.Content, Does.Not.Contain("stroke="));
        Assert.That(svg.Content, Does.Contain("stroke:var(--icon-stroke,currentColor);"));
        Assert.That(svg.Content, Does.Contain("stroke-width:var(--icon-stroke-width,1.6);"));
        Assert.That(svg.RootAttributes.ContainsKey("style"), Is.False);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)svg.RootAttributes)["fill"] = "red");
        var cached = await _service.GetSvgAsync("hi:cart-01@sr");
        Assert.That(cached.RootAttributes["fill"], Is.EqualTo("none"));
        Assert.That(cached.Content, Is.EqualTo(svg.Content));
        _cache.Verify(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Finds actual names, tags and conceptual names, with total count before paging.
    /// </summary>
    [TestCase("basket")]
    [TestCase("cart")]
    public async Task Search_Uses_Metadata_And_Mapping(string term)
    {
        var results = await _service.SearchAsync(new IconSearchQuery { Term = term, Take = 1 });
        Assert.That(results.TotalCount, Is.EqualTo(1));
        Assert.That(results.Items[0].Name, Is.EqualTo("cart-01"));
    }

    /// <summary>
    /// Unions shared tag matches, intersects words across fields and excludes unavailable targets before paging.
    /// </summary>
    [Test]
    public async Task Search_Combines_Fields_And_Filters_By_Variant()
    {
        Write("Icons/hugeicons/metadata.json", """{"icons":{"cart-01":{"tags":["basket","shared"]},"direct":{"tags":["shared"]},"missing":{"tags":["shared"]}}}""");
        Write("Icons/hugeicons/mapping.json", """{"shopping":"cart-01","ghost":"missing"}""");
        var shared = await _service.SearchAsync(new IconSearchQuery { Term = "SHARED", Skip = 1, Take = 1 });
        Assert.That(shared.TotalCount, Is.EqualTo(2));
        Assert.That(shared.Items.Single().Name, Is.EqualTo("direct"));
        var combined = await _service.SearchAsync(new IconSearchQuery { Term = "SHOP bask 01" });
        Assert.That(combined.Items.Single().Name, Is.EqualTo("cart-01"));
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "ghost" })).TotalCount, Is.Zero);
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "shared", Variant = "sharp" })).TotalCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Includes substring matches for names, tags and aliases even when an exact match exists.
    /// </summary>
    /// <param name="term">An exact searchable name, tag or alias.</param>
    [TestCase("CART-01")]
    [TestCase("BASKET")]
    [TestCase("CART")]
    public async Task Search_Includes_Exact_And_Partial_Matches(string term)
    {
        Write("Icons/hugeicons/rounded/icons/cart-01-extra.svg", _svg);
        Write("Icons/hugeicons/metadata.json", """{"icons":{"cart-01":{"tags":["basket"]},"cart-01-extra":{"tags":["basket-extra"]}}}""");
        var exact = await _service.SearchAsync(new IconSearchQuery { Term = term });
        Assert.That(exact.TotalCount, Is.EqualTo(2));
        Assert.That(exact.Items.Select(x => x.Name), Is.EqualTo(new[] { "cart-01", "cart-01-extra" }));
        var partial = await _service.SearchAsync(new IconSearchQuery { Term = "bask" });
        Assert.That(partial.TotalCount, Is.EqualTo(2));
        var combined = await _service.SearchAsync(new IconSearchQuery { Term = "cart bask" });
        Assert.That(combined.TotalCount, Is.EqualTo(2));
    }

    /// <summary>
    /// Reuses the name index for repeated searches and rebuilds searchable tags after watcher invalidation.
    /// </summary>
    [Test]
    public async Task Search_Reuses_Names_And_Reloads_Tags_After_Invalidation()
    {
        await _service.SearchAsync(new IconSearchQuery { Term = "basket" });
        await _service.SearchAsync(new IconSearchQuery { Term = "cart" });
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetDirectoryContents("Icons/hugeicons/rounded/user"), Times.Once);
        files.Verify(x => x.GetDirectoryContents("Icons/hugeicons/rounded/icons"), Times.Once);
        Write("Icons/hugeicons/metadata.json", """{"icons":{"cart-01":{"tags":["trolley"]}}}""");
        SignalChanges();
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "basket" })).TotalCount, Is.Zero);
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "trolley" })).TotalCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Reloads overrides and changes revisions even when timestamps and lengths match.
    /// </summary>
    [Test]
    public async Task Source_Changes_Invalidate_Revision()
    {
        var original = await _service.GetSvgAsync("cart");
        string overridePath = "Icons/hugeicons/rounded/icons/cart-01.svg";
        Write(overridePath, _svg.Replace("M0 0L1 1", "M0 0L2 2"));
        SignalChanges();
        var overridden = await _service.GetSvgAsync("cart");
        Assert.That(overridden.Revision, Is.Not.EqualTo(original.Revision));
        Assert.That(overridden.Content, Does.Contain("M0 0L2 2"));
        DateTime timestamp = File.GetLastWriteTimeUtc(Path.Combine(_root, overridePath));
        Write(overridePath, _svg.Replace("M0 0L1 1", "M0 0L3 3"));
        File.SetLastWriteTimeUtc(Path.Combine(_root, overridePath), timestamp);
        SignalChanges();
        var modified = await _service.GetSvgAsync("cart");
        Assert.That(modified.Revision, Is.Not.EqualTo(overridden.Revision));
        Assert.That(modified.Content, Does.Contain("M0 0L3 3"));
        var otherService = new IconService(_context.Object, _cache.Object);
        Assert.That((await otherService.GetSvgAsync("cart")).Revision, Is.EqualTo(modified.Revision));
    }

    /// <summary>
    /// Rejects active and externally referencing SVG sources.
    /// </summary>
    [TestCase("<svg><script>alert(1)</script></svg>")]
    [TestCase("<svg onload=\"alert(1)\"/>")]
    [TestCase("<svg><use href=\"https://example.com/icon.svg\"/></svg>")]
    [TestCase("<svg><path style=\"fill:red\"/></svg>")]
    [TestCase("<svg><path fill=\"url(https://example.com/image)\"/></svg>")]
    public void Unsafe_Svg_Is_Rejected(string svg)
    {
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", svg);
        Assert.ThrowsAsync<InvalidDataException>(() => _service.GetSvgAsync("cart"));
    }

    /// <summary>
    /// Shares immutable manifests and prevents collection mutation through alternate interfaces.
    /// </summary>
    [Test]
    public async Task Library_Results_Are_Immutable_And_Shared()
    {
        var library = _service.DefaultLibrary;
        Assert.That(_service.GetLibrary("hi"), Is.SameAs(library));
        Assert.That((await _service.GetLibrariesAsync())[0], Is.SameAs(library));
        var dictionary = (IDictionary<string, IconVariant>)library.Variants;
        Assert.Throws<NotSupportedException>(() => dictionary.Clear());
        var changed = library with { ShortName = "changed" };
        Assert.That(changed.ShortName, Is.EqualTo("changed"));
        Assert.That(_service.DefaultLibrary.ShortName, Is.EqualTo("hi"));
    }

    /// <summary>
    /// Freezes input collections and supports System.Text.Json with init-only manifest properties.
    /// </summary>
    [Test]
    public void Library_Initialization_Detaches_Collections_And_Roundtrips_Json()
    {
        var variants = new Dictionary<string, IconVariant>
        {
            ["rounded"] = new IconVariant { DisplayName = "Stroke Rounded", DefaultViewBox = "0 0 24 24", ShortName = "sr", StrokeWidthScale = 1.5 }
        };
        var library = new IconLibrary { ShortName = "hi", DefaultVariant = "rounded", Variants = variants };
        variants.Clear();
        Assert.That(library.Variants.Count, Is.EqualTo(1));
        var restored = JsonSerializer.Deserialize<IconLibrary>(JsonSerializer.Serialize(library));
        Assert.That(restored.ShortName, Is.EqualTo("hi"));
        Assert.That(restored.Variants["ROUNDED"].StrokeWidthScale, Is.EqualTo(1.5));
        Assert.That(restored.Variants["ROUNDED"].DisplayName, Is.EqualTo("Stroke Rounded"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, IconVariant>)restored.Variants).Clear());
    }

    /// <summary>
    /// Coalesces concurrent cache misses for the same canonical icon.
    /// </summary>
    [Test]
    public async Task Concurrent_Misses_Prepare_One_Payload()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cache.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()))
            .Returns(async (string key, IconSvg svg, CancellationToken ct) =>
            {
                started.TrySetResult();
                await release.Task;
                _entries[key] = JsonSerializer.Serialize(svg);
            });
        Task<IconSvg> first = _service.GetSvgAsync("cart");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<IconSvg>[] following = Enumerable.Range(0, 8).Select(_ => _service.GetSvgAsync("hi:cart-01@sr")).ToArray();
        release.SetResult();
        await Task.WhenAll(following.Append(first)).WaitAsync(TimeSpan.FromSeconds(5));
        _cache.Verify(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Discovers new override icons even without metadata entries.
    /// </summary>
    /// <param name="layer">The loose-file layer containing the additional icon.</param>
    [TestCase("user")]
    [TestCase("icons")]
    public async Task Override_Can_Add_An_Icon(string layer)
    {
        Write($"Icons/hugeicons/rounded/{layer}/custom.svg", _svg);
        Assert.That(await _service.GetSvgAsync("custom"), Is.Not.Null);
        var result = await _service.SearchAsync(new IconSearchQuery { Term = "custom" });
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items[0].Tags, Is.Empty);
    }

    /// <summary>
    /// A custom library needs no archive, mapping or metadata files to resolve and search loose icons.
    /// </summary>
    /// <param name="layer">The directory supplying the custom SVG.</param>
    [TestCase("user")]
    [TestCase("icons")]
    public async Task Library_Without_Archive_Supports_Lookup_And_Search(string layer)
    {
        WriteLibrary("system", "sys", null);
        Write($"Icons/system/sharp/{layer}/custom.svg", _svg);
        Write("Icons/config.json", """{"defaultLibrary":"system"}""");

        Assert.That(_service.DefaultLibrary.SystemName, Is.EqualTo("system"));
        var icon = await _service.GetIconAsync("custom");
        Assert.That(icon.Address, Is.EqualTo("sys:custom@sharp"));
        Assert.That(await _service.GetSvgAsync(icon), Is.Not.Null);
        var result = await _service.SearchAsync(new IconSearchQuery());
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items[0].Name, Is.EqualTo("custom"));
        Assert.That(await _service.GetIconAsync("missing"), Is.Null);
        Assert.That(await _service.GetSvgAsync("missing"), Is.Null);

        // Even a declared variant with no source files is simply empty.
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Variant = "rounded" })).TotalCount, Is.Zero);
    }

    /// <summary>
    /// Layer priority survives additions and removals while search returns each name only once.
    /// </summary>
    [Test]
    public async Task User_Overrides_System_Then_Archive()
    {
        var archived = await _service.GetSvgAsync("cart");
        var systemPath = "Icons/hugeicons/rounded/icons/cart-01.svg";
        var userPath = "Icons/hugeicons/rounded/user/cart-01.svg";
        Write(systemPath, _svg.Replace("M0 0L1 1", "M0 0L2 2"));
        SignalChanges();
        var system = await _service.GetSvgAsync("cart");
        Assert.That(system.Content, Does.Contain("M0 0L2 2"));
        Assert.That(system.Revision, Is.Not.EqualTo(archived.Revision));

        Write(userPath, _svg.Replace("M0 0L1 1", "M0 0L3 3"));
        SignalChanges();
        var user = await _service.GetSvgAsync("cart");
        Assert.That(user.Content, Does.Contain("M0 0L3 3"));
        Assert.That(user.Revision, Is.Not.EqualTo(system.Revision));
        var result = await _service.SearchAsync(new IconSearchQuery { Term = "cart-01" });
        Assert.That(result.TotalCount, Is.EqualTo(1));

        File.Delete(Path.Combine(_root, userPath));
        SignalChanges();
        Assert.That((await _service.GetSvgAsync("cart")).Revision, Is.EqualTo(system.Revision));
        File.Delete(Path.Combine(_root, systemPath));
        SignalChanges();
        Assert.That((await _service.GetSvgAsync("cart")).Revision, Is.EqualTo(archived.Revision));
    }

    /// <summary>
    /// A user SVG prevents reads of broken lower layers and carries its own source revision.
    /// </summary>
    [Test]
    public async Task User_Lookup_Does_Not_Read_Lower_Layers()
    {
        Write("Icons/hugeicons/rounded/user/cart-01.svg", _svg);
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", "not SVG");
        Write("Icons/hugeicons/rounded/icons.zip", "not ZIP");
        Assert.That(await _service.GetSvgAsync("cart"), Is.Not.Null);
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons/cart-01.svg"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons.zip"), Times.Never);
    }

    /// <summary>
    /// Identical bytes in different loose-file layers still have distinct source revisions.
    /// </summary>
    [Test]
    public async Task Loose_File_Revision_Includes_Layer()
    {
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", _svg);
        var system = await _service.GetSvgAsync("cart");
        Write("Icons/hugeicons/rounded/user/cart-01.svg", _svg);
        SignalChanges();
        var user = await _service.GetSvgAsync("cart");
        Assert.That(user.Content, Is.EqualTo(system.Content));
        Assert.That(user.Revision, Is.Not.EqualTo(system.Revision));
    }

    /// <summary>
    /// An invalid user customization cannot silently fall back to a valid system icon.
    /// </summary>
    [Test]
    public void Invalid_User_Icon_Does_Not_Fall_Back()
    {
        Write("Icons/hugeicons/rounded/user/cart-01.svg", "<svg><script/></svg>");
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", _svg);
        Assert.ThrowsAsync<InvalidDataException>(() => _service.GetSvgAsync("cart"));
    }

    /// <summary>
    /// Requires system names in root defaults and rejects unavailable variants.
    /// </summary>
    [TestCase("{\"defaultLibrary\":\"hi\"}")]
    [TestCase("{\"defaultLibrary\":\"hugeicons\",\"defaultVariant\":\"missing\"}")]
    public void Invalid_Defaults_Are_Configuration_Errors(string config)
    {
        Write("Icons/config.json", config);
        Assert.Throws<InvalidDataException>(() => _service.GetLibrary("hi"));
    }

    /// <summary>
    /// Uses the variant default only when no viewBox is supplied.
    /// </summary>
    [Test]
    public async Task Missing_ViewBox_Uses_Default()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", "<svg><path d=\"M0 0L1 1\"/></svg>");
        Assert.That((await _service.GetSvgAsync("custom")).ViewBox, Is.EqualTo("0 0 24 24"));
    }

    /// <summary>
    /// Preserves a supplied viewBox verbatim without validating its coordinate values.
    /// </summary>
    /// <param name="viewBox">The source attribute value.</param>
    [TestCase("0 0 16 16")]
    [TestCase("not-coordinates")]
    [TestCase("")]
    public async Task Source_ViewBox_Takes_Precedence_Without_Validation(string viewBox)
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", $"<svg viewBox=\"{viewBox}\"><path d=\"M0 0L1 1\"/></svg>");
        Assert.That((await _service.GetSvgAsync("custom")).ViewBox, Is.EqualTo(viewBox));
    }

    /// <summary>
    /// Missing source and default coordinates omit inline output and kit symbols without caching null payloads.
    /// </summary>
    [Test]
    public async Task Missing_ViewBox_And_Default_Skip_Icon_And_Kit_Symbol()
    {
        Write("Icons/hugeicons/library.json", """{"defaultVariant":"rounded","variants":{"rounded":{}}}""");
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", "<svg><path d=\"M0 0L1 1\"/></svg>");
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        Assert.That(await _service.GetSvgAsync("cart"), Is.Null);
        _cache.Verify(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()), Times.Never);
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var url = kits.GetUrl("shared");
        var path = await kits.GetSpriteFileAsync("shared", url[^28..^4]);
        var sprite = XElement.Parse(File.ReadAllText(path));
        Assert.That(sprite.Descendants().Any(x => x.Name.LocalName == "symbol"), Is.False);
    }

    /// <summary>
    /// Preserves local references without allowing externally loaded resources.
    /// </summary>
    [Test]
    public async Task Local_References_Are_Preserved()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", """
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
                <defs><path id="shape" d="M0 0L1 1"/></defs><use xlink:href="#shape"/>
            </svg>
            """);
        var svg = await _service.GetSvgAsync("custom");
        Assert.That(svg.Content, Does.Contain("href=\"#shape\""));
        Assert.That(svg.Content, Does.Not.Contain("xmlns"));
    }

    /// <summary>
    /// Observes cancellation before lazy source discovery.
    /// </summary>
    [Test]
    public void Cancellation_Precedes_Discovery()
    {
        var context = new Mock<IApplicationContext>(MockBehavior.Strict);
        var service = new IconService(context.Object, _cache.Object);
        Assert.ThrowsAsync<OperationCanceledException>(() => service.GetSvgAsync("cart", cancelToken: new CancellationToken(true)));
        context.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Shares immutable payloads at both boundaries of the in-process cache.
    /// </summary>
    [Test]
    public async Task Cache_Reuses_Immutable_Payloads()
    {
        IconSvg stored = null;
        var manager = new Mock<ICacheManager>();
        manager.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CacheEntryOptions>()))
            .Callback((string key, object value, CacheEntryOptions options) => stored = (IconSvg)value).Returns(Task.CompletedTask);
        manager.Setup(x => x.GetAsync<IconSvg>(It.IsAny<string>(), false)).Returns(() => Task.FromResult(stored));
        var cache = new IconCache(manager.Object);
        var attributes = new Dictionary<string, string> { ["fill"] = "none" };
        var payload = new IconSvg { RootAttributes = attributes };
        attributes["fill"] = "red";
        await cache.PutAsync("hi:cart@sr:abcd", payload);
        Assert.That(stored, Is.SameAs(payload));
        var first = await cache.GetAsync("hi:cart@sr:abcd");
        Assert.That(first, Is.SameAs(payload));
        Assert.That(await cache.GetAsync("hi:cart@sr:abcd"), Is.SameAs(payload));
        Assert.That(first.RootAttributes["fill"], Is.EqualTo("none"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)first.RootAttributes)["fill"] = "blue");
        var restored = JsonSerializer.Deserialize<IconSvg>(JsonSerializer.Serialize(payload));
        Assert.That(restored.RootAttributes["fill"], Is.EqualTo("none"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)restored.RootAttributes)["fill"] = "blue");
        await cache.InvalidateLibraryAsync("hi");
        manager.Verify(x => x.RemoveByPatternAsync("icons:svg:hi:*"), Times.Once);
    }

    /// <summary>
    /// Cache inspection exposes parsed identities, preserves compound revisions and never loads SVG payloads.
    /// </summary>
    [Test]
    public async Task Cache_Entries_Expose_Identity_Without_Loading_Payloads()
    {
        var manager = new Mock<ICacheManager>(MockBehavior.Strict);
        manager.Setup(x => x.KeysAsync("icons:svg:*")).Returns(Keys());
        var entries = new List<IconCacheEntry>();
        await foreach (var entry in new IconCache(manager.Object).GetEntriesAsync())
        {
            entries.Add(entry);
        }

        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries[0].Key, Is.EqualTo("hi:cart-01@sr:14hash:zip:abcd"));
        Assert.That(entries[0].Address.Name, Is.EqualTo("cart-01"));
        Assert.That(entries[0].Address.Library, Is.EqualTo("hi"));
        Assert.That(entries[0].Address.Variant, Is.EqualTo("sr"));
        Assert.That(entries[0].Revision, Is.EqualTo("14hash:zip:abcd"));
        Assert.That(entries[1].Address, Is.EqualTo(entries[0].Address));
        Assert.That(entries[1].Revision, Is.EqualTo("14hash:user:ef01"));
        manager.Verify(x => x.KeysAsync("icons:svg:*"), Times.Once);
        manager.VerifyNoOtherCalls();

        static async IAsyncEnumerable<string> Keys()
        {
            await Task.CompletedTask;
            yield return "icons:svg:hi:cart-01@sr:14hash:zip:abcd";
            yield return "icons:svg:hi:cart-01@sr:14hash:user:ef01";
        }
    }

    /// <summary>
    /// A warm cache needs no archive handle; an uncached icon still reads its source on demand.
    /// </summary>
    [Test]
    public async Task Cache_Hit_Does_Not_Open_Source_And_Catalog_Does_Not_Retain_Svg()
    {
        await _service.GetSvgAsync("cart");
        string archivePath = Path.Combine(_root, "Icons/hugeicons/rounded/icons.zip");
        // The fixture controls the change token, so deleting the archive leaves the index intact.
        // A byte-retaining catalog would incorrectly allow the second, uncached lookup to succeed.
        File.Delete(archivePath);
        Assert.That(await _service.GetSvgAsync("cart"), Is.Not.Null);
        Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetSvgAsync("package-add-01 "));
    }

    /// <summary>
    /// Overrides are reopened on a cache miss rather than retained as byte arrays in the catalog.
    /// </summary>
    [Test]
    public async Task Override_Content_Is_Not_Retained_By_Discovery()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", _svg);
        Assert.That(await _service.GetIconAsync("custom"), Is.Not.Null);
        File.Delete(Path.Combine(_root, "Icons/hugeicons/rounded/icons/custom.svg"));
        Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetSvgAsync("custom"));
    }

    /// <summary>
    /// A changed override cannot populate a cache entry under the previously discovered revision.
    /// </summary>
    [Test]
    public async Task Changed_Override_Is_Rejected_Before_Caching()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", _svg);
        Assert.That(await _service.GetIconAsync("custom"), Is.Not.Null);
        Write("Icons/hugeicons/rounded/icons/custom.svg", _svg.Replace("M0 0L1 1", "M0 0L2 2"));
        Assert.ThrowsAsync<IOException>(() => _service.GetSvgAsync("custom"));
        _cache.Verify(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A ZIP entry replaced before watcher notification is not cached under the old revision.
    /// </summary>
    [Test]
    public async Task Changed_Archive_Entry_Is_Rejected_Before_Caching()
    {
        Assert.That(await _service.GetIconAsync("cart"), Is.Not.Null);
        File.Delete(Path.Combine(_root, "Icons/hugeicons/rounded/icons.zip"));
        WriteZip("hugeicons", "rounded", ("cart-01", _svg.Replace("M0 0L1 1", "M0 0L2 2")));
        Assert.ThrowsAsync<IOException>(() => _service.GetSvgAsync("cart"));
        _cache.Verify(x => x.PutAsync(It.IsAny<string>(), It.IsAny<IconSvg>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Resolves fresh file objects on misses and performs no file lookups on cache hits.
    /// </summary>
    /// <param name="useOverride">Whether to exercise an override file instead of an archived icon.</param>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Source_File_Is_Resolved_Again_Only_On_Cache_Miss(bool useOverride)
    {
        string path = useOverride
            ? "Icons/hugeicons/rounded/icons/custom.svg"
            : "Icons/hugeicons/rounded/icons.zip";
        string name = useOverride ? "custom" : "cart";
        if (useOverride)
        {
            Write(path, _svg);
        }

        // Finish discovery before observing lookups. The index must not reuse its file objects.
        Assert.That(await _service.GetIconAsync(name), Is.Not.Null);
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Invocations.Clear();
        Assert.That(await _service.GetSvgAsync(name), Is.Not.Null);
        files.Verify(x => x.GetFileInfo(path), Times.Once);

        files.Invocations.Clear();
        Assert.That(await _service.GetSvgAsync(name), Is.Not.Null);
        files.Verify(x => x.GetFileInfo(It.IsAny<string>()), Times.Never);

        _entries.Clear();
        Assert.That(await _service.GetSvgAsync(name), Is.Not.Null);
        files.Verify(x => x.GetFileInfo(path), Times.Once);
    }

    /// <summary>
    /// The string extension maps once, while direct preparation uses the actual name unchanged.
    /// </summary>
    [Test]
    public async Task Resolved_Icons_Are_Not_Mapped_Again()
    {
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","cart-01":"missing"}""");
        var icon = await _service.GetIconAsync("cart");
        Assert.That((await _service.GetSvgAsync("cart")).Name, Is.EqualTo("cart-01"));
        icon.Address = "untrusted:other@variant";
        var svg = await _service.GetSvgAsync(icon);
        Assert.That(svg.Address, Is.EqualTo("hi:cart-01@sr"));

        var result = await _service.SearchAsync(new IconSearchQuery { Term = "basket" });
        Assert.That((await _service.GetSvgAsync(result.Items[0])).Name, Is.EqualTo("cart-01"));
    }

    /// <summary>
    /// An earlier resolved identity uses current source revisions after a catalog reload.
    /// </summary>
    [Test]
    public async Task Resolved_Icon_Uses_Current_Source_After_Reload()
    {
        var icon = await _service.GetIconAsync("cart");
        var original = await _service.GetSvgAsync(icon);
        Write("Icons/hugeicons/rounded/icons/cart-01.svg", _svg.Replace("M0 0L1 1", "M0 0L3 3"));
        SignalChanges();
        var changed = await _service.GetSvgAsync(icon);
        Assert.That(changed.Revision, Is.Not.EqualTo(original.Revision));
        Assert.That(changed.Content, Does.Contain("M0 0L3 3"));
    }

    /// <summary>
    /// Icon identity resolution does not read tags until a consumer requests them.
    /// </summary>
    [Test]
    public async Task Resolved_Tags_Load_On_First_Access()
    {
        var icon = await _service.GetIconAsync("cart");
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/metadata.json"), Times.Never);
        await _service.GetSvgAsync(icon);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/metadata.json"), Times.Never);
        var tags = icon.Tags;
        Assert.That(tags, Is.EqualTo(new[] { "basket" }));
        Assert.That(icon.Tags, Is.SameAs(tags));
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/metadata.json"), Times.Once);
    }

    /// <summary>
    /// Library enumeration reads manifests but leaves all packages and supplementary JSON unopened.
    /// </summary>
    [Test]
    public async Task Library_Discovery_Reads_Only_Manifests()
    {
        WriteLibrary("unused", "u", "r");
        Write("Icons/unused/rounded/icons.zip", "not a ZIP");
        Write("Icons/unused/mapping.json", "not JSON");
        Write("Icons/unused/metadata.json", "not JSON");
        Assert.That(await _service.GetLibrariesAsync(), Has.Count.EqualTo(2));
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo(It.Is<string>(p => p.EndsWith("icons.zip") || p.EndsWith("mapping.json") || p.EndsWith("metadata.json"))), Times.Never);
        files.Verify(x => x.GetDirectoryContents(It.Is<string>(p => p != "Icons")), Times.Never);

        Assert.That(await _service.GetSvgAsync("cart"), Is.Not.Null);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/sharp/icons.zip"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/metadata.json"), Times.Never);
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "basket" })).TotalCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Reading an override neither opens its archive nor reads unrelated overrides or search metadata.
    /// </summary>
    [Test]
    public async Task Override_Lookup_Is_Independent_Of_Archive_And_Other_Overrides()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", _svg);
        Write("Icons/hugeicons/rounded/icons/broken.svg", "not SVG");
        Write("Icons/hugeicons/rounded/icons.zip", "not a ZIP");
        Write("Icons/hugeicons/metadata.json", "not JSON");
        Assert.That(await _service.GetSvgAsync("custom"), Is.Not.Null);
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons.zip"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons/broken.svg"), Times.Never);
        files.Verify(x => x.GetDirectoryContents(It.Is<string>(p => p.EndsWith("icons"))), Times.Never);
    }

    /// <summary>
    /// Search lists custom names without parsing or fingerprinting any override contents.
    /// </summary>
    [Test]
    public async Task Search_Does_Not_Read_Override_Contents()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", "not SVG");
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "custom" })).TotalCount, Is.EqualTo(1));
        Mock.Get(_context.Object.AppDataRoot).Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons/custom.svg"), Times.Never);
    }

    /// <summary>
    /// Scales inherited widths and explicit overrides once while retaining their relative weights.
    /// </summary>
    [Test]
    public async Task Stroke_Width_Scale_Preserves_Inheritance_And_Units()
    {
        Write("Icons/hugeicons/rounded/icons/scaled.svg", """
            <svg viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
                <g><path d="M0 0L1 1"/><path d="M0 0L2 2" stroke-width="3"/></g>
                <g stroke-width="2px"><circle r="1"/><path d="M0 0L1 1" stroke-width="inherit"/></g>
            </svg>
            """);
        var svg = await _service.GetSvgAsync("scaled");
        var tree = XElement.Parse("<svg>" + svg.Content + "</svg>");
        var groups = tree.Elements().ToArray();
        Assert.That(groups[0].Attribute("stroke-width"), Is.Null);
        Assert.That((string)groups[0].Attribute("style"), Does.Contain("stroke-width:var(--icon-stroke-width,2.4)"));
        Assert.That((string)groups[0].Attribute("stroke-linecap"), Is.EqualTo("round"));
        Assert.That(groups[0].Elements().First().Attribute("stroke-width"), Is.Null);
        Assert.That(groups[0].Elements().Last().Attribute("stroke-width"), Is.Null);
        Assert.That((string)groups[0].Elements().Last().Attribute("style"), Does.Contain("stroke-width:var(--icon-stroke-width,4.8)"));
        Assert.That(groups[1].Attribute("stroke-width"), Is.Null);
        Assert.That((string)groups[1].Attribute("style"), Does.Contain("stroke-width:var(--icon-stroke-width,3.2px)"));
        Assert.That((string)groups[1].Elements().Last().Attribute("stroke-width"), Is.EqualTo("inherit"));
        Assert.That(svg.RootAttributes.Keys.Any(x => x.StartsWith("stroke", StringComparison.Ordinal)), Is.False);
    }

    /// <summary>
    /// Scales the SVG initial width when absent and preserves declarations when the factor is omitted.
    /// </summary>
    [Test]
    public async Task Stroke_Width_Scale_Handles_Default_Width_And_Default_Factor()
    {
        Write("Icons/hugeicons/rounded/icons/initial.svg", """<svg><path stroke="black" d="M0 0L1 1"/></svg>""");
        var scaled = await _service.GetSvgAsync("initial");
        Assert.That(scaled.Content, Does.Contain("stroke-width:var(--icon-stroke-width,1.6)"));
        Write("Icons/hugeicons/sharp/icons/unscaled.svg", _svg);
        SignalChanges();
        var original = await _service.GetSvgAsync("unscaled", variant: "sharp");
        Assert.That(original.Content, Does.Contain("stroke-width:var(--icon-stroke-width,1)"));
        Assert.That(_service.GetLibrary("hi").Variants["sharp"].StrokeWidthScale, Is.EqualTo(1));
    }

    /// <summary>
    /// A manifest scale change produces a new cache revision and newly scaled source geometry.
    /// </summary>
    [Test]
    public async Task Stroke_Width_Scale_Change_Invalidates_Cached_Svg()
    {
        var original = await _service.GetSvgAsync("cart");
        string path = Path.Combine(_root, "Icons/hugeicons/library.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"strokeWidthScale\":1.6", "\"strokeWidthScale\":2"));
        SignalChanges();
        var changed = await _service.GetSvgAsync("cart");
        Assert.That(changed.Revision, Is.Not.EqualTo(original.Revision));
        Assert.That(changed.Content, Does.Contain("stroke-width:var(--icon-stroke-width,2)"));
    }

    /// <summary>
    /// Preserves source colors by default and applies configured fallbacks without painting none strokes.
    /// </summary>
    [Test]
    public async Task Stroke_Color_Configuration_And_Css_Fallbacks_Preserve_None()
    {
        Write("Icons/hugeicons/rounded/icons/colors.svg", """
            <svg fill="none"><path stroke="#141B34" stroke-width="1.5"/>
            <path stroke="none"/><path fill="red"/><circle stroke="blue" stroke-width="2"/></svg>
            """);
        var original = await _service.GetSvgAsync("colors");
        Assert.That(original.Content, Does.Contain("stroke:var(--icon-stroke,#141B34)"));
        Assert.That(original.Content, Does.Contain("stroke:var(--icon-stroke,blue)"));
        string path = Path.Combine(_root, "Icons/hugeicons/library.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"strokeWidthScale\":", "\"stroke\":\"currentColor\",\"strokeWidthScale\":"));
        SignalChanges();
        var configured = await _service.GetSvgAsync("colors");
        var tree = XElement.Parse("<svg>" + configured.Content + "</svg>");
        var children = tree.Elements().ToArray();
        Assert.That(configured.Revision, Is.Not.EqualTo(original.Revision));
        Assert.That((string)children[0].Attribute("style"), Does.Contain("stroke:var(--icon-stroke,currentColor)"));
        Assert.That((string)children[1].Attribute("stroke"), Is.EqualTo("none"));
        Assert.That((string)children[1].Attribute("style"), Does.Not.Contain("stroke:var"));
        Assert.That(children[2].Attribute("stroke"), Is.Null);
        Assert.That((string)children[2].Attribute("fill"), Is.EqualTo("red"));
        Assert.That((string)children[3].Attribute("style"), Does.Contain("stroke-width:var(--icon-stroke-width,3.2)"));
    }

    /// <summary>
    /// Rejects source paint that would inject another declaration into generated CSS.
    /// </summary>
    [Test]
    public void Stroke_Fallback_Rejects_Declaration_Injection()
    {
        Write("Icons/hugeicons/rounded/icons/injection.svg", """<svg><path stroke="red;opacity:0"/></svg>""");
        Assert.ThrowsAsync<InvalidDataException>(async () => await _service.GetSvgAsync("injection"));
    }

    /// <summary>
    /// Optional kit configuration can be created, changed and removed between catalog generations.
    /// </summary>
    [Test]
    public async Task Kit_File_Is_Optional_And_Reloaded_After_Changes()
    {
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var renderer = new IconRenderer(_service, kits);
        Assert.That(kits.Kits, Is.Empty);
        var icon = await _service.GetIconAsync("cart");
        Assert.That((await renderer.RenderAsync(icon)).Attributes["viewBox"], Is.EqualTo("-1 -2 24 25"));
        Mock.Get(_context.Object.AppDataRoot).Verify(x => x.Watch("Icons/kits.json"), Times.Once);

        Write("Icons/kits.json", """{"shared":["cart"]}""");
        SignalChanges();
        Assert.That(kits.GetReference(await _service.GetIconAsync("cart")), Is.Not.Null);

        Write("Icons/kits.json", """{"shared":[]}""");
        SignalChanges();
        Assert.That(kits.GetReference(await _service.GetIconAsync("cart")), Is.Null);

        File.Delete(Path.Combine(_root, "Icons", "kits.json"));
        SignalChanges();
        Assert.That(kits.Kits, Is.Empty);
        Assert.That((await renderer.RenderAsync(await _service.GetIconAsync("cart"))).Attributes["viewBox"], Is.EqualTo("-1 -2 24 25"));
    }

    /// <summary>
    /// Resolves shared membership without parsing artwork or writing individual cache entries.
    /// </summary>
    [Test]
    public async Task Kit_Rendering_Uses_Shared_Reference_And_PathBase()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"backend":["cart"],"shared":["cart"]}""");
        var http = new DefaultHttpContext();
        http.Request.PathBase = "/store";
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor { HttpContext = http });
        var icon = await _service.GetIconAsync("cart");
        var reference = kits.GetReference(icon);
        Assert.That(reference.Href, Does.StartWith("/store/icons/shared-").And.EndWith("#cart"));

        var renderer = new IconRenderer(_service, kits);
        var options = new IconOptions();
        options.Attributes["class"] = "icon-3x";
        options.Attributes["aria-label"] = "Cart";
        var rendered = await renderer.RenderAsync(icon, options);
        Assert.That(rendered.Attributes["class"], Does.Contain("icon-hi-sr").And.Contain("icon-3x"));
        Assert.That(rendered.Attributes["role"], Is.EqualTo("img"));
        using var writer = new StringWriter();
        rendered.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        Assert.That(writer.ToString(), Does.Contain("<use").And.Not.Contain("<path"));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Caches whole sprites, isolates source IDs and invalidates URLs after an override changes.
    /// </summary>
    [Test]
    public async Task Kit_Sprites_Are_Cached_And_Revisioned_Without_Individual_Cache_Entries()
    {
        const string drawing = """<svg viewBox="0 0 16 16" fill="none"><defs><linearGradient id="paint"><stop stop-color="red"/></linearGradient></defs><path id="shape" fill="url(#paint)" d="M0 0L1 1"/></svg>""";
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart","copy","alias"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","copy":"direct","alias":"cart-01"}""");
        Write("Icons/hugeicons/rounded/user/cart-01.svg", drawing);
        Write("Icons/hugeicons/rounded/user/direct.svg", drawing);
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var url = kits.GetUrl("shared");
        var revision = url[^28..^4];
        Assert.That(revision, Has.Length.EqualTo(24));
        var path = await kits.GetSpriteFileAsync("shared", revision);
        Assert.That(Path.GetFileName(path), Is.EqualTo($"shared-{revision}.svg"));
        Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.Combine(_root, ".cache", "IconKits")));
        var sprite = File.ReadAllText(path);
        var tree = XElement.Parse(sprite);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.That(tree.Elements(ns + "symbol").Count(x => !((string)x.Attribute("id")).StartsWith("source:")), Is.EqualTo(3));
        Assert.That(tree.Descendants(ns + "path").Count(), Is.EqualTo(2));
        var ids = tree.Descendants().Attributes("id").Select(x => x.Value).ToArray();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
        Assert.That(sprite, Does.Not.Contain("url(#paint)"));
        Assert.That(await kits.GetSpriteFileAsync("shared", revision), Is.EqualTo(path));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)), Has.Length.EqualTo(1));
        _cache.VerifyNoOtherCalls();

        Write("Icons/hugeicons/rounded/user/cart-01.svg", drawing.Replace("red", "blue"));
        SignalChanges();
        Assert.That(kits.GetUrl("shared"), Is.Not.EqualTo(url));
        Assert.That(await kits.GetSpriteFileAsync("shared", revision), Is.EqualTo(path));
        Assert.That(File.ReadAllText(path), Is.EqualTo(sprite));
        var newRevision = kits.GetUrl("shared")[^28..^4];
        var newPath = await kits.GetSpriteFileAsync("shared", newRevision);
        Assert.That(File.ReadAllText(newPath), Does.Contain("blue"));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.svg"), Has.Length.EqualTo(2));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp"), Is.Empty);
        Assert.That(await kits.GetSpriteFileAsync("shared", "../invalid"), Is.Null);
        Assert.That(await kits.GetSpriteFileAsync("shared", new string('0', 24)), Is.Null);
    }

    /// <summary>
    /// Changes membership and mappings without leaving a stale reverse lookup behind.
    /// </summary>
    [Test]
    public async Task Kit_Index_Reloads_After_Configuration_Changes()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var cart = await _service.GetIconAsync("cart");
        Assert.That(kits.GetReference(cart), Is.Not.Null);
        Write("Icons/hugeicons/mapping.json", """{"cart":"direct"}""");
        SignalChanges();
        Assert.That(kits.GetReference(cart), Is.Null);
        Assert.That(kits.GetReference(await _service.GetIconAsync("cart")), Is.Not.Null);
        Assert.That(kits.GetUrl("unknown"), Is.Null);
        Assert.That(kits.GetUrl("shared", "unknown"), Is.Null);
    }

    /// <summary>
    /// Coalesces concurrent file generation and removes incomplete output after preparation fails.
    /// </summary>
    [Test]
    public async Task Kit_Files_Are_Published_Atomically_And_Failures_Are_Cleaned_Up()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var revision = kits.GetUrl("shared")[^28..^4];
        var paths = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => kits.GetSpriteFileAsync("shared", revision))));
        Assert.That(paths.Distinct().Count(), Is.EqualTo(1));
        Assert.That(Directory.GetFiles(Path.Combine(_root, ".cache", "IconKits")), Has.Length.EqualTo(1));

        Write("Icons/hugeicons/rounded/user/cart-01.svg", "<svg><script/></svg>");
        SignalChanges();
        var invalidRevision = kits.GetUrl("shared")[^28..^4];
        Assert.ThrowsAsync<InvalidDataException>(() => kits.GetSpriteFileAsync("shared", invalidRevision));
        Assert.That(Directory.GetFiles(Path.Combine(_root, ".cache", "IconKits")), Has.Length.EqualTo(1));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Reads ZIP metadata once for discovery and opens one archive for the whole sprite batch.
    /// Verifies that generation releases the archive rather than retaining a singleton handle.
    /// </summary>
    [Test]
    public async Task Kit_Generation_Reuses_One_Archive_And_Releases_It()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart","copy"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","copy":"direct"}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var revision = kits.GetUrl("shared")[^28..^4];
        const string archivePath = "Icons/hugeicons/rounded/icons.zip";
        Assert.That(_sourceLookups[archivePath], Is.EqualTo(1));
        var path = await kits.GetSpriteFileAsync("shared", revision);
        Assert.That(_sourceLookups[archivePath], Is.EqualTo(2));
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.That(XElement.Load(path).Elements(ns + "symbol").Count(x => !((string)x.Attribute("id")).StartsWith("source:")), Is.EqualTo(2));
        using var exclusive = File.Open(Path.Combine(_root, archivePath), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.That(exclusive.CanRead, Is.True);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Resolves opaque URLs after a restart, both for existing files and ungenerated current revisions.
    /// Distinct variants retain distinct revisions without exposing their selectors in the URL.
    /// </summary>
    [Test]
    public async Task Opaque_Kit_Urls_Survive_Restarts_And_Distinguish_Variants()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var roundedUrl = kits.GetUrl("shared", "hi", "sr");
        var sharpUrl = kits.GetUrl("shared", "hi", "sharp");
        Assert.That(roundedUrl, Does.StartWith("/icons/shared-").And.EndWith(".svg"));
        Assert.That(roundedUrl, Does.Not.Contain("hugeicons").And.Not.Contain("rounded"));
        Assert.That(sharpUrl, Is.Not.EqualTo(roundedUrl));
        var roundedRevision = roundedUrl[^28..^4];
        var roundedPath = await kits.GetSpriteFileAsync("shared", roundedRevision);

        // The endpoint is the first consumer after restart: no preceding GetUrl call.
        var restarted = new IconKitService(new IconService(_context.Object, _cache.Object), _context.Object, new HttpContextAccessor());
        Assert.That(await restarted.GetSpriteFileAsync("shared", roundedRevision), Is.EqualTo(roundedPath));
        var sharpPath = await restarted.GetSpriteFileAsync("shared", sharpUrl[^28..^4]);
        Assert.That(File.Exists(sharpPath), Is.True);
        Assert.That(sharpPath, Is.Not.EqualTo(roundedPath));
        Assert.That(await restarted.GetSpriteFileAsync("../shared", roundedRevision), Is.Null);
        Assert.That(await restarted.GetSpriteFileAsync("unknown", roundedRevision), Is.Null);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Applies mapping transforms around an offset viewBox without changing the shared source payload.
    /// Explicit direct addresses skip both name mapping and its transformation.
    /// </summary>
    [Test]
    public async Task Mapping_Modifiers_Render_Inline_And_Keep_Source_Cache_Shared()
    {
        Write("Icons/hugeicons/mapping.json", """{"left":"cart-01?flip=x&rotate=-90","up":"cart-01?rotate=-90&flip=x","cart-01":"direct?flip=y"}""");
        var left = await _service.GetIconAsync("left");
        var up = await _service.GetIconAsync("up");
        var direct = await _service.GetIconAsync("cart-01!");
        Assert.That(left.Name, Is.EqualTo("cart-01"));
        Assert.That(left.Transform, Is.EqualTo(up.Transform));
        Assert.That(left.Transform.Rotation, Is.EqualTo(270));
        Assert.That(direct.Transform.IsIdentity, Is.True);
        Assert.That(direct.Name, Is.EqualTo("cart-01"));
        var renderer = new IconRenderer(_service, Mock.Of<IIconKitService>());
        var output = await renderer.RenderAsync(left);
        using var writer = new StringWriter();
        output.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        var xml = XElement.Parse(writer.ToString());
        var transform = xml.Descendants().Attributes("transform").Single().Value;
        Assert.That(transform, Is.EqualTo("translate(11 10.5) rotate(270) scale(-1 1) translate(-11 -10.5)"));
        Assert.That(xml.Elements().Single().Attribute("transform"), Is.Null);
        var source = await _service.GetSvgAsync(direct);
        Assert.That(source.Content, Does.Not.Contain("rotate(").And.Not.Contain("scale("));
        Assert.That(_entries.Count, Is.EqualTo(1));
        var search = await _service.SearchAsync(new IconSearchQuery { Term = "left" });
        Assert.That(search.Items.Single().Name, Is.EqualTo("cart-01"));
    }

    /// <summary>
    /// Produces independent transformed symbols with one drawing and prevents double transforms at render time.
    /// </summary>
    [Test]
    public async Task Mapping_Modifiers_Are_Part_Of_Kit_Identity_And_Revision()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["left","up"]}""");
        Write("Icons/hugeicons/mapping.json", """{"left":"cart-01?flip=x","up":"cart-01?rotate=-90"}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var left = await _service.GetIconAsync("left");
        var up = await _service.GetIconAsync("up");
        Assert.That(kits.GetReference(left).Href, Does.EndWith("#left"));
        Assert.That(kits.GetReference(up).Href, Does.EndWith("#up"));
        Assert.That(kits.GetReference(await _service.GetIconAsync("cart-01!")), Is.Null);
        var url = kits.GetUrl("shared");
        var path = await kits.GetSpriteFileAsync("shared", url[^28..^4]);
        var xml = XElement.Load(path);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.That(xml.Descendants(ns + "path").Count(), Is.EqualTo(1));
        Assert.That(xml.Element(ns + "defs").Element(ns + "g").Attribute("viewBox"), Is.Null);
        var leftSymbol = xml.Elements(ns + "symbol").Single(x => (string)x.Attribute("id") == "left");
        var upSymbol = xml.Elements(ns + "symbol").Single(x => (string)x.Attribute("id") == "up");
        Assert.That(leftSymbol.Element(ns + "g").Attribute("transform").Value, Does.Contain("scale(-1 1)"));
        Assert.That(upSymbol.Element(ns + "g").Attribute("transform").Value, Does.Contain("rotate(270)"));
        var renderer = new IconRenderer(_service, kits);
        var output = await renderer.RenderAsync(left);
        using var writer = new StringWriter();
        output.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        Assert.That(writer.ToString(), Does.Contain("#left").And.Not.Contain("transform="));
        _cache.VerifyNoOtherCalls();

        Write("Icons/hugeicons/mapping.json", """{"left":"cart-01?flip=y","up":"cart-01?rotate=-90"}""");
        SignalChanges();
        Assert.That(kits.GetUrl("shared"), Is.Not.EqualTo(url));
    }

    private static string RenderMarkup(Microsoft.AspNetCore.Html.IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    /// <summary>
    /// Merges individual overrides and falls back to the source without changing the kit or cache identity.
    /// </summary>
    [Test]
    public async Task Address_And_Renderer_Modifiers_Override_Mapping_Without_Changing_Kits()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons"}""");
        Write("Icons/kits.json", """{"shared":["left"]}""");
        Write("Icons/hugeicons/mapping.json", """{"left":"cart-01?flip=xy&rotate=90&stroke-scale=1.2"}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var renderer = new IconRenderer(_service, kits);
        var url = kits.GetUrl("shared");
        var icon = await _service.GetIconAsync("hi:left@sr?rotate=180&stroke-scale=1.1");
        Assert.That(icon.Transform, Is.EqualTo(new IconTransform(true, true, 180)));
        Assert.That(icon.StrokeScale, Is.EqualTo(1.1));
        var output = await renderer.RenderAsync(icon, new IconOptions { Rotate = 0, FlipHorizontal = false, StrokeScale = 1 });
        var drawing = RenderMarkup(output.InnerHtml);
        Assert.That(drawing, Does.Contain("rotate(0)").And.Contain("scale(1 -1)").And.Not.Contain("<use"));
        Assert.That(icon.Transform.Rotation, Is.EqualTo(180), "Rendering must not mutate the resolved identity.");
        Assert.That(kits.GetUrl("shared"), Is.EqualTo(url));
        var direct = await _service.GetIconAsync("hi:cart-01!@sr?flip=none&rotate=0");
        await renderer.RenderAsync(direct);
        Assert.That(_entries.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Scales individual stroke fallbacks without reparsing XML or changing the prepared source.
    /// </summary>
    [Test]
    public async Task Stroke_Modifier_Preserves_Different_Source_Widths()
    {
        Write("Icons/hugeicons/rounded/user/cart-01.svg",
            """<svg viewBox="0 0 24 24"><path stroke="currentColor" stroke-width="1.5" d="M0 0L1 1"/><path stroke="currentColor" stroke-width="2" d="M2 2L3 3"/></svg>""");
        var icon = await _service.GetIconAsync("cart?stroke-scale=1.1");
        var source = await _service.GetSvgAsync(icon);
        var original = source.Content;
        var renderer = new IconRenderer(_service, Mock.Of<IIconKitService>());
        var output = await renderer.RenderAsync(icon);
        Assert.That(RenderMarkup(output.InnerHtml), Does.Contain("calc(var(--icon-stroke-width,2.4) * var(--icon-stroke-scale,1))")
            .And.Contain("calc(var(--icon-stroke-width,3.2) * var(--icon-stroke-scale,1))"));
        Assert.That(output.Attributes["style"], Does.Contain("--icon-stroke-scale:1.1"));
        Assert.That(source.Content, Is.EqualTo(original));
        Assert.That(_entries.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Presentation-only options preserve external kit rendering and caller style precedence.
    /// </summary>
    [Test]
    public async Task Presentation_Options_Keep_Kit_Rendering()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons"}""");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var renderer = new IconRenderer(_service, kits);
        var options = new IconOptions { Size = "3x", Animation = "beat-fade", FontScale = 2, ShiftX = 2, AnimationReverse = false };
        options.Attributes["style"] = "--icon-size-factor:4";
        var output = await renderer.RenderAsync(await _service.GetIconAsync("cart"), options);
        Assert.That(RenderMarkup(output.InnerHtml), Does.Contain("<use"));
        Assert.That(output.Attributes["class"], Does.Contain("icon-3x").And.Contain("icon-beat-fade"));
        Assert.That(output.Attributes["style"], Does.Contain("--icon-shift-x:0.125em").And.EndWith("--icon-size-factor:4"));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Rejects unknown, duplicate and malformed modifiers with actionable mapping diagnostics.
    /// </summary>
    /// <param name="target">The invalid mapping expression.</param>
    [TestCase("cart-01?stroke-scale=0")]
    [TestCase("cart-01?stroke-scale=-1")]
    [TestCase("cart-01?stroke-scale=NaN")]
    [TestCase("cart-01?stroke-scale=1&stroke-scale=2")]
    [TestCase("cart-01?flip=z")]
    [TestCase("cart-01?rotate=NaN")]
    [TestCase("cart-01?rotate=Infinity")]
    [TestCase("cart-01?rotate=1,5")]
    [TestCase("cart-01?rotate=")]
    [TestCase("cart-01?flip=x&flip=y")]
    [TestCase("cart-01?rotate=90&rotate=180")]
    [TestCase("cart-01?size=2")]
    [TestCase("cart-01?")]
    [TestCase("cart-01?flip=x&")]
    public void Invalid_Mapping_Modifiers_Report_The_Concept(string target)
    {
        Write("Icons/hugeicons/mapping.json", JsonSerializer.Serialize(new Dictionary<string, string> { ["left"] = target }));
        var error = Assert.ThrowsAsync<InvalidDataException>(() => _service.GetIconAsync("left"));
        Assert.That(error.Message, Does.Contain("mapping.json (left)"));
    }

    /// <summary>
    /// Normalizes valid flip axes and decimal angles independently of the process culture.
    /// </summary>
    /// <param name="modifiers">The valid mapping query.</param>
    /// <param name="flipX">The expected horizontal reflection.</param>
    /// <param name="flipY">The expected vertical reflection.</param>
    /// <param name="rotation">The expected normalized angle.</param>
    [TestCase("flip=y&rotate=22.5", false, true, 22.5)]
    [TestCase("flip=xy&rotate=450", true, true, 90)]
    [TestCase("rotate=-360", false, false, 0)]
    public async Task Mapping_Modifiers_Normalize_Valid_Values(string modifiers, bool flipX, bool flipY, double rotation)
    {
        Write("Icons/hugeicons/mapping.json", JsonSerializer.Serialize(new Dictionary<string, string> { ["left"] = "cart-01?" + modifiers }));
        var icon = await _service.GetIconAsync("left");
        Assert.That(icon.Transform.FlipX, Is.EqualTo(flipX));
        Assert.That(icon.Transform.FlipY, Is.EqualTo(flipY));
        Assert.That(icon.Transform.Rotation, Is.EqualTo(rotation));
    }

    /// <summary>
    /// Uses real file notifications rather than the fixture's manual change token.
    /// Mapping writes invalidate the catalog while generated cache files do not.
    /// </summary>
    [Test]
    public async Task Physical_Watcher_Reloads_Mappings_And_Ignores_Generated_Files()
    {
        Mock.Get(_context.Object.AppDataRoot).Setup(x => x.Watch(It.IsAny<string>()))
            .Returns((string filter) => _provider.Watch(filter));
        var first = await _service.GetIconAsync("cart");
        var manifest = _service.DefaultLibrary;
        Assert.That(first.Name, Is.EqualTo("cart-01"));
        Write(".cache/IconKits/shared-test.svg", "<svg/>");
        await Task.Delay(300);
        Assert.That(_service.DefaultLibrary, Is.SameAs(manifest));

        Write("Icons/hugeicons/mapping.json", """{"cart":"direct?flip=x"}""");
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        IconInfo updated;
        do
        {
            await Task.Delay(50);
            updated = await _service.GetIconAsync("cart");
        }
        while (updated.Name != "direct" && timeout.Elapsed < TimeSpan.FromSeconds(10));

        Assert.That(updated.Name, Is.EqualTo("direct"));
        Assert.That(updated.Transform.FlipX, Is.True);
        Assert.That(_service.DefaultLibrary, Is.Not.SameAs(manifest));
    }

    /// <summary>
    /// Applies kit defaults before mapping while explicit selectors and direct addresses retain precedence.
    /// </summary>
    [Test]
    public async Task Kit_Defaults_Select_Library_And_Scope_Variant_Overrides()
    {
        WriteLibrary("other", "ot", "rd");
        WriteZip("other", "rounded", ("cart-01", _svg));
        WriteZip("other", "sharp", ("cart-01", _svg));
        Write("Icons/other/mapping.json", """{"cart":"cart-01"}""");
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"backend":{"defaultLibrary":"other","defaultVariant":"sharp","icons":["cart","cart-01"]}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var icon = await _service.GetIconAsync("cart");
        Assert.That(icon.LibraryName, Is.EqualTo("other"));
        Assert.That(icon.VariantName, Is.EqualTo("sharp"));
        Assert.That(icon.Name, Is.EqualTo("cart-01"));
        Assert.That(kits.GetReference(icon).Href, Does.StartWith(kits.GetUrl("backend") + "#"));
        Assert.That(kits.Kits.Single().DefaultLibrary, Is.EqualTo("other"));

        var explicitLibrary = await _service.GetIconAsync("hi:cart");
        Assert.That(explicitLibrary.LibraryName, Is.EqualTo("hugeicons"));
        Assert.That(explicitLibrary.VariantName, Is.EqualTo("rounded"));
        Assert.That((await _service.GetIconAsync("cart", "hi")).VariantName, Is.EqualTo("rounded"));
        Assert.That((await _service.GetIconAsync("cart@rd")).VariantName, Is.EqualTo("rounded"));
        Assert.That((await _service.GetIconAsync("cart", variant: "rd")).VariantName, Is.EqualTo("rounded"));
        Assert.That(kits.GetUrl("backend", "hi"), Is.EqualTo(kits.GetUrl("backend", "hi", "rounded")));
        Assert.That(kits.GetUrl("backend"), Is.Not.EqualTo(kits.GetUrl("backend", "hi")));

        var direct = await _service.GetIconAsync("cart-01!");
        Assert.That(direct.LibraryName, Is.EqualTo("hugeicons"));
        Assert.That(direct.VariantName, Is.EqualTo("rounded"));
        Assert.That(direct.Transform.IsIdentity, Is.True);
    }

    /// <summary>
    /// Uses a library's own default when a kit changes libraries without specifying a variant.
    /// </summary>
    [Test]
    public async Task Kit_Default_Library_Does_Not_Inherit_Global_Variant()
    {
        WriteLibrary("other", "ot", "rd");
        WriteZip("other", "sharp", ("cart-01", _svg));
        Write("Icons/other/mapping.json", """{"cart":"cart-01"}""");
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"backend":{"defaultLibrary":"other","icons":["cart"]}}""");
        Assert.That((await _service.GetIconAsync("cart")).VariantName, Is.EqualTo("sharp"));
    }

    /// <summary>
    /// Resolves overlapping concepts deterministically and reloads changed kit defaults.
    /// </summary>
    [Test]
    public async Task Kit_Default_Precedence_And_Invalidation_Are_Deterministic()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"backend":{"defaultVariant":"sharp","icons":["cart"]},"shared":{"icons":["cart"]}}""");
        Assert.That((await _service.GetIconAsync("cart")).VariantName, Is.EqualTo("rounded"));
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"zulu":{"icons":["cart"]},"alpha":{"defaultVariant":"sharp","icons":["cart"]}}""");
        SignalChanges();
        Assert.That((await _service.GetIconAsync("cart")).VariantName, Is.EqualTo("sharp"));
        Assert.That((await _service.GetIconAsync("cart", variant: "rounded")).VariantName, Is.EqualTo("rounded"));
    }

    /// <summary>
    /// Rejects invalid kit defaults when reading configuration rather than failing during rendering.
    /// </summary>
    /// <param name="defaults">The invalid kit default properties.</param>
    [TestCase("\"defaultLibrary\":\"missing\"")]
    [TestCase("\"defaultLibrary\":\"hi\"")]
    [TestCase("\"defaultVariant\":\"missing\"")]
    public void Kit_Defaults_Must_Reference_Registered_System_Identities(string defaults)
    {
        Write("Icons/kits.json", "{\"backend\":{" + defaults + ",\"icons\":[\"cart\"]}}");
        var error = Assert.Throws<InvalidDataException>(() => _ = _service.DefaultLibrary);
        Assert.That(error.Message, Does.Contain("backend"));
    }

    private void WriteLibrary(string name, string shortName, string variantShortName)
    {
        Write($"Icons/{name}/library.json", JsonSerializer.Serialize(new
        {
            displayName = name,
            shortName,
            defaultVariant = "sharp",
            variants = new
            {
                rounded = new { defaultViewBox = "0 0 24 24", shortName = variantShortName, strokeWidthScale = 1.6 },
                sharp = new { defaultViewBox = "0 0 24 24" }
            }
        }));
    }

    private void Write(string path, string content)
    {
        string fullPath = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, content, new UTF8Encoding(false));
    }

    private void WriteZip(string library, string variant, params (string Name, string Svg)[] icons)
    {
        string path = Path.Combine(_root, "Icons", library, variant, "icons.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var icon in icons)
        {
            using var writer = new StreamWriter(archive.CreateEntry(icon.Name + ".svg").Open());
            writer.Write(icon.Svg);
        }
    }

    private void SignalChanges()
    {
        _changes.Cancel();
        _changes.Dispose();
        _changes = new CancellationTokenSource();
    }
}
