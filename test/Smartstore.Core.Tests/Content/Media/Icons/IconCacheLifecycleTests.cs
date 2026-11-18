using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using Smartstore.IconKit;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Core.Tests.Content.Media.Icons;

[TestFixture]
public class IconCacheLifecycleTests
{
    private string _root;
    private LocalFileSystem _sources;
    private LocalFileSystem _contentRoot;
    private Mock<IApplicationContext> _context;
    private IOptions<IconKitOptions> _options;
    private readonly List<IconCacheLifecycle> _lifecycles = [];
    private readonly Dictionary<IconCacheLifecycle, Mock<IIconCacheRecovery>> _recoveries = [];
    private static readonly IChangeToken Unchanged = NullChangeToken.Singleton;
    private const string Old = "kits/public-aaaaaaaaaaaaaaaaaaaaaaaa.svg";
    private const string Current = "kits/public-bbbbbbbbbbbbbbbbbbbbbbbb.svg";

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(TestContext.CurrentContext.WorkDirectory, ".temp", "icon-lifecycle", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "sources"));
        Directory.CreateDirectory(Path.Combine(_root, "cache", "kits"));
        _sources = new LocalFileSystem(Path.Combine(_root, "sources"));
        _contentRoot = new LocalFileSystem(_root);
        _context = new Mock<IApplicationContext>();
        _context.SetupGet(x => x.AppDataRoot).Returns(_sources);
        _context.SetupGet(x => x.ContentRoot).Returns(_contentRoot);
        _options = Options.Create(new IconKitOptions { SourcePath = _sources.Root, CachePath = Path.Combine(_root, "cache") });
    }

    [TearDown]
    public async Task TearDown()
    {
        foreach (var lifecycle in _lifecycles) await lifecycle.DisposeAsync();
        _lifecycles.Clear();
        _recoveries.Clear();
        _sources.Dispose();
        _contentRoot.Dispose();
        Directory.Delete(_root, true);
    }

    private IconCacheLifecycle Create()
    {
        var recovery = new Mock<IIconCacheRecovery>();
        var lifecycle = new IconCacheLifecycle(_context.Object, _options, recovery.Object);
        _lifecycles.Add(lifecycle);
        _recoveries.Add(lifecycle, recovery);
        return lifecycle;
    }

    private Task TrackAsync(IconCacheLifecycle lifecycle, IconCacheArtifact artifact, IChangeToken token,
        Func<IReadOnlySet<string>, IReadOnlyCollection<IconCacheArtifact>> recover)
    {
        _recoveries[lifecycle]
            .Setup(x => x.GetCurrentFiles(It.IsAny<IconCatalog>(), It.IsAny<IReadOnlySet<string>>()))
            .Returns((IconCatalog _, IReadOnlySet<string> files) => recover(files));

        return lifecycle.TrackCacheFileAsync(artifact, new IconCatalog(token));
    }

    private void Write(string name, int ageMinutes = 0)
    {
        var path = Path.Combine(_options.Value.CachePath, name);
        File.WriteAllText(path, "<svg/>");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-ageMinutes));
    }

    private static IconCacheArtifact Artifact(string file = Current, string library = "fontawesome", string variant = "regular")
        => new("kit", "public", library, variant, file);

    private JsonDocument ReadIndex() => JsonDocument.Parse(File.ReadAllText(Path.Combine(_options.Value.CachePath, "index.json")));
    private bool Exists(string name) => File.Exists(Path.Combine(_options.Value.CachePath, name));

    [TestCase(false)]
    [TestCase(true)]
    public async Task Missing_Or_Corrupt_Index_Recovers_And_Deletes_Only_Old_Unreferenced_Files(bool corrupt)
    {
        Write(Old, 20);
        Write(Current, 60);
        Write("kits/manual.svg", 60);
        if (corrupt) File.WriteAllText(Path.Combine(_options.Value.CachePath, "index.json"), "{");
        var lifecycle = Create();
        await TrackAsync(lifecycle, Artifact(), Unchanged, files =>
        {
            Assert.That(files, Is.EquivalentTo(new[] { Old, Current }));
            return new[] { Artifact() };
        });
        await lifecycle.PendingCleanup;
        Assert.That(Exists(Old), Is.False);
        Assert.That(Exists(Current), Is.True, "Current files survive regardless of age.");
        Assert.That(Exists("kits/manual.svg"), Is.True, "Unmanaged files are not deleted.");
        using var index = ReadIndex();
        Assert.That(index.RootElement.GetProperty("currentFiles").GetArrayLength(), Is.EqualTo(1));
        Assert.That(index.RootElement.GetProperty("obsoleteFiles").EnumerateObject(), Is.Empty);
    }

    [Test]
    public async Task Recent_Orphan_Is_Retained_With_Its_File_Timestamp()
    {
        Write(Old, 2);
        Write(Current);
        var lifecycle = Create();
        await TrackAsync(lifecycle, Artifact(), Unchanged, _ => new[] { Artifact() });
        await lifecycle.PendingCleanup;
        Assert.That(Exists(Old), Is.True);
        using var index = ReadIndex();
        var obsoleteSince = index.RootElement.GetProperty("obsoleteFiles").GetProperty(Old).GetDateTimeOffset();
        Assert.That(obsoleteSince, Is.LessThan(DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Test]
    public async Task Newly_Replaced_File_Gets_Full_Grace_Even_When_Created_Long_Ago()
    {
        Write(Old, 60);
        var first = Create();
        await TrackAsync(first, Artifact(Old), Unchanged, _ => new[] { Artifact(Old) });
        await first.PendingCleanup;
        Write(Current);
        var restarted = Create();
        await TrackAsync(restarted, Artifact(), Unchanged, _ => new[] { Artifact() });
        await restarted.PendingCleanup;
        Assert.That(Exists(Old), Is.True);
        using var index = ReadIndex();
        var obsoleteSince = index.RootElement.GetProperty("obsoleteFiles").GetProperty(Old).GetDateTimeOffset();
        Assert.That(obsoleteSince, Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Restart_Uses_Persisted_Expiry_Unless_Revision_Is_Current_Again(bool reactivate)
    {
        Write(Old, 60);
        Write(Current);
        var persisted = new
        {
            schemaVersion = 1,
            currentFiles = new[] { Artifact() },
            obsoleteFiles = new Dictionary<string, DateTimeOffset> { [Old] = DateTimeOffset.UtcNow.AddMinutes(-20) }
        };
        File.WriteAllText(Path.Combine(_options.Value.CachePath, "index.json"), JsonSerializer.Serialize(persisted,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var lifecycle = Create();
        var active = Artifact(reactivate ? Old : Current);
        await TrackAsync(lifecycle, active, Unchanged, _ => new[] { active });
        await lifecycle.PendingCleanup;

        Assert.That(Exists(Old), Is.EqualTo(reactivate));
        using var index = ReadIndex();
        Assert.That(index.RootElement.GetProperty("obsoleteFiles").TryGetProperty(Old, out _), Is.False);
    }

    [Test]
    public async Task Index_Deleted_During_Process_Is_Reconstructed()
    {
        Write(Current);
        var lifecycle = Create();
        await TrackAsync(lifecycle, Artifact(), Unchanged, _ => new[] { Artifact() });
        await lifecycle.PendingCleanup;
        File.Delete(Path.Combine(_options.Value.CachePath, "index.json"));
        Write(Old, 20);
        var recovered = false;
        await TrackAsync(lifecycle, Artifact(), Unchanged, files =>
        {
            recovered = files.Contains(Old);
            return new[] { Artifact() };
        });
        Assert.That(recovered, Is.True);
        using var index = ReadIndex();
        Assert.That(index.RootElement.GetProperty("obsoleteFiles").TryGetProperty(Old, out _), Is.True);
    }

    [Test]
    public async Task Parallel_Selections_And_Unchanged_Registrations_Preserve_Index()
    {
        Write(Old);
        Write(Current);
        var lifecycle = Create();
        var artifacts = new[] { Artifact(), Artifact(Old, "hugeicons", "rounded") };
        await Task.WhenAll(artifacts.Select(x => TrackAsync(lifecycle, x, Unchanged, _ => artifacts)));
        await lifecycle.PendingCleanup;
        var indexPath = Path.Combine(_options.Value.CachePath, "index.json");
        File.SetLastWriteTimeUtc(indexPath, DateTime.UtcNow.AddHours(-1));
        var before = File.GetLastWriteTimeUtc(indexPath);
        await TrackAsync(lifecycle, Artifact(), Unchanged, _ => throw new Exception("Unchanged sources must not be rescanned."));
        Assert.That(File.GetLastWriteTimeUtc(indexPath), Is.EqualTo(before));
        using var index = ReadIndex();
        Assert.That(index.RootElement.GetProperty("currentFiles").GetArrayLength(), Is.EqualTo(2));
        Assert.That(index.RootElement.GetProperty("obsoleteFiles").EnumerateObject(), Is.Empty);
    }

    [Test]
    public void Failed_Recovery_And_Stale_Generation_Never_Delete_Files()
    {
        Write(Old, 60);
        Write(Current);
        var lifecycle = Create();
        Assert.ThrowsAsync<InvalidDataException>(async () => await TrackAsync(lifecycle, Artifact(), Unchanged,
            _ => throw new InvalidDataException("Unavailable sources")));
        using var changes = new CancellationTokenSource();
        var token = new CancellationChangeToken(changes.Token);
        Assert.ThrowsAsync<IOException>(async () => await TrackAsync(lifecycle, Artifact(), token, _ =>
        {
            changes.Cancel();
            return new[] { Artifact() };
        }));
        Assert.That(Exists(Old), Is.True);
        Assert.That(Exists("index.json"), Is.False);
    }

    [Test]
    public void Refresh_Keeps_Unchanged_Watches_To_Avoid_Losing_Late_Notifications()
    {
        Directory.CreateDirectory(Path.Combine(_sources.Root, "hugeicons"));
        File.WriteAllText(Path.Combine(_sources.Root, "hugeicons", "library.json"), """{"variants":{"rounded":{}}}""");
        using var mappingChanged = new CancellationTokenSource();
        using var sourceChanged = new CancellationTokenSource();
        var files = new Mock<IFileSystem>();
        files.Setup(x => x.GetDirectoryContents(It.IsAny<string>())).Returns((string path) => _sources.GetDirectoryContents(path));
        files.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) => _sources.GetFileInfo(path));
        files.Setup(x => x.Watch(It.IsAny<string>())).Returns(NullChangeToken.Singleton);
        files.SetupSequence(x => x.Watch("hugeicons/mapping.json"))
            .Returns(new CancellationChangeToken(mappingChanged.Token)).Returns(NullChangeToken.Singleton);
        files.Setup(x => x.Watch("hugeicons/rounded/icons.zip")).Returns(new CancellationChangeToken(sourceChanged.Token));
        var lifecycle = Create();
        var observed = new List<IconChangeKind>();
        lifecycle.StartWatchingSources(files.Object);
        mappingChanged.Cancel();
        observed.AddRange(lifecycle.CollectChanges().Select(change => change.Kind));

        // This notification arrives after reading the mapping change but before restarting the watches.
        sourceChanged.Cancel();
        var next = lifecycle.StartWatchingSources(files.Object);
        Assert.That(next.HasChanged, Is.True);
        observed.AddRange(lifecycle.CollectChanges().Select(change => change.Kind));

        Assert.That(observed, Is.EqualTo(new[] { IconChangeKind.Mapping, IconChangeKind.VariantSources }));
        files.Verify(x => x.Watch("hugeicons/rounded/icons.zip"), Times.Once);
        files.Verify(x => x.Watch("hugeicons/mapping.json"), Times.Exactly(2));
    }

    [Test]
    public void Changes_Report_Their_Library_And_Variant_Once()
    {
        Directory.CreateDirectory(Path.Combine(_sources.Root, "hugeicons", "rounded"));
        File.WriteAllText(Path.Combine(_sources.Root, "hugeicons", "library.json"), """{"variants":{"rounded":{}}}""");
        var files = new Mock<IFileSystem>();
        files.Setup(x => x.GetDirectoryContents(It.IsAny<string>())).Returns((string path) => _sources.GetDirectoryContents(path));
        files.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) => _sources.GetFileInfo(path));
        using var changes = new CancellationTokenSource();
        files.Setup(x => x.Watch(It.IsAny<string>())).Returns(NullChangeToken.Singleton);
        files.Setup(x => x.Watch("hugeicons/rounded/icons.zip")).Returns(new CancellationChangeToken(changes.Token));
        files.SetupGet(x => x.Root).Returns(_sources.Root);
        _context.SetupGet(x => x.AppDataRoot).Returns(files.Object);
        var lifecycle = new IconCacheLifecycle(_context.Object, _options, Mock.Of<IIconCacheRecovery>());
        _lifecycles.Add(lifecycle);
        var received = new List<IconChange>();
        var snapshot = lifecycle.StartWatchingSources(files.Object);
        changes.Cancel();
        received.AddRange(lifecycle.CollectChanges());
        received.AddRange(lifecycle.CollectChanges());
        Assert.That(snapshot.HasChanged, Is.True);
        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].Kind, Is.EqualTo(IconChangeKind.VariantSources));
        Assert.That(received[0].Library, Is.EqualTo("hugeicons"));
        Assert.That(received[0].Variant, Is.EqualTo("rounded"));
    }
}
