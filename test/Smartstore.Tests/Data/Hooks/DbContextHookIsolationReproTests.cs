using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using EntityState = Smartstore.Data.EntityState;

namespace Smartstore.Tests.Data.Hooks;

/// <summary>
/// Reproduction of https://github.com/smartstore/Smartstore/issues/1508.
/// These deliberately fail on the untouched upstream registry until context isolation is fixed.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DbContextHookIsolationReproTests
{
    [SetUp]
    public void ResetCounters() => CountingCustomContextHook.Reset();

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public void Registry_must_not_select_custom_context_hook_for_SmartDbContext(HookStage stage)
    {
        using var smart = CreateSmartContext();
        var registry = CreateRegistry(typeof(CustomDbContext));
        var selected = registry.SelectHooks(Entry(smart), stage);
        Assert.That(selected, Is.Empty,
            "A hook declared for CustomDbContext cannot run for SmartDbContext.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Processor_must_not_invoke_custom_context_hook_for_SmartDbContext(bool afterSave)
    {
        using var smart = CreateSmartContext();
        var registry = CreateRegistry(typeof(CustomDbContext));
        var processor = new DefaultDbHookProcessor(registry, new SimpleDbHookActivator());
        var entries = new[] { Entry(smart) };

        if (afterSave)
        {
            await processor.SavedChangesAsync(entries);
            Assert.That(CountingCustomContextHook.AfterCalls, Is.Zero);
        }
        else
        {
            await processor.SavingChangesAsync(entries);
            Assert.That(CountingCustomContextHook.BeforeCalls, Is.Zero);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Cache_must_keep_contexts_separate_in_both_access_orders(bool matchingContextFirst)
    {
        using var smart = CreateSmartContext();
        using var custom = new CustomDbContext();
        var registry = CreateRegistry(typeof(CustomDbContext));

        var first = matchingContextFirst ? Entry(custom) : Entry(smart);
        var second = matchingContextFirst ? Entry(smart) : Entry(custom);

        var firstExpected = matchingContextFirst ? 1 : 0;
        var secondExpected = matchingContextFirst ? 0 : 1;

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(first, HookStage.PreSave), Has.Length.EqualTo(firstExpected));
            Assert.That(registry.SelectHooks(second, HookStage.PreSave), Has.Length.EqualTo(secondExpected));
        });
    }

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public void Registry_must_select_matching_custom_context_hook(HookStage stage)
    {
        using var custom = new CustomDbContext();
        var selected = CreateRegistry(typeof(CustomDbContext)).SelectHooks(Entry(custom), stage);
        Assert.That(selected, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Processor_invokes_matching_context_hook_before_and_after_save()
    {
        using var custom = new CustomDbContext();
        var processor = new DefaultDbHookProcessor(CreateRegistry(typeof(CustomDbContext)),
            new SimpleDbHookActivator());
        var entries = new[] { Entry(custom) };

        await processor.SavingChangesAsync(entries);
        await processor.SavedChangesAsync(entries);

        Assert.Multiple(() =>
        {
            Assert.That(CountingCustomContextHook.BeforeCalls, Is.EqualTo(1));
            Assert.That(CountingCustomContextHook.AfterCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void A_hook_bound_to_DbContext_base_type_must_match_both_contexts()
    {
        using var smart = CreateSmartContext();
        using var custom = new CustomDbContext();
        var registry = CreateRegistry(typeof(DbContext));

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(Entry(smart), HookStage.PreSave), Has.Length.EqualTo(1));
            Assert.That(registry.SelectHooks(Entry(custom), HookStage.PreSave), Has.Length.EqualTo(1));
        });
    }

    private static SmartDbContext CreateSmartContext() => new(new DbContextOptions<SmartDbContext>());
    private static HookedEntityMock Entry(DbContext db) => new(new Product(), EntityState.Added, db);

    private static DefaultDbHookRegistry CreateRegistry(Type contextType)
    {
        var metadata = new HookMetadata
        {
            HookedType = typeof(Product),
            ImplType = typeof(CountingCustomContextHook),
            DbContextType = contextType,
            Importance = HookImportance.Normal,
            Order = 0
        };

        return new DefaultDbHookRegistry(
            new[] { new Lazy<IDbSaveHook, HookMetadata>(() => new CountingCustomContextHook(), metadata) });
    }

    public class CustomDbContext : DbContext
    {
        public CustomDbContext() : base(new DbContextOptions<CustomDbContext>()) { }
    }

    public class CountingCustomContextHook : DbSaveHook<CustomDbContext, Product>
    {
        public static int BeforeCalls;
        public static int AfterCalls;

        public static void Reset()
        {
            BeforeCalls = 0;
            AfterCalls = 0;
        }

        protected override HookResult OnInserting(Product product, IHookedEntity entry)
        {
            Interlocked.Increment(ref BeforeCalls);
            return HookResult.Ok;
        }

        protected override HookResult OnInserted(Product product, IHookedEntity entry)
        {
            Interlocked.Increment(ref AfterCalls);
            return HookResult.Ok;
        }
    }
}
