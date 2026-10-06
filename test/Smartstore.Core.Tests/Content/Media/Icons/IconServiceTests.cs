using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        files.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) => _provider.GetFileInfo(path));
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
    public void Address_Rejects_Malformed_Input(string value)
    {
        Assert.That(IconAddress.TryParse(value, out _), Is.False);
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
        Assert.That(icon.Variant, Is.EqualTo("rounded"));
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
        Assert.That(icon.Variant, Is.EqualTo("sharp"));
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
        Assert.That(svg.RootAttributes["width"], Is.EqualTo("1em"));
        Assert.That(svg.RootAttributes["height"], Is.EqualTo("1em"));
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
    /// Reloads overrides and changes revisions even when timestamps and lengths match.
    /// </summary>
    [Test]
    public async Task Source_Changes_Invalidate_Revision()
    {
        var original = await _service.GetSvgAsync("cart");
        string overridePath = "Icons/hugeicons/rounded/overrides/cart-01.svg";
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
        Write("Icons/hugeicons/rounded/overrides/cart-01.svg", svg);
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
            ["rounded"] = new IconVariant { GridSize = 24, ShortName = "sr", StrokeWidthScale = 1.5 }
        };
        var library = new IconLibrary { ShortName = "hi", DefaultVariant = "rounded", Variants = variants };
        variants.Clear();
        Assert.That(library.Variants.Count, Is.EqualTo(1));
        var restored = JsonSerializer.Deserialize<IconLibrary>(JsonSerializer.Serialize(library));
        Assert.That(restored.ShortName, Is.EqualTo("hi"));
        Assert.That(restored.Variants["ROUNDED"].StrokeWidthScale, Is.EqualTo(1.5));
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
    [Test]
    public async Task Override_Can_Add_An_Icon()
    {
        Write("Icons/hugeicons/rounded/overrides/custom.svg", _svg);
        Assert.That(await _service.GetSvgAsync("custom"), Is.Not.Null);
        var result = await _service.SearchAsync(new IconSearchQuery { Term = "custom" });
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items[0].Tags, Is.Empty);
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
    /// Uses grid coordinates only when no viewBox is supplied.
    /// </summary>
    [Test]
    public async Task Missing_ViewBox_Uses_Grid()
    {
        Write("Icons/hugeicons/rounded/overrides/custom.svg", "<svg><path d=\"M0 0L1 1\"/></svg>");
        Assert.That((await _service.GetSvgAsync("custom")).ViewBox, Is.EqualTo("0 0 24 24"));
    }

    /// <summary>
    /// Preserves local references without allowing externally loaded resources.
    /// </summary>
    [Test]
    public async Task Local_References_Are_Preserved()
    {
        Write("Icons/hugeicons/rounded/overrides/custom.svg", """
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
        Write("Icons/hugeicons/rounded/overrides/custom.svg", _svg);
        Assert.That(await _service.GetIconAsync("custom"), Is.Not.Null);
        File.Delete(Path.Combine(_root, "Icons/hugeicons/rounded/overrides/custom.svg"));
        Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetSvgAsync("custom"));
    }

    /// <summary>
    /// A changed override cannot populate a cache entry under the previously discovered revision.
    /// </summary>
    [Test]
    public async Task Changed_Override_Is_Rejected_Before_Caching()
    {
        Write("Icons/hugeicons/rounded/overrides/custom.svg", _svg);
        Assert.That(await _service.GetIconAsync("custom"), Is.Not.Null);
        Write("Icons/hugeicons/rounded/overrides/custom.svg", _svg.Replace("M0 0L1 1", "M0 0L2 2"));
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
            ? "Icons/hugeicons/rounded/overrides/custom.svg"
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
        Write("Icons/hugeicons/rounded/overrides/cart-01.svg", _svg.Replace("M0 0L1 1", "M0 0L3 3"));
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
        Write("Icons/hugeicons/rounded/overrides/custom.svg", _svg);
        Write("Icons/hugeicons/rounded/overrides/broken.svg", "not SVG");
        Write("Icons/hugeicons/rounded/icons.zip", "not a ZIP");
        Write("Icons/hugeicons/metadata.json", "not JSON");
        Assert.That(await _service.GetSvgAsync("custom"), Is.Not.Null);
        var files = Mock.Get(_context.Object.AppDataRoot);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/icons.zip"), Times.Never);
        files.Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/overrides/broken.svg"), Times.Never);
        files.Verify(x => x.GetDirectoryContents(It.Is<string>(p => p.EndsWith("overrides"))), Times.Never);
    }

    /// <summary>
    /// Search lists custom names without parsing or fingerprinting any override contents.
    /// </summary>
    [Test]
    public async Task Search_Does_Not_Read_Override_Contents()
    {
        Write("Icons/hugeicons/rounded/overrides/custom.svg", "not SVG");
        Assert.That((await _service.SearchAsync(new IconSearchQuery { Term = "custom" })).TotalCount, Is.EqualTo(1));
        Mock.Get(_context.Object.AppDataRoot).Verify(x => x.GetFileInfo("Icons/hugeicons/rounded/overrides/custom.svg"), Times.Never);
    }

    /// <summary>
    /// Scales inherited widths and explicit overrides once while retaining their relative weights.
    /// </summary>
    [Test]
    public async Task Stroke_Width_Scale_Preserves_Inheritance_And_Units()
    {
        Write("Icons/hugeicons/rounded/overrides/scaled.svg", """
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
        Write("Icons/hugeicons/rounded/overrides/initial.svg", """<svg><path stroke="black" d="M0 0L1 1"/></svg>""");
        var scaled = await _service.GetSvgAsync("initial");
        Assert.That(scaled.Content, Does.Contain("stroke-width:var(--icon-stroke-width,1.6)"));
        Write("Icons/hugeicons/sharp/overrides/unscaled.svg", _svg);
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
        Write("Icons/hugeicons/rounded/overrides/colors.svg", """
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
        Write("Icons/hugeicons/rounded/overrides/injection.svg", """<svg><path stroke="red;opacity:0"/></svg>""");
        Assert.ThrowsAsync<InvalidDataException>(async () => await _service.GetSvgAsync("injection"));
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
                rounded = new { gridSize = 24, shortName = variantShortName, strokeWidthScale = 1.6 },
                sharp = new { gridSize = 24 }
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
