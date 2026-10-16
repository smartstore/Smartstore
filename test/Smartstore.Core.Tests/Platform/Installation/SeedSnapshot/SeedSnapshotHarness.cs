using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Smartstore.Caching;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Installation;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Tests.Content.Media.Storage;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests.Platform.Installation.SeedSnapshot;

/// <summary>
/// Runs the real <see cref="InstallationDataSeeder"/> for one culture against an isolated
/// in-memory SQLite database, so that the seeded data can be snapshotted.
/// </summary>
/// <remarks>
/// Only the seed data itself is in scope. Locale resources and message templates are skipped,
/// modules are not installed and no DB hooks run.
/// </remarks>
internal sealed class SeedSnapshotHarness : IDisposable
{
    public const string AdminEmail = "admin@seed-snapshot.test";

    private readonly string _contentRoot;
    private readonly IEngine _previousEngine;
    private readonly IFileSystem _previousCommonContentRoot;
    private readonly IContainer _container;

    private SeedSnapshotHarness(string sampleMediaSourceDir)
    {
        _previousEngine = EngineContext.Current;
        _previousCommonContentRoot = CommonHelper.ContentRoot;

        // Isolated content root: only the sample media is needed. Without "App_Data/Localization"
        // the seeder skips locale resources and never touches the DB migrator.
        _contentRoot = Path.Combine(Path.GetTempPath(), "SmartstoreSeedSnapshot", Guid.NewGuid().ToString("N"));
        CopyDirectory(sampleMediaSourceDir, Path.Combine(_contentRoot, "App_Data", "Samples"));

        var contentRootFs = new LocalFileSystem(_contentRoot);
        var appDataFs = new LocalFileSystem(Path.Combine(_contentRoot, "App_Data"));

        var builder = new ContainerBuilder();
        builder.RegisterInstance(new HttpContextAccessor()).As<IHttpContextAccessor>();
        builder.RegisterInstance(new Mock<IRouteHelper>().Object).As<IRouteHelper>();
        builder.RegisterInstance(new MediaTypeResolver(NullCache.Instance, new MediaSettings())).As<IMediaTypeResolver>();
        builder.RegisterInstance<Func<IMediaStorageProvider>>(() => new TestDatabaseMediaStorageProvider());
        _container = builder.Build();

        var typeScanner = new DefaultTypeScanner(typeof(IEngine).Assembly, typeof(SmartDbContext).Assembly);

        var appContextMock = new Mock<IApplicationContext>();
        appContextMock.SetupGet(x => x.ContentRoot).Returns(contentRootFs);
        appContextMock.SetupGet(x => x.AppDataRoot).Returns(appDataFs);
        appContextMock.SetupGet(x => x.TypeScanner).Returns(typeScanner);
        appContextMock.SetupGet(x => x.Services).Returns(_container);
        appContextMock.SetupGet(x => x.IsInstalled).Returns(false);
        AppContext = appContextMock.Object;

        var scopeAccessorMock = new Mock<ILifetimeScopeAccessor>();
        scopeAccessorMock.SetupGet(x => x.LifetimeScope).Returns(_container);

        EngineContext.Replace(new SnapshotEngine
        {
            Application = AppContext,
            Scope = new ScopedServiceContainer(scopeAccessorMock.Object, _container.Resolve<IHttpContextAccessor>(), _container)
        });

        // A private in-memory SQLite database. It lives as long as the context keeps its connection open.
        const string connectionString = "Data Source=:memory:";
        var dbFactory = CreateSqliteDbFactory();

        DataSettings.Instance = new DataSettings
        {
            AppVersion = SmartstoreVersion.Version,
            ConnectionString = connectionString,
            TenantName = "Default",
            DbFactory = dbFactory
        };
        DataSettings.SetTestMode(true);

        var options = new DbContextOptionsBuilder<SmartDbContext>()
            .UseDbFactory(dbFactory, connectionString, b => b.AddModelAssemblies([typeof(SmartDbContext).Assembly]))
            .Options;

        Db = new SmartDbContext((DbContextOptions<SmartDbContext>)options);
        Db.Database.OpenConnection();
        Db.Database.EnsureCreated();
    }

    public IApplicationContext AppContext { get; }

    public SmartDbContext Db { get; }

    /// <summary>
    /// Seeds a fresh database with the given seed data and returns the harness holding it.
    /// </summary>
    public static async Task<SeedSnapshotHarness> SeedAsync(
        string sampleMediaSourceDir,
        InvariantSeedData data,
        string culture,
        string languageName,
        string uniqueSeoCode,
        string flagImageFileName)
    {
        var harness = new SeedSnapshotHarness(sampleMediaSourceDir);

        try
        {
            var configuration = new SeedDataConfiguration
            {
                DefaultUserName = AdminEmail,
                DefaultUserPassword = Guid.NewGuid().ToString("N"),
                SeedSampleData = true,
                StoreMediaInDB = true,
                Data = data,
                Language = new Language
                {
                    Name = languageName,
                    LanguageCulture = culture,
                    UniqueSeoCode = uniqueSeoCode,
                    FlagImageFileName = flagImageFileName
                }
            };

            // The migrator is only needed for pending locale resources, which are out of scope.
            var migrator = (DbMigrator<SmartDbContext>)RuntimeHelpers.GetUninitializedObject(typeof(DbMigrator<SmartDbContext>));
            var templateService = new Mock<IMessageTemplateService>().Object;

            var seeder = new InstallationDataSeeder(harness.AppContext, migrator, templateService, configuration, NullLogger.Instance);
            await seeder.SeedAsync(harness.Db);
        }
        catch
        {
            harness.Dispose();
            throw;
        }

        return harness;
    }

    public void Dispose()
    {
        Db.Dispose();
        _container.Dispose();

        EngineContext.Replace(_previousEngine);
        // Same cleanup as ServiceTestBase: force a reload on next access.
        DataSettings.Reload();
        CommonHelper.ContentRoot = _previousCommonContentRoot;

        try
        {
            Directory.Delete(_contentRoot, true);
        }
        catch
        {
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            File.Copy(file, dest);
        }
    }

    private sealed class SnapshotEngine : IEngine
    {
        public IApplicationContext Application { get; set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsStarted { get; set; } = true;
        public bool IsInitialized { get; set; } = true;

        public IEngineStarter Start(IApplicationContext application)
            => throw new NotSupportedException();
    }

    private static DbFactory CreateSqliteDbFactory()
    {
        // The factory type is internal. Production code loads it the same way (see DbFactory.Load).
        var assembly = Assembly.Load("Smartstore.Data.Sqlite");
        var factoryType = assembly.GetTypes().Single(x => typeof(DbFactory).IsAssignableFrom(x) && !x.IsAbstract);
        return (DbFactory)Activator.CreateInstance(factoryType, nonPublic: true);
    }
}
