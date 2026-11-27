using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
/// Regression matrix for #1508: state/stage dispatch, importance filtering,
/// cache scoping, mixed batches, inherited contexts and concurrent selection.
/// No database connection is made: the registry/processor operate on hook entries.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DbContextHookIsolationRegressionMatrixTests
{
    [SetUp]
    public void Reset()
    {
        RecordingHook<SmartDbContext>.Clear();
        RecordingHook<SecondaryContext>.Clear();
    }

    [TestCase(EntityState.Added, HookStage.PreSave)]
    [TestCase(EntityState.Modified, HookStage.PreSave)]
    [TestCase(EntityState.Deleted, HookStage.PreSave)]
    [TestCase(EntityState.Added, HookStage.PostSave)]
    [TestCase(EntityState.Modified, HookStage.PostSave)]
    [TestCase(EntityState.Deleted, HookStage.PostSave)]
    public async Task All_mutation_states_and_stages_dispatch_only_to_the_matching_context(
        EntityState state, HookStage stage)
    {
        using var smart = Smart();
        using var secondary = new SecondaryContext();
        var processor = Processor(
            Meta(typeof(RecordingHook<SmartDbContext>), typeof(SmartDbContext)),
            Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));
        var entries = new[] { Entry(smart, state), Entry(secondary, state) };

        await Dispatch(processor, stage, entries);

        var suffix = state + ":" + (stage == HookStage.PreSave ? "Pre" : "Post");
        Assert.Multiple(() =>
        {
            Assert.That(RecordingHook<SmartDbContext>.Snapshot(), Is.EqualTo(new[] { "RecordingHook\u00601:" + suffix }));
            Assert.That(RecordingHook<SecondaryContext>.Snapshot(), Is.EqualTo(new[] { "RecordingHook\u00601:" + suffix }));
        });
    }

    [TestCase(EntityState.Added, HookStage.PreSave)]
    [TestCase(EntityState.Modified, HookStage.PreSave)]
    [TestCase(EntityState.Deleted, HookStage.PreSave)]
    [TestCase(EntityState.Added, HookStage.PostSave)]
    [TestCase(EntityState.Modified, HookStage.PostSave)]
    [TestCase(EntityState.Deleted, HookStage.PostSave)]
    public async Task Mismatched_context_never_dispatches_any_mutation_callback(
        EntityState state, HookStage stage)
    {
        using var smart = Smart();
        var processor = Processor(Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));

        await Dispatch(processor, stage, new[] { Entry(smart, state) });

        Assert.That(RecordingHook<SecondaryContext>.Snapshot(), Is.Empty);
    }

    [TestCase(HookStage.PreSave, HookImportance.Normal, 3)]
    [TestCase(HookStage.PreSave, HookImportance.Important, 2)]
    [TestCase(HookStage.PreSave, HookImportance.Essential, 1)]
    [TestCase(HookStage.PostSave, HookImportance.Normal, 3)]
    [TestCase(HookStage.PostSave, HookImportance.Important, 2)]
    [TestCase(HookStage.PostSave, HookImportance.Essential, 1)]
    public async Task Importance_thresholds_filter_only_eligible_context_hooks_in_both_stages(
        HookStage stage, HookImportance minimum, int expectedCount)
    {
        using var smart = Smart();
        using var secondary = new SecondaryContext();
        var metadata = ImportanceMetadata();
        var registry = Registry(metadata);
        var selected = registry.SelectHooks(Entry(secondary), stage, minimum);
        var foreign = registry.SelectHooks(Entry(smart), stage, minimum);
        var processor = new DefaultDbHookProcessor(registry, new SimpleDbHookActivator());

        await Dispatch(processor, stage, new[] { Entry(secondary) }, minimum);

        var expectedNames = new[] { typeof(NormalHook).Name, typeof(ImportantHook).Name, typeof(EssentialHook).Name }
            .Skip(3 - expectedCount).ToArray();
        var expectedStage = stage == HookStage.PreSave ? "Pre" : "Post";
        Assert.Multiple(() =>
        {
            Assert.That(selected.Select(x => x.ImplType.Name), Is.EquivalentTo(expectedNames));
            Assert.That(foreign, Is.Empty);
            Assert.That(RecordingHook<SecondaryContext>.Snapshot(),
                Is.EquivalentTo(expectedNames.Select(x => x + ":Added:" + expectedStage)));
            Assert.That(RecordingHook<SmartDbContext>.Snapshot(), Is.Empty);
        });
    }

    [Test]
    public void Importance_filter_result_must_not_poison_subsequent_lower_threshold_cache_lookup()
    {
        using var secondary = new SecondaryContext();
        var registry = Registry(ImportanceMetadata());
        var entry = Entry(secondary);
        var criticalFirst = registry.SelectHooks(entry, HookStage.PreSave, HookImportance.Essential);
        var normalLater = registry.SelectHooks(entry, HookStage.PreSave, HookImportance.Normal);
        var importantLater = registry.SelectHooks(entry, HookStage.PreSave, HookImportance.Important);

        Assert.Multiple(() =>
        {
            Assert.That(criticalFirst, Has.Length.EqualTo(1));
            Assert.That(normalLater, Has.Length.EqualTo(3));
            Assert.That(importantLater, Has.Length.EqualTo(2));
        });
    }

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public void Registry_respects_order_for_context_matching_hooks_only(HookStage stage)
    {
        using var secondary = new SecondaryContext();
        using var smart = Smart();
        var normal = Meta(typeof(NormalHook), typeof(SecondaryContext), order: 30);
        var important = Meta(typeof(ImportantHook), typeof(SecondaryContext),
            HookImportance.Important, order: 10);
        var essential = Meta(typeof(EssentialHook), typeof(SecondaryContext),
            HookImportance.Essential, order: 20);
        var foreign = Meta(typeof(RecordingHook<SmartDbContext>), typeof(SmartDbContext), order: 0);
        var registry = Registry(normal, important, essential, foreign);

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(Entry(secondary), stage).Select(x => x.ImplType),
                Is.EqualTo(new[] { typeof(ImportantHook), typeof(EssentialHook), typeof(NormalHook) }));
            Assert.That(registry.SelectHooks(Entry(smart), stage).Select(x => x.ImplType),
                Is.EqualTo(new[] { typeof(RecordingHook<SmartDbContext>) }));
        });
    }

    [TestCase(EntityState.Added)]
    [TestCase(EntityState.Modified)]
    [TestCase(EntityState.Deleted)]
    public void Void_cache_is_isolated_by_context_state_and_stage(EntityState state)
    {
        using var secondary = new SecondaryContext();
        using var smart = Smart();
        var hook = Meta(typeof(RecordingHook<SecondaryContext>), typeof(DbContext));
        var registry = Registry(hook);
        var selected = Entry(secondary, state);
        var otherState = state == EntityState.Added ? EntityState.Modified : EntityState.Added;

        Assert.That(registry.SelectHooks(selected, HookStage.PreSave), Has.Length.EqualTo(1));
        registry.RegisterVoidHook(hook, selected, HookStage.PreSave);

        Assert.Multiple(() =>
        {
            Assert.That(registry.SelectHooks(selected, HookStage.PreSave), Is.Empty);
            Assert.That(registry.SelectHooks(Entry(secondary, otherState), HookStage.PreSave), Has.Length.EqualTo(1));
            Assert.That(registry.SelectHooks(selected, HookStage.PostSave), Has.Length.EqualTo(1));
            Assert.That(registry.SelectHooks(Entry(smart, state), HookStage.PreSave), Has.Length.EqualTo(1));
        });
    }

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public async Task Mixed_context_and_state_batches_must_not_use_a_cross_context_shared_fast_path(HookStage stage)
    {
        using var smart = Smart();
        using var secondary = new SecondaryContext();
        var processor = Processor(
            Meta(typeof(RecordingHook<SmartDbContext>), typeof(SmartDbContext)),
            Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));
        var entries = new[]
        {
            Entry(secondary, EntityState.Added),
            Entry(smart, EntityState.Modified),
            Entry(secondary, EntityState.Deleted),
            Entry(smart, EntityState.Added),
            Entry(secondary, EntityState.Modified),
            Entry(smart, EntityState.Deleted)
        };
        await Dispatch(processor, stage, entries);
        var suffix = stage == HookStage.PreSave ? "Pre" : "Post";

        Assert.Multiple(() =>
        {
            Assert.That(RecordingHook<SmartDbContext>.Snapshot(),
                Is.EquivalentTo(new[] { "RecordingHook\u00601:Modified:" + suffix, "RecordingHook\u00601:Added:" + suffix, "RecordingHook\u00601:Deleted:" + suffix }));
            Assert.That(RecordingHook<SecondaryContext>.Snapshot(),
                Is.EquivalentTo(new[] { "RecordingHook\u00601:Added:" + suffix, "RecordingHook\u00601:Deleted:" + suffix, "RecordingHook\u00601:Modified:" + suffix }));
        });
    }

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public async Task Uniform_five_entry_batch_must_still_dispatch_every_matching_hook(HookStage stage)
    {
        using var secondary = new SecondaryContext();
        var processor = Processor(Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));
        await Dispatch(processor, stage, Enumerable.Range(0, 5).Select(_ => Entry(secondary)).ToArray());

        var suffix = stage == HookStage.PreSave ? "Pre" : "Post";
        Assert.That(RecordingHook<SecondaryContext>.Snapshot(),
            Is.EquivalentTo(Enumerable.Repeat("RecordingHook\u00601:Added:" + suffix, 5)));
    }

    [TestCase(HookStage.PreSave)]
    [TestCase(HookStage.PostSave)]
    public async Task Derived_context_matches_base_context_hook_through_all_save_stages(HookStage stage)
    {
        using var derived = new DerivedSecondaryContext();
        var processor = Processor(Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));

        await Dispatch(processor, stage, new[] { Entry(derived, EntityState.Modified) });

        Assert.That(RecordingHook<SecondaryContext>.Snapshot(),
            Is.EqualTo(new[] { "RecordingHook\u00601:Modified:" + (stage == HookStage.PreSave ? "Pre" : "Post") }));
    }

    [Test]
    public async Task Canceled_processor_does_not_dispatch_hooks_on_either_context()
    {
        using var smart = Smart();
        using var secondary = new SecondaryContext();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var processor = Processor(
            Meta(typeof(RecordingHook<SmartDbContext>), typeof(SmartDbContext)),
            Meta(typeof(RecordingHook<SecondaryContext>), typeof(SecondaryContext)));
        var entries = new[] { Entry(smart), Entry(secondary) };

        await processor.SavingChangesAsync(entries, cancelToken: cancel.Token);
        await processor.SavedChangesAsync(entries, cancelToken: cancel.Token);

        Assert.Multiple(() =>
        {
            Assert.That(RecordingHook<SmartDbContext>.Snapshot(), Is.Empty);
            Assert.That(RecordingHook<SecondaryContext>.Snapshot(), Is.Empty);
        });
    }

    [Test]
    public void High_contention_parallel_reads_must_keep_context_stage_state_and_importance_isolated()
    {
        var registry = Registry(ImportanceMetadata().Concat(new[]
        {
            Meta(typeof(RecordingHook<SmartDbContext>), typeof(SmartDbContext), HookImportance.Essential)
        }).ToArray());
        var mismatches = new ConcurrentQueue<string>();
        var states = new[] { EntityState.Added, EntityState.Modified, EntityState.Deleted };
        var minima = new[] { HookImportance.Normal, HookImportance.Important, HookImportance.Essential };

        Parallel.For(0, 1200, i =>
        {
            using DbContext context = i % 3 == 0 ? Smart() :
                i % 3 == 1 ? new SecondaryContext() : new DerivedSecondaryContext();
            var state = states[i % states.Length];
            var stage = i % 2 == 0 ? HookStage.PreSave : HookStage.PostSave;
            var min = minima[(i / 3) % minima.Length];
            var selected = registry.SelectHooks(Entry(context, state), stage, min);
            var expected = context is SmartDbContext
                ? new[] { typeof(RecordingHook<SmartDbContext>) }
                : (min == HookImportance.Normal
                    ? new[] { typeof(NormalHook), typeof(ImportantHook), typeof(EssentialHook) }
                    : min == HookImportance.Important
                        ? new[] { typeof(ImportantHook), typeof(EssentialHook) }
                        : new[] { typeof(EssentialHook) });
            if (!selected.Select(x => x.ImplType).SequenceEqual(expected))
            {
                mismatches.Enqueue("Iteration " + i + " selected: " +
                    string.Join(",", selected.Select(x => x.ImplType.Name)));
            }
        });

        Assert.That(mismatches, Is.Empty);
    }

    private static SmartDbContext Smart() => new(new DbContextOptions<SmartDbContext>());
    private static HookedEntityMock Entry(DbContext context, EntityState state = EntityState.Added)
        => new(new Product(), state, context);

    private static HookMetadata Meta(Type hookType, Type contextType,
        HookImportance importance = HookImportance.Normal, int order = 0) => new()
    {
        ImplType = hookType,
        HookedType = typeof(Product),
        DbContextType = contextType,
        Importance = importance,
        Order = order
    };

    private static HookMetadata[] ImportanceMetadata() => new[]
    {
        Meta(typeof(NormalHook), typeof(SecondaryContext), HookImportance.Normal),
        Meta(typeof(ImportantHook), typeof(SecondaryContext), HookImportance.Important),
        Meta(typeof(EssentialHook), typeof(SecondaryContext), HookImportance.Essential)
    };

    private static DefaultDbHookRegistry Registry(params HookMetadata[] hooks)
        => new(hooks.Select(m => new Lazy<IDbSaveHook, HookMetadata>(
            () => (IDbSaveHook)Activator.CreateInstance(m.ImplType), m)));

    private static DefaultDbHookProcessor Processor(params HookMetadata[] hooks)
        => new(Registry(hooks), new SimpleDbHookActivator());

    private static async Task Dispatch(DefaultDbHookProcessor processor, HookStage stage,
        IHookedEntity[] entries, HookImportance minimum = HookImportance.Normal)
    {
        if (stage == HookStage.PreSave)
        {
            await processor.SavingChangesAsync(entries, minimum);
        }
        else
        {
            await processor.SavedChangesAsync(entries, minimum);
        }
    }

    public class SecondaryContext : DbContext
    {
        public SecondaryContext() : base(new DbContextOptions<SecondaryContext>()) { }
    }

    public class DerivedSecondaryContext : SecondaryContext { }

    public class RecordingHook<TContext> : DbSaveHook<TContext, Product> where TContext : DbContext
    {
        private static readonly ConcurrentQueue<string> Calls = new();

        public static void Clear()
        {
            while (Calls.TryDequeue(out _)) { }
        }

        public static string[] Snapshot() => Calls.ToArray();

        private HookResult Record(EntityState state, string stage)
        {
            Calls.Enqueue(GetType().Name + ":" + state + ":" + stage);
            return HookResult.Ok;
        }

        protected override HookResult OnInserting(Product product, IHookedEntity entry)
            => Record(EntityState.Added, "Pre");
        protected override HookResult OnUpdating(Product product, IHookedEntity entry)
            => Record(EntityState.Modified, "Pre");
        protected override HookResult OnDeleting(Product product, IHookedEntity entry)
            => Record(EntityState.Deleted, "Pre");
        protected override HookResult OnInserted(Product product, IHookedEntity entry)
            => Record(EntityState.Added, "Post");
        protected override HookResult OnUpdated(Product product, IHookedEntity entry)
            => Record(EntityState.Modified, "Post");
        protected override HookResult OnDeleted(Product product, IHookedEntity entry)
            => Record(EntityState.Deleted, "Post");
    }

    public class NormalHook : RecordingHook<SecondaryContext> { }
    public class ImportantHook : RecordingHook<SecondaryContext> { }
    public class EssentialHook : RecordingHook<SecondaryContext> { }
}
