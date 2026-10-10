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
    /// Applies direction policy to concepts only, for inline and kit rendering alike.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Rtl_Policy_Follows_Concept_Without_Changing_Artwork(bool useKit)
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","mirrorInRtl":["cart"]}""");
        if (useKit)
        {
            Write("Icons/kits.json", """{"shared":["cart"]}""");
        }

        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var renderer = new IconRenderer(_service, kits);
        var concept = await _service.GetIconAsync("cart");
        Assert.That(concept.MirrorInRtl, Is.True);
        var svg = await renderer.RenderAsync(concept);
        Assert.That(svg.Attributes["class"], Does.Contain("icon-mirror-rtl"));
        Assert.That((await _service.GetIconAsync("cart?rotate=90")).MirrorInRtl, Is.True);
        Assert.That((await _service.GetIconAsync("hi:cart")).MirrorInRtl, Is.False);
        Assert.That((await _service.GetIconAsync("cart@sr")).MirrorInRtl, Is.False);
        Assert.That((await _service.GetIconAsync("cart-01!")).MirrorInRtl, Is.False);
        Assert.That((await _service.GetIconAsync("cart-01")).MirrorInRtl, Is.False);
    }

    /// <summary>
    /// Versions the direction policy independently of immutable sprite artwork.
    /// </summary>
    [Test]
    public async Task Rtl_Policy_Changes_Manifest_But_Not_Kit_Reference()
    {
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var originalManifest = kits.GetManifestUrl();
        var originalHref = kits.GetReference(await _service.GetIconAsync("cart")).Href;
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","mirrorInRtl":["cart"]}""");
        SignalChanges();

        Assert.That(kits.GetManifestUrl(), Is.Not.EqualTo(originalManifest));
        Assert.That(kits.GetReference(await _service.GetIconAsync("cart")).Href, Is.EqualTo(originalHref));
        var path = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.That(json.RootElement.GetProperty("mirrorInRtl").EnumerateArray().Select(x => x.GetString()),
            Is.EqualTo(new[] { "cart" }));
    }

    /// <summary>
    /// Bakes mapping widths into independent drawings, preserving kit hits and inline overrides.
    /// </summary>
    [Test]
    public async Task Kit_Bakes_Stroke_Scale_And_Versions_Changes()
    {
        Write("Icons/kits.json", """{"shared":["cart","thick","thick-alias"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","thick":"cart-01?stroke-scale=1.25","thick-alias":"cart-01?stroke-scale=1.25"}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var renderer = new IconRenderer(_service, kits);
        var icon = await _service.GetIconAsync("thick");
        var reference = kits.GetReference(icon);
        var output = await renderer.RenderAsync(icon);
        Assert.That(RenderMarkup(output.InnerHtml), Does.Contain("<use"));
        Assert.That(await renderer.RenderAsync(icon, new IconOptions { StrokeScale = 1.25 }), Is.Not.Null);
        var reset = await renderer.RenderAsync(icon, new IconOptions { StrokeScale = 1 });
        Assert.That(RenderMarkup(reset.InnerHtml), Does.Not.Contain("<use"));
        Assert.That(RenderMarkup((await renderer.RenderAsync(await _service.GetIconAsync("thick?stroke-scale=1"))).InnerHtml),
            Does.Not.Contain("<use"));

        var path = await kits.GetSpriteFileAsync("shared", Path.GetFileNameWithoutExtension(reference.Href.Split('#')[0]).Substring("shared-".Length));
        var text = await File.ReadAllTextAsync(path);
        Assert.That(text, Does.Contain("calc(var(--icon-stroke-width,1.6) * 1.25)"));
        XNamespace ns = "http://www.w3.org/2000/svg";
        var sprite = XDocument.Parse(text);
        Assert.That(sprite.Descendants(ns + "symbol").Single(x => (string)x.Attribute("id") == "cart").ToString(),
            Does.Not.Contain(" * 1.25"));
        Assert.That(sprite.Descendants(ns + "defs").Count(), Is.EqualTo(1), "Equal stroke scales share artwork.");

        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01","thick":"cart-01?stroke-scale=1.5","thick-alias":"cart-01?stroke-scale=1.25"}""");
        SignalChanges();
        Assert.That(kits.GetReference(await _service.GetIconAsync("thick")).Href, Is.Not.EqualTo(reference.Href));
    }

    /// <summary>
    /// Exports compact resolution data once, preserves PathBase and leaves individual SVG caching untouched.
    /// </summary>
    [Test]
    public async Task Browser_Manifest_Is_Compact_Immutable_And_PathBase_Independent()
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded"}""");
        Write("Icons/kits.json", """{"shared":["cart","thick"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01?flip=x","thick":"cart-01?stroke-scale=1.1","same":"same"}""");
        var http = new DefaultHttpContext();
        http.Request.PathBase = "/shop";
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor { HttpContext = http });
        var url = kits.GetManifestUrl();
        Assert.That(url, Does.StartWith("/shop/icons/manifest/"));
        var revision = Path.GetFileNameWithoutExtension(url);
        var paths = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => kits.GetManifestFileAsync(revision)));
        Assert.That(paths.Distinct().Count(), Is.EqualTo(1));
        Assert.That(Path.GetDirectoryName(paths[0]), Is.EqualTo(Path.Combine(_root, ".cache", "icons", "kits")));
        Assert.That(Path.GetFileName(paths[0]), Is.EqualTo("manifest-" + revision + ".json"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(paths[0]));
        Assert.That(json.RootElement.EnumerateObject().Select(x => x.Name), Is.EquivalentTo(new[] { "schemaVersion", "mirrorInRtl", "kits" }));
        Assert.That(json.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(7));
        var shared = json.RootElement.GetProperty("kits").GetProperty("shared");
        Assert.That(shared.EnumerateObject().Select(x => x.Name), Is.EquivalentTo(new[] { "url", "defaultLibrary", "defaultVariant", "icons", "sources" }));
        Assert.That(shared.GetProperty("icons").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "cart", "thick" }),
            "Mapping flips and stroke multipliers are baked into kit symbols.");
        Assert.That(shared.GetProperty("url").GetString(), Does.StartWith("icons/shared-"));
        Assert.That(shared.GetProperty("defaultLibrary").GetString(), Is.EqualTo("hi"));
        Assert.That(shared.GetProperty("defaultVariant").GetString(), Is.EqualTo("sr"));
        Assert.That(shared.GetProperty("sources").GetProperty("cart").GetString(), Is.EqualTo("cart-01"),
            "Only the differing source name is needed; transforms are already baked into the symbol.");
        Assert.That(shared.GetProperty("sources").GetProperty("thick").GetString(), Is.EqualTo("cart-01"));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(paths[0]), "*.svg"), Is.Empty,
            "Manifest creation must not generate sprites.");
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
    /// Publishes each concept once using server kit priority, independently of dictionary order.
    /// </summary>
    /// <param name="configuration">The configured kits and overlapping concepts.</param>
    /// <param name="preferred">The kit selected for the shared concept.</param>
    [TestCase("{\"backend\":[\"cart\"],\"shared\":[\"cart\"]}", "shared")]
    [TestCase("{\"frontend\":[\"cart\"],\"backend\":[\"cart\"]}", "backend")]
    public async Task Browser_Manifest_Uses_Preferred_Concept_Kit(string configuration, string preferred)
    {
        Write("Icons/kits.json", configuration);
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var path = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var published = json.RootElement.GetProperty("kits");
        Assert.That(published.EnumerateObject().Select(x => x.Name), Is.EqualTo(new[] { preferred }));
        Assert.That(published.GetProperty(preferred).GetProperty("icons")[0].GetString(), Is.EqualTo("cart"));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// An incomplete preferred kit must not advertise a lower-priority source for the same concept.
    /// </summary>
    [Test]
    public async Task Browser_Manifest_Omits_Concepts_From_Unavailable_Preferred_Kits()
    {
        Write("Icons/kits.json", """{"shared":["cart","missing"],"backend":{"icons":["cart"],"sources":{"cart":"hi:direct@sr"}}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var path = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.That(json.RootElement.GetProperty("kits").EnumerateObject(), Is.Empty);
        Assert.That((await _service.GetIconAsync("cart")).Name, Is.EqualTo("cart-01"));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Manifest creation does not load mappings or archives of libraries unused by kits.
    /// </summary>
    [Test]
    public async Task Browser_Manifest_Leaves_Unused_Libraries_Deferred()
    {
        WriteLibrary("unused", "u", "r");
        Write("Icons/unused/mapping.json", "not JSON");
        Write("Icons/unused/rounded/icons.zip", "not a ZIP");
        Write("Icons/hugeicons/metadata.json", "not JSON");
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        Assert.That(kits.GetManifestUrl(), Does.StartWith("/icons/manifest/"));
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo("Icons/unused/mapping.json"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/unused/rounded/icons.zip"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/metadata.json"), Times.Never);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// DOM identities use system names when short names are absent and omit unchanged sources.
    /// </summary>
    [Test]
    public async Task Browser_Manifest_Identity_Patches_Preserve_Keys_Without_Short_Names()
    {
        Write("Icons/Original/library.json", """{"defaultVariant":"Outline","variants":{"Outline":{"defaultViewBox":"0 0 16 16"}}}""");
        Write("Icons/Original/Outline/icons/plain.svg", _svg);
        Write("Icons/other/Outline/icons/plain.svg", _svg);
        Write("Icons/other/library.json", """{"defaultVariant":"Outline","variants":{"Outline":{"defaultViewBox":"0 0 16 16"}}}""");
        Write("Icons/kits.json", """{"shared":{"defaultLibrary":"Original","icons":["plain","renamed","foreign"],"sources":{"renamed":"plain","foreign":"other:plain@Outline"}}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var path = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var shared = json.RootElement.GetProperty("kits").GetProperty("shared");
        Assert.That(shared.GetProperty("defaultLibrary").GetString(), Is.EqualTo("Original"));
        Assert.That(shared.GetProperty("defaultVariant").GetString(), Is.EqualTo("Outline"));
        var sources = shared.GetProperty("sources");
        Assert.That(sources.TryGetProperty("plain", out _), Is.False);
        Assert.That(sources.GetProperty("renamed").GetString(), Is.EqualTo("plain"));
        Assert.That(sources.GetProperty("foreign").GetString(), Is.EqualTo("other:plain"),
            "Unspecified patch qualifiers inherit kit metadata even across libraries.");
        var icon = await _service.GetIconAsync("renamed");
        Assert.That(icon.Address, Is.EqualTo("original:plain@outline"));
        Assert.That(icon.LibraryKey, Is.EqualTo("Original"));
        Assert.That(icon.VariantKey, Is.EqualTo("Outline"));
        _cache.VerifyNoOtherCalls();
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
    /// Removes source root classes while retaining classes inside the drawing.
    /// </summary>
    [Test]
    public async Task Svg_Drops_Source_Root_Classes_Only()
    {
        Write("Icons/hugeicons/rounded/icons/cart-01.svg",
            """<svg viewBox="0 0 24 24" class="vendor vendor-cart"><path class="drawing" d="M0 0L1 1"/></svg>""");
        var svg = await _service.GetSvgAsync("cart");
        Assert.That(svg.RootAttributes.ContainsKey("class"), Is.False);
        Assert.That(XElement.Parse("<svg>" + svg.Content + "</svg>").Element("path").Attribute("class").Value, Is.EqualTo("drawing"));
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
    /// Preserves JSON variant order through catalog normalization and serialization for picker display.
    /// </summary>
    [Test]
    public void Library_Variants_Retain_Definition_Order()
    {
        Write("Icons/other/library.json", """
            {"defaultVariant":"solid","variants":{
              "solid":{"displayName":"Solid"},
              "regular":{"displayName":"Regular"},
              "brands":{"displayName":"Brands"}}}
            """);
        var library = _service.GetLibrary("other");
        var expected = new[] { "solid", "regular", "brands" };
        Assert.That(library.Variants.Keys, Is.EqualTo(expected));
        Assert.That(library.Variants.Values.Select(x => x.Name), Is.EqualTo(expected));
        var restored = JsonSerializer.Deserialize<IconLibrary>(JsonSerializer.Serialize(library));
        Assert.That(restored.Variants.Keys, Is.EqualTo(expected));
        Assert.That(restored.Variants["REGULAR"].DisplayName, Is.EqualTo("Regular"));
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
    /// Clones options with an independent case-insensitive attribute dictionary through both contracts.
    /// </summary>
    /// <param name="untyped">Whether to clone through the non-generic interface.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void Options_Clone_Detaches_Attributes(bool untyped)
    {
        var original = new IconOptions { Size = "2x", Rotate = 0, FlipHorizontal = false, StrokeScale = 1.1 };
        original.Attributes["class"] = "original";
        var clone = untyped ? (IconOptions)((ICloneable)original).Clone() : original.Clone();
        Assert.That(clone, Is.Not.SameAs(original));
        Assert.That(clone.Attributes, Is.Not.SameAs(original.Attributes));
        Assert.That(clone.Size, Is.EqualTo("2x"));
        Assert.That(clone.Rotate, Is.Zero);
        Assert.That(clone.FlipHorizontal, Is.False);
        Assert.That(clone.StrokeScale, Is.EqualTo(1.1));

        clone.Size = "3x";
        clone.Attributes["CLASS"] = "clone";
        Assert.That(clone.Attributes.Count, Is.EqualTo(1));
        Assert.That(clone.Attributes["class"], Is.EqualTo("clone"));
        Assert.That(original.Size, Is.EqualTo("2x"));
        Assert.That(original.Attributes["class"], Is.EqualTo("original"));
        original.Attributes["title"] = "Original";
        Assert.That(clone.Attributes.ContainsKey("title"), Is.False);
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
        Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.Combine(_root, ".cache", "icons", "kits")));
        var sprite = File.ReadAllText(path);
        var tree = XElement.Parse(sprite);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.That(tree.Elements(ns + "symbol").Count(x => !((string)x.Attribute("id")).StartsWith("source:")), Is.EqualTo(3));
        Assert.That(tree.Descendants(ns + "path").Count(), Is.EqualTo(2));
        Assert.That(tree.Elements(ns + "defs").Count(), Is.EqualTo(1));
        var symbols = tree.Elements(ns + "symbol").ToDictionary(x => (string)x.Attribute("id"));
        Assert.That((string)symbols["cart"].Element(ns + "use").Attribute("href"),
            Is.EqualTo((string)symbols["alias"].Element(ns + "use").Attribute("href")));
        Assert.That(symbols["copy"].Element(ns + "use"), Is.Null);
        Assert.That(symbols["copy"].Descendants(ns + "path").Count(), Is.EqualTo(1));
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
        Assert.That(Directory.GetFiles(Path.Combine(_root, ".cache", "icons", "kits")), Has.Length.EqualTo(1));

        Write("Icons/hugeicons/rounded/user/cart-01.svg", "<svg><script/></svg>");
        SignalChanges();
        var invalidRevision = kits.GetUrl("shared")[^28..^4];
        Assert.ThrowsAsync<InvalidDataException>(() => kits.GetSpriteFileAsync("shared", invalidRevision));
        Assert.That(Directory.GetFiles(Path.Combine(_root, ".cache", "icons", "kits")), Has.Length.EqualTo(1));
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
        Write(".cache/icons/kits/shared-test.svg", "<svg/>");
        Write(".cache/icons/browser/preview-test.svg", "<svg/>");
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

    /// <summary>
    /// Keeps mixed libraries and variants in one sprite while preserving source identity,
    /// mapping modifiers, independent viewBoxes and direct-source entries.
    /// </summary>
    [Test]
    public async Task Mixed_Kit_Uses_One_Sprite_Without_Populating_The_Icon_Cache()
    {
        WriteLibrary("other", "ot", "rd");
        var foreignSvg = """<svg viewBox="0 0 16 16"><path d="M1 1L8 8"/></svg>""";
        WriteZip("other", "rounded", ("cart-01", foreignSvg), ("raw", foreignSvg));
        Write("Icons/other/mapping.json", """{"foreign":"missing","cart-01":"missing","raw":"missing"}""");
        Write("Icons/hugeicons/sharp/icons/sharp-only.svg", """<svg viewBox="0 0 32 32"><path d="M2 2L8 8"/></svg>""");
        Write("Icons/hugeicons/rounded/icons/foreign.svg", _svg);
        Write("Icons/kits.json", """{"brands":{"icons":["cart","foreign","second","raw","sharp-only"],"sources":{"foreign":"ot:cart-01@rd","second":"ot:cart-01@rd","raw":"ot:raw@rd","sharp-only":"hi:sharp-only@sharp"}}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var url = kits.GetUrl("brands");
        var revision = Path.GetFileNameWithoutExtension(url)["brands-".Length..];
        foreach (var name in new[] { "cart", "foreign", "second", "raw", "sharp-only" })
        {
            var icon = await _service.GetIconAsync(name);
            Assert.That(icon, Is.Not.Null, name);
            Assert.That(kits.GetReference(icon)?.Href, Is.EqualTo(url + "#" + (name == "second" ? "foreign" : name)));
        }

        var foreign = await _service.GetIconAsync("OTHER:foreign@ROUNDED");
        Assert.That(foreign.LibraryName, Is.EqualTo("other"));
        Assert.That(foreign.Name, Is.EqualTo("cart-01"));
        Assert.That(kits.GetReference(foreign)?.Href, Is.EqualTo(url + "#foreign"));
        var overridden = await _service.GetIconAsync("foreign", library: "hi");
        Assert.That(overridden.LibraryName, Is.EqualTo("hugeicons"));
        Assert.That(kits.GetReference(overridden), Is.Null);
        Assert.That(kits.GetReference(await _service.GetIconAsync("ot:raw!@rd"))?.Href, Is.EqualTo(url + "#raw"));

        var path = await kits.GetSpriteFileAsync("brands", revision);
        var svg = XDocument.Load(path);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var symbols = svg.Root.Elements(ns + "symbol").ToDictionary(x => (string)x.Attribute("id"));
        Assert.That(symbols.Keys, Is.EquivalentTo(new[] { "cart", "foreign", "second", "raw", "sharp-only" }));
        Assert.That((string)symbols["cart"].Attribute("viewBox"), Is.EqualTo("-1 -2 24 25"));
        Assert.That((string)symbols["foreign"].Attribute("viewBox"), Is.EqualTo("0 0 16 16"));
        Assert.That((string)symbols["sharp-only"].Attribute("viewBox"), Is.EqualTo("0 0 32 32"));
        Assert.That(symbols["foreign"].Element(ns + "g"), Is.Null, "Concrete sources must not inherit a library mapping or transformation.");
        Assert.That(svg.Root.Elements(ns + "defs").Count(), Is.EqualTo(1), "Only the repeated canonical source needs shared artwork.");
        Assert.That(_sourceLookups["Icons/other/rounded/icons.zip"], Is.EqualTo(2), "One directory lookup and one archive open for all foreign SVGs.");
        using (var exclusive = File.Open(Path.Combine(_root, "Icons/other/rounded/icons.zip"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.That(exclusive.CanRead, Is.True, "Sprite generation must release foreign archives.");
        }

        _cache.VerifyNoOtherCalls();

        var manifestPath = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        Assert.That(manifest.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(7));
        var brands = manifest.RootElement.GetProperty("kits").GetProperty("brands");
        Assert.That(brands.GetProperty("icons").EnumerateArray().Select(x => x.GetString()), Is.EquivalentTo(symbols.Keys),
            "Each concept references its own symbol, including aliases sharing one source.");
        Assert.That(brands.GetProperty("url").GetString(), Is.EqualTo(url.TrimStart('/')));
        var sources = brands.GetProperty("sources");
        Assert.That(sources.GetProperty("cart").GetString(), Is.EqualTo("cart-01"));
        Assert.That(sources.GetProperty("foreign").GetString(), Is.EqualTo("ot:cart-01@rd"));
        Assert.That(sources.GetProperty("second").GetString(), Is.EqualTo("ot:cart-01@rd"));
        Assert.That(sources.GetProperty("sharp-only").GetString(), Is.EqualTo("sharp-only@sharp"));

        // Recover a missing mixed sprite from its public revision after a process restart.
        File.Delete(path);
        var restarted = new IconKitService(new IconService(_context.Object, _cache.Object), _context.Object, new HttpContextAccessor());
        Assert.That(await restarted.GetSpriteFileAsync("brands", revision), Is.EqualTo(path));
        Assert.That(File.Exists(path), Is.True);

        Write("Icons/other/rounded/user/cart-01.svg", foreignSvg.Replace("L8 8", "L9 9"));
        SignalChanges();
        Assert.That(kits.GetUrl("brands"), Is.Not.EqualTo(url), "A foreign source change must revise the whole mixed sprite.");
        Assert.That(await kits.GetSpriteFileAsync("brands", revision), Is.EqualTo(path), "Historical files remain available.");
    }

    /// <summary>
    /// Completes partially qualified entries from their kit context and honors caller overrides.
    /// </summary>
    [Test]
    public async Task Kit_Entry_Qualifiers_Are_Defaults_For_Individual_Icon_Requests()
    {
        WriteLibrary("other", "ot", "rd");
        WriteZip("other", "sharp", ("foreign", _svg), ("direct", _svg));
        Write("Icons/other/mapping.json", """{"direct":"missing"}""");
        Write("Icons/hugeicons/sharp/icons/variant-only.svg", _svg);
        Write("Icons/hugeicons/rounded/icons/variant-only.svg", _svg);
        Write("Icons/kits.json", """{"brands":{"icons":["foreign","variant-only","direct"],"sources":{"foreign":"ot:foreign","variant-only":"variant-only@sharp","direct":"direct"}}}""");
        Assert.That((await _service.GetIconAsync("foreign")).VariantName, Is.EqualTo("sharp"));
        Assert.That((await _service.GetIconAsync("variant-only")).VariantName, Is.EqualTo("sharp"));
        Assert.That((await _service.GetIconAsync("variant-only", variant: "sr")).VariantName, Is.EqualTo("rounded"));
        Assert.That((await _service.GetIconAsync("direct")).Name, Is.EqualTo("direct"), "A source target is concrete even without qualifiers.");
        Assert.That(await _service.GetIconAsync("direct", library: "ot"), Is.Null, "An explicit caller selection uses that library mapping.");
    }

    /// <summary>
    /// Rejects ambiguous symbol IDs and unknown qualifiers before a sprite is requested.
    /// </summary>
    /// <param name="entries">The invalid kit entries as a JSON array.</param>
    [TestCase("[\"cart\",\"hi:cart@sr\"]")]
    [TestCase("[\"missing:cart\"]")]
    [TestCase("[\"hi:cart@missing\"]")]
    [TestCase("[\"hi:cart@sr?flip=x\"]")]
    public void Kit_Entries_Reject_Invalid_Or_Ambiguous_Addresses(string entries)
    {
        Write("Icons/kits.json", "{\"brands\":{\"icons\":" + entries + "}}");
        Assert.Throws<InvalidDataException>(() => _ = _service.DefaultLibrary);
    }

    /// <summary>
    /// Source assignments cannot add members, recurse through mappings or select unknown variants.
    /// </summary>
    /// <param name="sources">The invalid source dictionary as JSON.</param>
    [TestCase("{\"orphan\":\"hi:cart-01@sr\"}")]
    [TestCase("{\"cart\":\"missing:cart-01\"}")]
    [TestCase("{\"cart\":\"hi:cart-01@missing\"}")]
    [TestCase("{\"cart\":\"hi:cart-01@sr?flip=x\"}")]
    [TestCase("{\"cart\":null}")]
    public void Kit_Sources_Must_Target_Registered_Selections_And_Declared_Members(string sources)
    {
        Write("Icons/kits.json", "{\"brands\":{\"icons\":[\"cart\"],\"sources\":" + sources + "}}");
        Assert.Throws<InvalidDataException>(() => _ = _service.DefaultLibrary);
    }

    /// <summary>
    /// Renamed concepts keep their public symbol while concrete source targets bypass library mappings.
    /// Changing a source assignment invalidates both sprite and resolved manifest identities.
    /// </summary>
    [Test]
    public async Task Kit_Source_Renames_Resolve_Once_And_Invalidate_The_Manifest()
    {
        Write("Icons/kits.json", """{"shared":{"icons":["assistant"],"sources":{"assistant":"hi:direct@sr"}}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var icon = await _service.GetIconAsync("assistant");
        Assert.That(icon.Name, Is.EqualTo("direct"), "The library's direct -> missing mapping must not be followed.");
        Assert.That(kits.GetReference(icon).Href, Does.EndWith("#assistant"));
        Assert.That(await _service.GetIconAsync("assistant!"), Is.Null, "A caller bypass skips kit routing as well.");
        var before = kits.GetManifestUrl();
        var beforeSprite = kits.GetUrl("shared");
        Write("Icons/kits.json", """{"shared":{"icons":["assistant"],"sources":{"assistant":"hi:cart-01@sr"}}}""");
        SignalChanges();
        Assert.That((await _service.GetIconAsync("assistant")).Name, Is.EqualTo("cart-01"));
        Assert.That(kits.GetManifestUrl(), Is.Not.EqualTo(before));
        Assert.That(kits.GetUrl("shared"), Is.Not.EqualTo(beforeSprite));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Restores source context without resolving artwork, generating sprites or losing explicit selectors.
    /// </summary>
    [TestCase(null, "shared", null, null)]
    [TestCase("", "shared", null, null)]
    [TestCase("cart", "shared", null, null)]
    [TestCase("cart?rotate=90", "shared", null, null)]
    [TestCase("direct", "backend", null, null)]
    [TestCase("cart!", null, "hugeicons", "rounded")]
    [TestCase("unlisted", null, "hugeicons", "rounded")]
    [TestCase("hi:cart@sr", null, "hugeicons", "rounded")]
    [TestCase("hi:missing!@sharp?flip=x", null, "hugeicons", "sharp")]
    [TestCase("unknown:cart", null, null, null)]
    [TestCase("hi:cart@unknown", null, null, null)]
    [TestCase("system:cart", null, null, null)]
    public void Browser_Restores_Source(string address, string kit, string library, string variant)
    {
        Write("Icons/kits.json", """{"backend":["cart","direct"],"shared":["cart"]}""");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);

        var source = browser.GetSource(address);

        Assert.That(source, Is.EqualTo(kit == null && library == null ? null : new IconBrowserSource(kit, library, variant)));
        Assert.That(Directory.Exists(Path.Combine(_root, ".cache")), Is.False);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// An empty editor without Shared and unqualified names honor the global variant override.
    /// </summary>
    [TestCase(null)]
    [TestCase("unlisted")]
    public void Browser_Restores_Global_Variant_Without_Kit(string address)
    {
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"sharp"}""");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);

        Assert.That(browser.GetSource(address), Is.EqualTo(new IconBrowserSource(null, "hugeicons", "sharp")));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// The first small result page prepares all variant icons once, retaining override priority and literal names.
    /// </summary>
    [Test]
    public async Task Browser_Prepares_Complete_Variant_Once_Without_Individual_Cache()
    {
        Write("Icons/hugeicons/rounded/icons/custom.svg", _svg);
        Write("Icons/hugeicons/rounded/user/cart-01.svg", """<svg viewBox="0 0 16 16"><path d="M3 3L9 9"/></svg>""");
        var http = new DefaultHttpContext();
        http.Request.PathBase = "/shop";
        var accessor = new HttpContextAccessor { HttpContext = http };
        var kits = new IconKitService(_service, _context.Object, accessor);
        var browser = new IconBrowser(_service, kits, accessor);
        var query = new IconSearchQuery { Library = "hi", Variant = "sr", Take = 1 };
        var pages = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => browser.SearchAsync(query)));
        var first = pages[0];
        Assert.That(first.TotalCount, Is.EqualTo(4));
        Assert.That(first.Items, Has.Count.EqualTo(1));
        Assert.That(first.Items[0].Value, Is.EqualTo("hi:cart-01!@sr"));
        Assert.That(first.SpriteUrl, Does.StartWith("/shop/icons/browser/hugeicons/rounded/"));
        Assert.That(pages.Select(x => x.SpriteUrl).Distinct().Count(), Is.EqualTo(1));
        var revision = Path.GetFileNameWithoutExtension(first.SpriteUrl);
        var path = await browser.GetSpriteFileAsync("hi", "sr", revision);
        Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.Combine(_root, ".cache", "icons", "browser")));
        XNamespace ns = "http://www.w3.org/2000/svg";
        var sprite = XDocument.Load(path);
        var symbols = sprite.Root.Elements(ns + "symbol").ToDictionary(x => (string)x.Attribute("id"));
        Assert.That(symbols.Keys, Is.EquivalentTo(new[] { "cart-01", "custom", "direct", "package-add-01 " }));
        Assert.That((string)symbols["cart-01"].Attribute("viewBox"), Is.EqualTo("0 0 16 16"));
        Assert.That(_sourceLookups["Icons/hugeicons/rounded/icons.zip"], Is.EqualTo(2), "One index read and one archive open for the entire sprite.");
        query.Skip = 2;
        var next = await browser.SearchAsync(query);
        Assert.That(next.Items[0].Value, Is.EqualTo("hi:direct!@sr"), "The direct -> missing mapping must not affect library browsing.");
        query.Skip = 0;
        query.Term = "basket";
        Assert.That((await browser.SearchAsync(query)).Items[0].Name, Is.EqualTo("cart-01"));
        Assert.That(_sourceLookups["Icons/hugeicons/rounded/icons.zip"], Is.EqualTo(2));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)), Has.Length.EqualTo(1));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Kit browsing uses its own sources and tags, preserving conceptual values and baked modifiers.
    /// </summary>
    [Test]
    public async Task Browser_Kit_Preview_Uses_Selected_Kit_With_Baked_Stroke_Scale()
    {
        Write("Icons/kits.json", """{"shared":{"icons":["cart"],"sources":{"cart":"hi:direct@sr"}},"backend":["cart","thick"]}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01?flip=x","thick":"cart-01?rotate=90&stroke-scale=1.2"}""");
        var accessor = new HttpContextAccessor();
        var kits = new IconKitService(_service, _context.Object, accessor);
        var browser = new IconBrowser(_service, kits, accessor);
        var result = await browser.SearchAsync(new IconSearchQuery { Term = "basket" }, "backend");
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.Items[0].Name, Is.EqualTo("cart"));
        Assert.That(result.Items[0].Value, Is.EqualTo("cart"));
        Assert.That(result.Items[0].Address, Is.EqualTo("hi:cart-01@sr"), "The selected kit, not shared, determines the preview.");
        Assert.That(result.Items[0].InlineName, Is.Null);
        Assert.That(result.Items[1].InlineName, Is.Null);
        Assert.That(result.Items[1].Name, Is.EqualTo("thick"));
        Assert.That(Directory.GetFiles(Path.Combine(_root, ".cache", "icons", "kits"), "*.svg"), Has.Length.EqualTo(1));
        Assert.That(Directory.Exists(Path.Combine(_root, ".cache", "icons", "browser")), Is.False);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// A source change produces a new picker sprite without replacing historical bytes.
    /// </summary>
    [Test]
    public async Task Browser_Revisions_Follow_Source_Changes_And_Reject_Unknown_Revisions()
    {
        var accessor = new HttpContextAccessor();
        var kits = new IconKitService(_service, _context.Object, accessor);
        var browser = new IconBrowser(_service, kits, accessor);
        var query = new IconSearchQuery { Library = "hi", Variant = "sr" };
        var first = await browser.SearchAsync(query);
        var revision = Path.GetFileNameWithoutExtension(first.SpriteUrl);
        var oldPath = await browser.GetSpriteFileAsync("hi", "sr", revision);
        var bytes = File.ReadAllBytes(oldPath);
        Write("Icons/hugeicons/rounded/user/new.svg", _svg);
        SignalChanges();
        var second = await browser.SearchAsync(query);
        Assert.That(second.SpriteUrl, Is.Not.EqualTo(first.SpriteUrl));
        Assert.That(second.TotalCount, Is.EqualTo(first.TotalCount + 1));
        Assert.That(await browser.GetSpriteFileAsync("hi", "sr", revision), Is.EqualTo(oldPath));
        Assert.That(File.ReadAllBytes(oldPath), Is.EqualTo(bytes));
        Assert.That(await browser.GetSpriteFileAsync("hi", "sr", "../invalid"), Is.Null);
        Assert.That(await browser.GetSpriteFileAsync("hi", "sr", new string('0', 24)), Is.Null);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Hidden system libraries, unknown sources and conflicting source selectors cannot generate sprites.
    /// </summary>
    [Test]
    public async Task Browser_Rejects_Excluded_And_Invalid_Selections()
    {
        WriteLibrary("system", "sys", "s");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);
        foreach (var library in new[] { "system", "sys", "unknown" })
        {
            Assert.That(await browser.SearchAsync(new IconSearchQuery { Library = library }), Is.Null);
            Assert.That(await browser.GetSpriteFileAsync(library, "sharp", new string('0', 24)), Is.Null);
        }
        Assert.That(await browser.SearchAsync(new IconSearchQuery { Library = "hi", Variant = "unknown" }), Is.Null);
        Assert.That(await browser.SearchAsync(new IconSearchQuery(), "unknown"), Is.Null);
        Assert.ThrowsAsync<ArgumentException>(() => browser.SearchAsync(new IconSearchQuery { Library = "hi" }, "shared"));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => browser.SearchAsync(new IconSearchQuery { Take = 501 }));
        Assert.That(Directory.Exists(Path.Combine(_root, ".cache")), Is.False);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// One matching alternate-library icon must not force generation of an incomplete kit.
    /// </summary>
    [Test]
    public async Task Incomplete_Alternate_Kit_Falls_Back_To_Individual_Rendering()
    {
        WriteLibrary("other", "ot", "rd");
        WriteZip("other", "rounded", ("cart", _svg), ("foreign", _svg));
        Write("Icons/kits.json", """{"brands":{"defaultLibrary":"other","defaultVariant":"rounded","icons":["cart","foreign"]}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var selected = await _service.GetIconAsync("hi:cart@sr");
        Assert.That(kits.GetReference(selected), Is.Null);
        var normal = await _service.GetIconAsync("cart");
        Assert.That(kits.GetReference(normal), Is.Not.Null);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Kit and library searches share word combination, total counts and paging while retaining their own labels.
    /// </summary>
    [Test]
    public async Task Browser_Search_Combines_Fields_And_Pages_Both_Source_Types()
    {
        Write("Icons/kits.json", """{"backend":["z-cart","a-cart"]}""");
        Write("Icons/hugeicons/mapping.json", """{"z-cart":"cart-01","a-cart":"direct"}""");
        Write("Icons/hugeicons/metadata.json", """{"icons":{"cart-01":{"tags":["basket"]},"direct":{"tags":["basket"]}}}""");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);
        var query = new IconSearchQuery { Term = "CART bask", Skip = 1, Take = 1 };
        var kit = await browser.SearchAsync(query, "backend");
        var library = await browser.SearchAsync(query);
        Assert.That(kit.TotalCount, Is.EqualTo(2));
        Assert.That(library.TotalCount, Is.EqualTo(2));
        Assert.That(kit.Items.Single().Name, Is.EqualTo("z-cart"));
        Assert.That(library.Items.Single().Name, Is.EqualTo("direct"));
        query.Skip = 2;
        foreach (var source in new[] { "backend", null })
        {
            var page = await browser.SearchAsync(query, source);
            Assert.That(page.TotalCount, Is.EqualTo(2));
            Assert.That(page.Items, Is.Empty);
        }
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Empty picker searches must not load supplemental tags for either source type.
    /// </summary>
    [TestCase(null)]
    [TestCase("shared")]
    public async Task Browser_Empty_Search_Does_Not_Load_Tags(string kit)
    {
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        Write("Icons/hugeicons/metadata.json", "invalid JSON that must remain unread");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);
        var page = await browser.SearchAsync(new IconSearchQuery { Term = "  ", Take = 1 }, kit);
        Assert.That(page.Items, Has.Count.EqualTo(1));
        Assert.That(page.TotalCount, Is.EqualTo(kit == null ? 3 : 1));
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Failed sprite generation removes temporary files and never exposes a partial kit or variant sprite.
    /// </summary>
    [TestCase(null)]
    [TestCase("shared")]
    public void Browser_Failed_Sprite_Publication_Leaves_No_Files(string kit)
    {
        Write("Icons/kits.json", """{"shared":["cart"]}""");
        Write("Icons/hugeicons/rounded/user/cart-01.svg", "<svg><path");
        var accessor = new HttpContextAccessor();
        var browser = new IconBrowser(_service, new IconKitService(_service, _context.Object, accessor), accessor);
        Assert.ThrowsAsync<System.Xml.XmlException>(() => browser.SearchAsync(new IconSearchQuery(), kit));
        var directory = Path.Combine(_root, ".cache", "icons", kit == null ? "browser" : "kits");
        Assert.That(Directory.GetFiles(directory), Is.Empty);
        _cache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Direct XML composition preserves root presentation, local references and either source or fallback coordinates.
    /// </summary>
    [TestCase(null, false, false)]
    [TestCase(null, true, false)]
    [TestCase("-2 -3 16 16", false, false)]
    [TestCase("-2 -3 16 16", true, false)]
    [TestCase("-2 -3 16 16", false, true)]
    [TestCase("-2 -3 16 16", true, true)]
    public async Task Sprite_Xml_Preserves_Source_Root_And_References(string viewBox, bool shared, bool transformed)
    {
        var coordinates = viewBox == null ? string.Empty : $"viewBox=\"{viewBox}\"";
        Write("Icons/hugeicons/rounded/user/cart-01.svg", $$"""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"
                 id="root" {{coordinates}} preserveAspectRatio="xMinYMin meet" transform="translate(2 3)"
                 fill="url(#paint)" aria-labelledby="caption" class="bi bi-test" width="16" height="16">
                <!-- Source comment must be removed. -->
                <title id="caption">A &amp; B</title>
                <defs><linearGradient id="paint"><stop offset="0" stop-color="red"/></linearGradient></defs>
                <g id="shape"><path class="inner" d="M0 0L1 1"/></g>
                <use xlink:href="#shape"/>
            </svg>
            """);
        Write("Icons/kits.json", shared ? """{"shared":["first","second"]}""" : """{"shared":["first"]}""");
        Write("Icons/hugeicons/mapping.json", transformed
            ? """{"first":"cart-01?rotate=90","second":"cart-01"}"""
            : """{"first":"cart-01","second":"cart-01"}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var path = await kits.GetSpriteFileAsync("shared", kits.GetUrl("shared")[^28..^4]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var xml = XElement.Load(path);
        var first = xml.Elements(ns + "symbol").Single(x => (string)x.Attribute("id") == "first");
        var content = transformed ? first.Element(ns + "g").Elements().Single() : first.Elements().Single();
        Assert.That(xml.Elements(ns + "defs").Count(), Is.EqualTo(shared ? 1 : 0));
        Assert.That(content.Name, Is.EqualTo(ns + (shared ? "use" : "g")));
        if (transformed)
        {
            Assert.That((string)first.Element(ns + "g").Attribute("transform"), Does.Contain("rotate(90)"));
        }
        if (shared)
        {
            Assert.That((string)content.Attribute("href"), Is.EqualTo("#source:0"));
            Assert.That((string)xml.Elements(ns + "symbol").Single(x => (string)x.Attribute("id") == "second")
                .Element(ns + "use").Attribute("href"), Is.EqualTo("#source:0"));
        }
        var root = shared ? xml.Element(ns + "defs").Element(ns + "g").Element(ns + "g") : content;
        Assert.That((string)root.Attribute("id"), Is.EqualTo("source:0:root"));
        Assert.That((string)root.Attribute("transform"), Is.EqualTo("translate(2 3)"));
        Assert.That((string)root.Attribute("fill"), Is.EqualTo("url(#source:0:paint)"));
        Assert.That((string)root.Attribute("aria-labelledby"), Is.EqualTo("source:0:caption"));
        Assert.That(root.Element(ns + "title").Value, Is.EqualTo("A & B"));
        Assert.That((string)root.Element(ns + "use").Attribute("href"), Is.EqualTo("#source:0:shape"));
        Assert.That(root.Descendants(ns + "path").Count(), Is.EqualTo(1));
        Assert.That((string)root.Descendants(ns + "path").Single().Attribute("class"), Is.EqualTo("inner"));
        Assert.That(root.Attributes().Select(x => x.Name.LocalName),
            Does.Not.Contain("viewBox").And.Not.Contain("preserveAspectRatio").And.Not.Contain("class").And.Not.Contain("width").And.Not.Contain("height"));
        Assert.That(xml.DescendantNodes().OfType<XComment>(), Is.Empty);
        var symbols = xml.Elements(ns + "symbol").ToArray();
        Assert.That(symbols, Has.Length.EqualTo(shared ? 2 : 1));
        Assert.That(symbols.All(x => (string)x.Attribute("viewBox") == (viewBox ?? "0 0 24 24")), Is.True);
        Assert.That(symbols.All(x => (string)x.Attribute("preserveAspectRatio") == "xMinYMin meet"), Is.True);
        _cache.VerifyNoOtherCalls();

        // Sprite composition must not leak its ID prefixes or moved attributes into cached single icons.
        var payload = await _service.GetSvgAsync("first");
        Assert.That(payload.ViewBox, Is.EqualTo(viewBox ?? "0 0 24 24"));
        Assert.That(payload.RootAttributes["id"], Is.EqualTo("root"));
        Assert.That(payload.RootAttributes["preserveAspectRatio"], Is.EqualTo("xMinYMin meet"));
        Assert.That(payload.Content, Does.Contain("href=\"#shape\"").And.Not.Contain("source:0:"));
    }

    /// <summary>
    /// Cyclic variant fallbacks retain mapping modifiers, report actual identities and share SVG cache entries.
    /// </summary>
    [Test]
    public async Task Variant_Fallbacks_Resolve_Cycles_And_Use_Actual_Cache_Identity()
    {
        WriteFallbackLibrary();
        Write("Icons/other/mapping.json", """{"cart":"cart-01?flip=x&rotate=90"}""");
        Write("Icons/other/solid/icons/cart-01.svg", _svg);
        var icon = await _service.GetIconAsync("ot:cart@r");
        Assert.That(icon.Address, Is.EqualTo("ot:cart-01@s"));
        Assert.That(icon.VariantName, Is.EqualTo("solid"));
        Assert.That(icon.Transform, Is.EqualTo(new IconTransform(true, false, 90)));
        Assert.That(await _service.GetIconAsync("ot:missing@r"), Is.Null, "A mutual fallback must terminate.");
        Assert.That((await _service.GetIconAsync("ot:cart-01!@r")).Address, Is.EqualTo(icon.Address));
        Assert.That(await _service.GetIconAsync("ot:cart@b"), Is.Null, "Brands must not implicitly use regular or solid.");
        var payload = await _service.GetSvgAsync(icon);
        var direct = await _service.GetSvgAsync("ot:cart-01!@s");
        Assert.That(payload.Address, Is.EqualTo(direct.Address));
        Assert.That(_entries.Count, Is.EqualTo(1));
        Assert.That(_entries.Keys.Single(), Does.StartWith("ot:cart-01@s:"));
    }

    /// <summary>
    /// Fallbacks follow declared depth-first order, collapse duplicate aliases and reload after manifest edits.
    /// </summary>
    [Test]
    public async Task Variant_Fallback_Order_And_Watcher_Invalidation_Are_Deterministic()
    {
        Write("Icons/other/library.json", """
            {"shortName":"ot","defaultVariant":"regular","variants":{
              "regular":{"shortName":"r","fallbacks":["s","third","solid"]},
              "solid":{"shortName":"s","fallbacks":["r","deep"]},
              "deep":{},"third":{}}}
            """);
        Write("Icons/other/deep/icons/direct.svg", _svg);
        Write("Icons/other/third/icons/direct.svg", _svg);
        Assert.That((await _service.GetIconAsync("ot:direct")).VariantName, Is.EqualTo("deep"));
        Write("Icons/other/solid/user/direct.svg", _svg);
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:direct")).VariantName, Is.EqualTo("solid"));
        Write("Icons/other/regular/icons/direct.svg", _svg);
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:direct")).VariantName, Is.EqualTo("regular"));
        Write("Icons/other/library.json", """
            {"shortName":"ot","defaultVariant":"third","variants":{
              "regular":{"fallbacks":["third","solid"]},"solid":{},"third":{}}}
            """);
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:direct")).VariantName, Is.EqualTo("third"));
    }

    /// <summary>
    /// A missing or incomplete mapping can use the system default, while direct addresses and unknown selectors stay local.
    /// </summary>
    [Test]
    public async Task Library_Fallback_Remaps_Original_Concept_And_Is_Opt_In()
    {
        WriteFallbackLibrary();
        Assert.That(await _service.GetIconAsync("ot:cart"), Is.Null);
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","fallbackToDefaultLibrary":true}""");
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:cart")).Address, Is.EqualTo("hi:cart-01@sr"), "No mapping.json is required in the new library.");
        Write("Icons/other/mapping.json", """{"cart":"foreign-cart?flip=y&stroke-scale=2"}""");
        Write("Icons/hugeicons/mapping.json", """{"cart":"cart-01?rotate=90&stroke-scale=1.1"}""");
        SignalChanges();
        var fallback = await _service.GetIconAsync("ot:cart?flip=x");
        Assert.That(fallback.Address, Is.EqualTo("hi:cart-01@sr"));
        Assert.That(fallback.Transform, Is.EqualTo(new IconTransform(true, false, 90)));
        Assert.That(fallback.StrokeScale, Is.EqualTo(1.1));
        Assert.That(await _service.GetIconAsync("ot:cart!"), Is.Null);
        Assert.That(await _service.GetIconAsync("ot:cart-01!"), Is.Null);
        Assert.That(await _service.GetIconAsync("ot:cart@missing"), Is.Null);
        Assert.That(await _service.GetIconAsync("missing:cart"), Is.Null);
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","fallbackToDefaultLibrary":false}""");
        SignalChanges();
        Assert.That(await _service.GetIconAsync("ot:cart"), Is.Null);
    }

    /// <summary>
    /// Same-name local artwork wins without a mapping; an existing broken mapping cannot silently select another local name.
    /// </summary>
    [Test]
    public async Task Direct_Name_Precedes_System_Fallback_But_Does_Not_Override_A_Mapping()
    {
        WriteFallbackLibrary();
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","fallbackToDefaultLibrary":true}""");
        Write("Icons/other/solid/icons/cart.svg", _svg);
        Assert.That((await _service.GetIconAsync("ot:cart")).Address, Is.EqualTo("ot:cart@s"));
        Write("Icons/other/mapping.json", """{"cart":"missing"}""");
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:cart")).Address, Is.EqualTo("hi:cart-01@sr"));
        Assert.That((await _service.GetIconAsync("ot:cart!@r")).Address, Is.EqualTo("ot:cart@s"));
        Write("Icons/other/regular/icons/missing.svg", "<not-svg />");
        SignalChanges();
        Assert.That((await _service.GetIconAsync("ot:cart")).LibraryName, Is.EqualTo("other"));
        Assert.ThrowsAsync<InvalidDataException>(() => _service.GetSvgAsync("ot:cart"));
    }

    /// <summary>
    /// Mixed fallback kits keep server references, browser manifest identities and native variant inventories consistent.
    /// </summary>
    [Test]
    public async Task Fallback_Kit_And_Manifest_Use_The_Same_Resolved_Sources()
    {
        WriteFallbackLibrary();
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","fallbackToDefaultLibrary":true}""");
        Write("Icons/kits.json", """{"shared":{"defaultLibrary":"other","defaultVariant":"regular","icons":["cart","local","remote"]}}""");
        Write("Icons/other/mapping.json", """{"cart":"cart-01","local":"local-solid?rotate=90","remote":"not-here?flip=y"}""");
        Write("Icons/hugeicons/mapping.json", """{"remote":"direct"}""");
        Write("Icons/other/regular/icons/cart-01.svg", _svg);
        Write("Icons/other/solid/icons/local-solid.svg", _svg);
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var url = kits.GetUrl("shared");
        var path = await kits.GetSpriteFileAsync("shared", url[^28..^4]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.That(XElement.Load(path).Elements(ns + "symbol").Select(x => (string)x.Attribute("id")),
            Is.EquivalentTo(new[] { "cart", "local", "remote" }));
        var expected = new[] { "ot:cart-01@r", "ot:local-solid@s", "hi:direct@sr" };
        var concepts = new[] { "cart", "local", "remote" };
        for (var i = 0; i < concepts.Length; i++)
        {
            var info = await _service.GetIconAsync(concepts[i]);
            Assert.That(info.Address, Is.EqualTo(expected[i]));
            Assert.That(kits.GetReference(info)?.Href, Is.EqualTo(url + "#" + concepts[i]));
            var output = await new IconRenderer(_service, kits).RenderAsync(info);
            Assert.That(output.Attributes["data-icon"], Is.EqualTo(expected[i]));
            Assert.That(output.Attributes["class"], Does.Contain("icon-" + info.LibraryKey + "-" + info.VariantKey));
        }
        var manifestPath = await kits.GetManifestFileAsync(Path.GetFileNameWithoutExtension(kits.GetManifestUrl()));
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        var kit = manifest.RootElement.GetProperty("kits").GetProperty("shared");
        Assert.That(kit.GetProperty("url").GetString(), Is.EqualTo(url.TrimStart('/')));
        Assert.That(kit.GetProperty("sources").GetProperty("local").GetString(), Is.EqualTo("local-solid@s"));
        Assert.That(kit.GetProperty("sources").GetProperty("remote").GetString(), Is.EqualTo("hi:direct@sr"));
        Assert.That(_service.GetIconCount("ot", "r"), Is.EqualTo(1));
        var search = await _service.SearchAsync(new IconSearchQuery { Library = "ot", Variant = "r" });
        Assert.That(search.Items.Select(x => x.Name), Is.EqualTo(new[] { "cart-01" }));
        _cache.VerifyNoOtherCalls();

        // Availability changes must rebuild both the mixed sprite revision and its identity patches.
        Write("Icons/other/regular/user/local-solid.svg", _svg);
        SignalChanges();
        var newUrl = kits.GetUrl("shared");
        Assert.That(newUrl, Is.Not.EqualTo(url));
        var local = await _service.GetIconAsync("local");
        Assert.That(local.Address, Is.EqualTo("ot:local-solid@r"));
        Assert.That(kits.GetReference(local)?.Href, Is.EqualTo(newUrl + "#local"));
        Assert.That(await kits.GetSpriteFileAsync("shared", url[^28..^4]), Is.EqualTo(path));
    }

    /// <summary>
    /// A concrete kit source may use another variant but must never switch libraries or follow mappings.
    /// </summary>
    [Test]
    public async Task Pinned_Kit_Sources_Use_Only_Variant_Fallbacks()
    {
        WriteFallbackLibrary();
        Write("Icons/config.json", """{"defaultLibrary":"hugeicons","defaultVariant":"rounded","fallbackToDefaultLibrary":true}""");
        Write("Icons/other/mapping.json", """{"cart-01":"missing"}""");
        Write("Icons/other/solid/icons/cart-01.svg", _svg);
        Write("Icons/kits.json", """{"shared":{"icons":["cart"],"sources":{"cart":"ot:cart-01@r"}}}""");
        var kits = new IconKitService(_service, _context.Object, new HttpContextAccessor());
        var info = await _service.GetIconAsync("cart");
        Assert.That(info.Address, Is.EqualTo("ot:cart-01@s"));
        Assert.That(kits.GetReference(info)?.Href, Is.EqualTo(kits.GetUrl("shared") + "#cart"));
        Write("Icons/kits.json", """{"shared":{"icons":["cart"],"sources":{"cart":"ot:direct@r"}}}""");
        SignalChanges();
        Assert.That(await _service.GetIconAsync("cart"), Is.Null);
        Assert.Throws<InvalidDataException>(() => kits.GetUrl("shared"));
    }

    /// <summary>
    /// Invalid variant references are rejected at manifest load, while caller-owned fallback collections cannot mutate manifests.
    /// </summary>
    [Test]
    public void Fallback_Configuration_Is_Validated_And_Immutable()
    {
        var values = new[] { "solid" };
        var variant = new IconVariant { Fallbacks = values };
        values[0] = "other";
        Assert.That(variant.Fallbacks, Is.EqualTo(new[] { "solid" }));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)variant.Fallbacks)[0] = "other");
        Write("Icons/other/library.json", """{"defaultVariant":"regular","variants":{"regular":{"fallbacks":["missing"]}}}""");
        Assert.Throws<InvalidDataException>(() => _service.GetLibrary("other"));
    }

    /// <summary>
    /// Defines an unequal library with mutually linked regular/solid variants and an isolated brands variant.
    /// </summary>
    private void WriteFallbackLibrary()
    {
        Write("Icons/other/library.json", """
            {"shortName":"ot","defaultVariant":"regular","variants":{
              "regular":{"shortName":"r","fallbacks":["s"]},
              "solid":{"shortName":"s","fallbacks":["r"]},
              "brands":{"shortName":"b"}}}
            """);
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
