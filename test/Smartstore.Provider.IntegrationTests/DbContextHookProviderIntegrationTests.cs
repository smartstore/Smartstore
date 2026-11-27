using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using EntityState = Smartstore.Data.EntityState;

namespace Smartstore.Provider.IntegrationTests;

/// <summary>
/// Opt-in live-provider validation for smartstore/Smartstore #1508.
/// Requires ONLY disposable QA databases. Connection strings are supplied through
/// SS1508_{POSTGRESQL|MYSQL|SQLSERVER}_CONNECTION environment variables.
/// SQLite uses a private in-memory relational database and needs no configuration.
/// All tests run the production DefaultDbHookRegistry and DefaultDbHookProcessor
/// before/after an actual EF Core SaveChangesAsync. They do not claim to exercise
/// the separate hosted HookingDbContext/EngineContext DI registration pipeline.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DbContextHookProviderIntegrationTests
{
    private static readonly string[] Providers = ["Sqlite", "PostgreSql", "MySql", "SqlServer"];

    public static IEnumerable<TestCaseData> MutationCases
    {
        get
        {
            foreach (var provider in Providers)
            foreach (var state in new[] { EntityState.Added, EntityState.Modified, EntityState.Deleted })
            foreach (var matchingContext in new[] { true, false })
            {
                yield return new TestCaseData(provider, state, matchingContext)
                    .SetName($"{provider}_{state}_{(matchingContext ? "Matching" : "Unrelated")}_RealSave");
            }
        }
    }

    public static IEnumerable<TestCaseData> CacheCases
    {
        get
        {
            foreach (var provider in Providers)
            foreach (var matchingFirst in new[] { true, false })
            {
                yield return new TestCaseData(provider, matchingFirst)
                    .SetName($"{provider}_SharedRegistryCache_MatchingFirst_{matchingFirst}");
            }
        }
    }

    public static IEnumerable<TestCaseData> ProviderCases
    {
        get
        {
            foreach (var provider in Providers)
            {
                yield return new TestCaseData(provider);
            }
        }
    }

    [SetUp]
    public void ResetCounters() => RecordingSaveHook.Reset();

    [TestCaseSource(nameof(MutationCases))]
    public async Task Persisted_row_and_both_save_stages_respect_DbContext_type(
        string provider, EntityState state, bool matching)
    {
        await using var sqlite = new SqliteConnection("Data Source=:memory:");
        if (provider == "Sqlite")
        {
            await sqlite.OpenAsync();
        }
        var options = Options(provider, sqlite);
        await Prepare(options);

        await using DbContext db = matching
            ? new MatchingDb(options)
            : new UnrelatedDb(options);

        var entity = new SavedRow { Name = "seed" };
        if (state != EntityState.Added)
        {
            db.Add(entity);
            await db.SaveChangesAsync();
        }

        switch (state)
        {
            case EntityState.Added:
                entity.Name = "new";
                db.Add(entity);
                break;
            case EntityState.Modified:
                entity.Name = "updated";
                db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                break;
            case EntityState.Deleted:
                db.Remove(entity);
                break;
        }

        var hooked = new HookedEntity(db.Entry(entity));
        var processor = Processor();
        await processor.SavingChangesAsync([hooked]);
        await db.SaveChangesAsync();
        await processor.SavedChangesAsync([hooked]);

        await using var checker = new MatchingDb(options);
        var actual = await checker.Rows.AsNoTracking().Select(x => x.Name).ToArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(RecordingSaveHook.Before, Is.EqualTo(matching ? 1 : 0));
            Assert.That(RecordingSaveHook.After, Is.EqualTo(matching ? 1 : 0));
            Assert.That(actual.Length, Is.EqualTo(state == EntityState.Deleted ? 0 : 1));
            if (state != EntityState.Deleted)
            {
                var name = state == EntityState.Added ? "new" : "updated";
                Assert.That(actual.Single(), Is.EqualTo(matching ? "hook-" + name : name));
            }
        });
    }

    [TestCaseSource(nameof(CacheCases))]
    public async Task Shared_hook_cache_never_leaks_between_real_contexts(string provider, bool matchingFirst)
    {
        await using var sqlite = new SqliteConnection("Data Source=:memory:");
        if (provider == "Sqlite") await sqlite.OpenAsync();
        var options = Options(provider, sqlite);
        await Prepare(options);

        var processor = Processor();
        foreach (var matching in matchingFirst ? new[] { true, false } : new[] { false, true })
        {
            await using DbContext db = matching ? new MatchingDb(options) : new UnrelatedDb(options);
            var entity = new SavedRow { Name = matching ? "matching" : "other" };
            db.Add(entity);
            var hooked = new HookedEntity(db.Entry(entity));
            await processor.SavingChangesAsync([hooked]);
            await db.SaveChangesAsync();
            await processor.SavedChangesAsync([hooked]);
        }

        await using var checker = new MatchingDb(options);
        var names = await checker.Rows.AsNoTracking().Select(x => x.Name).ToArrayAsync();
        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EquivalentTo(new[] { "hook-matching", "other" }));
            Assert.That(RecordingSaveHook.Before, Is.EqualTo(1));
            Assert.That(RecordingSaveHook.After, Is.EqualTo(1));
        });
    }

    [TestCaseSource(nameof(ProviderCases))]
    public async Task Derived_context_type_invokes_base_context_hook_and_persists_real_row(string provider)
    {
        await using var sqlite = new SqliteConnection("Data Source=:memory:");
        if (provider == "Sqlite") await sqlite.OpenAsync();
        var options = Options(provider, sqlite);
        await Prepare(options);

        await using var db = new DerivedMatchingDb(options);
        var entity = new SavedRow { Name = "derived" };
        db.Add(entity);
        var hooked = new HookedEntity(db.Entry(entity));
        var processor = Processor();
        await processor.SavingChangesAsync([hooked]);
        await db.SaveChangesAsync();
        await processor.SavedChangesAsync([hooked]);

        await using var checker = new MatchingDb(options);
        Assert.Multiple(() =>
        {
            Assert.That(checker.Rows.Single().Name, Is.EqualTo("hook-derived"));
            Assert.That(RecordingSaveHook.Before, Is.EqualTo(1));
            Assert.That(RecordingSaveHook.After, Is.EqualTo(1));
        });
    }

    [TestCaseSource(nameof(ProviderCases))]
    public async Task Real_transaction_rollback_preserves_previous_row_value(string provider)
    {
        await using var sqlite = new SqliteConnection("Data Source=:memory:");
        if (provider == "Sqlite") await sqlite.OpenAsync();
        var options = Options(provider, sqlite);
        await Prepare(options);

        await using (var db = new MatchingDb(options))
        {
            db.Rows.Add(new SavedRow { Name = "committed" });
            await db.SaveChangesAsync();
        }

        await using (var db = new MatchingDb(options))
        {
            var row = await db.Rows.SingleAsync();
            await using var trx = await db.Database.BeginTransactionAsync();
            row.Name = "tentative";
            var hooked = new HookedEntity(db.Entry(row));
            var processor = Processor();
            await processor.SavingChangesAsync([hooked]);
            await db.SaveChangesAsync();
            await trx.RollbackAsync();
            // Do not run post-commit callbacks after this explicit rollback.
        }

        await using var checker = new MatchingDb(options);
        Assert.Multiple(() =>
        {
            Assert.That(checker.Rows.Single().Name, Is.EqualTo("committed"));
            Assert.That(RecordingSaveHook.Before, Is.EqualTo(1));
            Assert.That(RecordingSaveHook.After, Is.Zero);
        });
    }

    private static DbContextOptions Options(string provider, SqliteConnection sqlite)
    {
        var builder = new DbContextOptionsBuilder();
        switch (provider)
        {
            case "Sqlite":
                builder.UseSqlite(sqlite);
                break;
            case "PostgreSql":
                builder.UseNpgsql(RequireConnection("POSTGRESQL"));
                break;
            case "MySql":
                builder.UseMySql(RequireConnection("MYSQL"), ServerVersion.Parse("8.4.0-mysql"));
                break;
            case "SqlServer":
                builder.UseSqlServer(RequireConnection("SQLSERVER"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported QA provider");
        }
        return builder.Options;
    }

    private static string RequireConnection(string provider)
    {
        var value = Environment.GetEnvironmentVariable("SS1508_" + provider + "_CONNECTION");
        if (string.IsNullOrWhiteSpace(value))
        {
            Assert.Ignore("Provider test is opt-in: set SS1508_" + provider + "_CONNECTION against a disposable QA database.");
        }
        return value!;
    }

    private static async Task Prepare(DbContextOptions options)
    {
        await using var setup = new MatchingDb(options);
        await setup.Database.EnsureCreatedAsync();
        var rows = await setup.Rows.ToListAsync();
        if (rows.Count > 0)
        {
            setup.RemoveRange(rows);
            await setup.SaveChangesAsync();
        }
    }

    private static DefaultDbHookProcessor Processor()
    {
        var metadata = new HookMetadata
        {
            ImplType = typeof(RecordingSaveHook),
            DbContextType = typeof(MatchingDb),
            HookedType = typeof(SavedRow),
            Importance = HookImportance.Normal
        };
        var registry = new DefaultDbHookRegistry(new[]
        {
            new Lazy<IDbSaveHook, HookMetadata>(() => new RecordingSaveHook(), metadata)
        });
        return new DefaultDbHookProcessor(registry, new RecordingHookActivator());
    }

    public sealed class SavedRow : BaseEntity
    {
        public string Name { get; set; } = "";
    }

    public class MatchingDb : DbContext
    {
        public MatchingDb(DbContextOptions options) : base(options) { }
        public DbSet<SavedRow> Rows => Set<SavedRow>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<SavedRow>().ToTable("Issue1508HookRows");
            builder.Entity<SavedRow>().Property(x => x.Name).IsRequired().HasMaxLength(150);
        }
    }

    public sealed class DerivedMatchingDb : MatchingDb
    {
        public DerivedMatchingDb(DbContextOptions options) : base(options) { }
    }

    public sealed class UnrelatedDb : DbContext
    {
        public UnrelatedDb(DbContextOptions options) : base(options) { }
        public DbSet<SavedRow> Rows => Set<SavedRow>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<SavedRow>().ToTable("Issue1508HookRows");
            builder.Entity<SavedRow>().Property(x => x.Name).IsRequired().HasMaxLength(150);
        }
    }

    public sealed class RecordingSaveHook : DbSaveHook<MatchingDb, SavedRow>
    {
        private static int _before;
        private static int _after;
        public static int Before => Volatile.Read(ref _before);
        public static int After => Volatile.Read(ref _after);
        public static void Reset()
        {
            Interlocked.Exchange(ref _before, 0);
            Interlocked.Exchange(ref _after, 0);
        }

        private static HookResult BeforeWrite(SavedRow entity, bool modify)
        {
            Interlocked.Increment(ref _before);
            if (modify) entity.Name = "hook-" + entity.Name;
            return HookResult.Ok;
        }

        private static HookResult AfterWrite()
        {
            Interlocked.Increment(ref _after);
            return HookResult.Ok;
        }

        protected override HookResult OnInserting(SavedRow entity, IHookedEntity entry) => BeforeWrite(entity, true);
        protected override HookResult OnUpdating(SavedRow entity, IHookedEntity entry) => BeforeWrite(entity, true);
        protected override HookResult OnDeleting(SavedRow entity, IHookedEntity entry) => BeforeWrite(entity, false);
        protected override HookResult OnInserted(SavedRow entity, IHookedEntity entry) => AfterWrite();
        protected override HookResult OnUpdated(SavedRow entity, IHookedEntity entry) => AfterWrite();
        protected override HookResult OnDeleted(SavedRow entity, IHookedEntity entry) => AfterWrite();
    }

    private sealed class RecordingHookActivator : IDbHookActivator
    {
        private readonly IDbSaveHook _instance = new RecordingSaveHook();
        public IDbSaveHook Activate(HookMetadata metadata) => _instance;
    }
}
