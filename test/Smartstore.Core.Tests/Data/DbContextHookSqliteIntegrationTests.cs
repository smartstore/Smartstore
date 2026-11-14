using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using EntityState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Tests.Data;

/// <summary>
/// #1508: Real SQLite persistence combined with the production hook registry/processor.
/// The save-stage calls here are explicit; this does not claim to test dependency-injected
/// HookingDbContext.SaveChanges orchestration or any other database provider.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DbContextHookSqliteIntegrationTests
{
    [SetUp]
    public void SetUp() => MatchingContextHook.Reset();

    [TestCase(EntityState.Added, true)]
    [TestCase(EntityState.Modified, true)]
    [TestCase(EntityState.Deleted, true)]
    [TestCase(EntityState.Added, false)]
    [TestCase(EntityState.Modified, false)]
    [TestCase(EntityState.Deleted, false)]
    public async Task Real_sqlite_persistence_respects_context_binding_in_both_save_stages(
        EntityState state, bool matchingContext)
    {
        await using var sqlite = await OpenDatabase();
        var options = new DbContextOptionsBuilder().UseSqlite(sqlite).Options;
        await using DbContext db = matchingContext
            ? new MatchingContext(options)
            : new UnrelatedContext(options);
        await db.Database.EnsureCreatedAsync();

        var item = new SqliteHookEntity { Name = "original" };
        if (state != EntityState.Added)
        {
            db.Add(item);
            await db.SaveChangesAsync();
            MatchingContextHook.Reset();
        }

        switch (state)
        {
            case EntityState.Added:
                item.Name = "inserted";
                db.Add(item);
                break;
            case EntityState.Modified:
                item.Name = "modified";
                db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                break;
            case EntityState.Deleted:
                db.Remove(item);
                break;
        }

        var entry = new HookedEntity(db.Entry(item));
        var processor = CreateProcessor();
        await processor.SavingChangesAsync(new IHookedEntity[] { entry });
        await db.SaveChangesAsync();
        await processor.SavedChangesAsync(new IHookedEntity[] { entry });

        await using var verifier = new MatchingContext(options);
        var rows = await verifier.Records.AsNoTracking().ToListAsync();
        var expectedCalls = matchingContext ? 2 : 0;

        Assert.Multiple(() =>
        {
            Assert.That(MatchingContextHook.BeforeCalls, Is.EqualTo(expectedCalls / 2),
                "A context-specific pre-save hook must not run in an unrelated DbContext.");
            Assert.That(MatchingContextHook.AfterCalls, Is.EqualTo(expectedCalls / 2),
                "A context-specific post-save hook must not run in an unrelated DbContext.");
            Assert.That(rows.Count, Is.EqualTo(state == EntityState.Deleted ? 0 : 1),
                "The database transaction must persist the requested state.");
            if (state != EntityState.Deleted)
            {
                Assert.That(rows.Single().Name,
                    Is.EqualTo(state == EntityState.Added ? "inserted" : "modified"));
            }
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Real_sqlite_back_to_back_context_transactions_do_not_poison_shared_registry_cache(bool matchingFirst)
    {
        await using var sqlite = await OpenDatabase();
        var options = new DbContextOptionsBuilder().UseSqlite(sqlite).Options;
        await using var schemaContext = new MatchingContext(options);
        await schemaContext.Database.EnsureCreatedAsync();

        var processor = CreateProcessor();
        var flags = matchingFirst ? new[] { true, false } : new[] { false, true };

        foreach (var matching in flags)
        {
            await using DbContext context = matching
                ? new MatchingContext(options)
                : new UnrelatedContext(options);
            var item = new SqliteHookEntity { Name = matching ? "matching" : "unrelated" };
            context.Add(item);
            var entry = new HookedEntity(context.Entry(item));
            await processor.SavingChangesAsync(new IHookedEntity[] { entry });
            await context.SaveChangesAsync();
            await processor.SavedChangesAsync(new IHookedEntity[] { entry });
        }

        await using var verifier = new MatchingContext(options);
        var names = await verifier.Records.AsNoTracking().Select(x => x.Name).ToArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EquivalentTo(new[] { "matching", "unrelated" }),
                "Both contexts must successfully persist real SQLite rows.");
            Assert.That(MatchingContextHook.BeforeCalls, Is.EqualTo(1));
            Assert.That(MatchingContextHook.AfterCalls, Is.EqualTo(1));
        });
    }

    private static async Task<SqliteConnection> OpenDatabase()
    {
        var sqlite = new SqliteConnection("Data Source=:memory:");
        await sqlite.OpenAsync();
        return sqlite;
    }

    private static DefaultDbHookProcessor CreateProcessor()
    {
        var metadata = new HookMetadata
        {
            HookedType = typeof(SqliteHookEntity),
            ImplType = typeof(MatchingContextHook),
            DbContextType = typeof(MatchingContext),
            Importance = HookImportance.Normal
        };
        var registry = new DefaultDbHookRegistry(new[]
        {
            new Lazy<IDbSaveHook, HookMetadata>(() => new MatchingContextHook(), metadata)
        });
        return new DefaultDbHookProcessor(registry, new IntegrationActivator());
    }

    public sealed class SqliteHookEntity : BaseEntity
    {
        public string Name { get; set; }
    }

    public class MatchingContext : DbContext
    {
        public MatchingContext(DbContextOptions options) : base(options) { }
        public DbSet<SqliteHookEntity> Records => Set<SqliteHookEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SqliteHookEntity>().ToTable("HookTestRecords");
            modelBuilder.Entity<SqliteHookEntity>().Property(x => x.Name).IsRequired();
        }
    }

    public class UnrelatedContext : DbContext
    {
        public UnrelatedContext(DbContextOptions options) : base(options) { }
        public DbSet<SqliteHookEntity> Records => Set<SqliteHookEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SqliteHookEntity>().ToTable("HookTestRecords");
            modelBuilder.Entity<SqliteHookEntity>().Property(x => x.Name).IsRequired();
        }
    }

    public class MatchingContextHook : DbSaveHook<MatchingContext, SqliteHookEntity>
    {
        private static int _before;
        private static int _after;
        public static int BeforeCalls => Volatile.Read(ref _before);
        public static int AfterCalls => Volatile.Read(ref _after);

        public static void Reset()
        {
            Interlocked.Exchange(ref _before, 0);
            Interlocked.Exchange(ref _after, 0);
        }

        protected override HookResult OnInserting(SqliteHookEntity entity, IHookedEntity entry) => Before();
        protected override HookResult OnUpdating(SqliteHookEntity entity, IHookedEntity entry) => Before();
        protected override HookResult OnDeleting(SqliteHookEntity entity, IHookedEntity entry) => Before();
        protected override HookResult OnInserted(SqliteHookEntity entity, IHookedEntity entry) => After();
        protected override HookResult OnUpdated(SqliteHookEntity entity, IHookedEntity entry) => After();
        protected override HookResult OnDeleted(SqliteHookEntity entity, IHookedEntity entry) => After();

        private static HookResult Before()
        {
            Interlocked.Increment(ref _before);
            return HookResult.Ok;
        }

        private static HookResult After()
        {
            Interlocked.Increment(ref _after);
            return HookResult.Ok;
        }
    }

    private sealed class IntegrationActivator : IDbHookActivator
    {
        private readonly IDbSaveHook _hook = new MatchingContextHook();
        public IDbSaveHook Activate(HookMetadata hook) => _hook;
    }
}
