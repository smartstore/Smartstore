using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using EntityState = Smartstore.Data.EntityState;

namespace Smartstore.Tests.Data.Hooks;

/// <summary>
/// Additional red/green coverage for #1508 (context inheritance, void cache,
/// dual registrations, shared-batch fast path and concurrent registry access).
/// No production implementation is changed by these tests.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DbContextHookIsolationExtendedReproTests
{
    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public void Registry_with_two_context_bound_hooks_must_select_only_the_matching_one(HookStage stage)
    {
        using var smart = Smart();
        using var custom = new CustomContext();
        var registry = Registry(
            Metadata(typeof(SmartOnlyHook), typeof(SmartDbContext)),
            Metadata(typeof(CustomOnlyHook), typeof(CustomContext)));

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(Entry(smart), stage).Select(x => x.ImplType),
                Is.EquivalentTo(new[] { typeof(SmartOnlyHook) }));
            Assert.That(registry.SelectHooks(Entry(custom), stage).Select(x => x.ImplType),
                Is.EquivalentTo(new[] { typeof(CustomOnlyHook) }));
        });
    }

    [Test]
    public void Registry_must_accept_derived_context_when_hook_is_bound_to_its_base_type()
    {
        using var derived = new DerivedCustomContext();
        var registry = Registry(Metadata(typeof(CustomOnlyHook), typeof(CustomContext)));
        Assert.That(registry.SelectHooks(Entry(derived), HookStage.PreSave), Has.Length.EqualTo(1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Cache_must_keep_both_context_bindings_independent_regardless_of_first_caller(bool smartFirst)
    {
        using var smart = Smart();
        using var custom = new CustomContext();
        var registry = Registry(
            Metadata(typeof(SmartOnlyHook), typeof(SmartDbContext)),
            Metadata(typeof(CustomOnlyHook), typeof(CustomContext)));
        var first = smartFirst ? Entry(smart) : Entry(custom);
        var second = smartFirst ? Entry(custom) : Entry(smart);

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(first, HookStage.PreSave).Select(x => x.ImplType),
                Is.EquivalentTo(new[] { smartFirst ? typeof(SmartOnlyHook) : typeof(CustomOnlyHook) }));
            Assert.That(registry.SelectHooks(second, HookStage.PreSave).Select(x => x.ImplType),
                Is.EquivalentTo(new[] { smartFirst ? typeof(CustomOnlyHook) : typeof(SmartOnlyHook) }));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Void_registration_must_not_poison_a_different_context_cache(bool smartFirst)
    {
        using var smart = Smart();
        using var custom = new CustomContext();
        var hook = Metadata(typeof(SmartOnlyHook), typeof(DbContext));
        var registry = Registry(hook);
        var first = smartFirst ? Entry(smart) : Entry(custom);
        var second = smartFirst ? Entry(custom) : Entry(smart);

        Assert.That(registry.SelectHooks(first, HookStage.PreSave), Has.Length.EqualTo(1));
        Assert.That(registry.SelectHooks(second, HookStage.PreSave), Has.Length.EqualTo(1));

        registry.RegisterVoidHook(hook, first, HookStage.PreSave);

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(first, HookStage.PreSave), Is.Empty);
            Assert.That(registry.SelectHooks(second, HookStage.PreSave), Has.Length.EqualTo(1),
                "A void result for one context must not suppress an otherwise eligible hook for a different context.");
        });
    }

    [Test]
    public void Other_entity_type_must_not_match_a_product_hook()
    {
        using var custom = new CustomContext();
        var registry = Registry(Metadata(typeof(CustomOnlyHook), typeof(CustomContext)));
        var category = new HookedEntityMock(new Category(), EntityState.Added, custom);
        Assert.That(registry.SelectHooks(category, HookStage.PreSave), Is.Empty);
    }

    [Test]
    public async Task Shared_batch_fast_path_must_not_dispatch_custom_hook_for_smart_entries()
    {
        using var smart = Smart();
        using var custom = new CustomContext();
        CustomOnlyHook.Reset();
        var processor = new DefaultDbHookProcessor(
            Registry(Metadata(typeof(CustomOnlyHook), typeof(CustomContext))),
            new SimpleDbHookActivator());

        // >3 equal entity types/states activates TryGetSharedHooks shortcut.
        // In ordinary SaveChanges all entries have one context, but this public API
        // can receive mixed-context arrays and should not leak hooks.
        var mixed = new[] { Entry(custom), Entry(smart), Entry(smart), Entry(smart) };
        await processor.SavingChangesAsync(mixed);
        Assert.That(CustomOnlyHook.BeforeCalls, Is.EqualTo(1),
            "Only the single CustomContext entry is eligible for the bound hook.");
    }

    [Test]
    public async Task Shared_batch_fast_path_with_single_matching_context_still_handles_all_entries()
    {
        using var custom = new CustomContext();
        CustomOnlyHook.Reset();
        var processor = new DefaultDbHookProcessor(
            Registry(Metadata(typeof(CustomOnlyHook), typeof(CustomContext))),
            new SimpleDbHookActivator());

        await processor.SavingChangesAsync(new[]
        {
            Entry(custom), Entry(custom), Entry(custom), Entry(custom)
        });
        Assert.That(CustomOnlyHook.BeforeCalls, Is.EqualTo(4));
    }

    [Test]
    public void Concurrent_selection_must_not_mix_context_bound_hooks()
    {
        using var smart = Smart();
        using var custom = new CustomContext();
        var registry = Registry(
            Metadata(typeof(SmartOnlyHook), typeof(SmartDbContext)),
            Metadata(typeof(CustomOnlyHook), typeof(CustomContext)));
        var mismatches = 0;

        Parallel.For(0, 80, i =>
        {
            var expected = i % 2 == 0 ? typeof(SmartOnlyHook) : typeof(CustomOnlyHook);
            var context = i % 2 == 0 ? (DbContext)smart : custom;
            var actual = registry.SelectHooks(Entry(context), HookStage.PreSave);
            if (actual.Length != 1 || actual[0].ImplType != expected)
            {
                Interlocked.Increment(ref mismatches);
            }
        });

        Assert.That(mismatches, Is.Zero, "Concurrent access must not cross-contaminate context selection.");
    }

    private static SmartDbContext Smart() => new(new DbContextOptions<SmartDbContext>());
    private static HookedEntityMock Entry(DbContext db) => new(new Product(), EntityState.Added, db);

    private static HookMetadata Metadata(Type implementation, Type contextType) => new()
    {
        HookedType = typeof(Product),
        ImplType = implementation,
        DbContextType = contextType,
        Importance = HookImportance.Normal,
        Order = 0
    };

    private static DefaultDbHookRegistry Registry(params HookMetadata[] hooks) =>
        new(hooks.Select(m => new Lazy<IDbSaveHook, HookMetadata>(
            () => (IDbSaveHook)Activator.CreateInstance(m.ImplType), m)).ToArray());

    public class CustomContext : DbContext
    {
        public CustomContext() : base(new DbContextOptions<CustomContext>()) { }
    }

    public class DerivedCustomContext : CustomContext { }

    public class SmartOnlyHook : DbSaveHook<SmartDbContext, Product>
    {
        protected override HookResult OnInserting(Product entity, IHookedEntity entry) => HookResult.Ok;
    }

    public class CustomOnlyHook : DbSaveHook<CustomContext, Product>
    {
        public static int BeforeCalls;
        public static void Reset() => BeforeCalls = 0;
        protected override HookResult OnInserting(Product entity, IHookedEntity entry)
        {
            Interlocked.Increment(ref BeforeCalls);
            return HookResult.Ok;
        }
    }
}
