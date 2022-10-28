using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Utilities;
using EfState = Microsoft.EntityFrameworkCore.EntityState;

namespace Smartstore.Data
{
    internal enum DbSaveStage
    {
        PreSave,
        PostSave
    }

    internal class DbSaveChangesOperation : IDisposable
    {
        private readonly static ConcurrentDictionary<Type, bool> _hookableEntities = new();

        private IEnumerable<EntityEntry> _changedEntries;
        private HookingDbContext _ctx;
        private readonly IDbCache _dbCache;

        public DbSaveChangesOperation(HookingDbContext ctx)
        {
            _ctx = ctx;
            _dbCache = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetService<IDbCache>();
        }

        public DbSaveStage Stage { get; private set; }

        public IEnumerable<EntityEntry> ChangedEntries => _changedEntries;

        public int Execute(bool acceptAllChangesOnSuccess)
        {
            return ExecuteInternal(acceptAllChangesOnSuccess, false).Await();
        }

        public Task<int> ExecuteAsync(bool acceptAllChangesOnSuccess, CancellationToken cancelToken)
        {
            return ExecuteInternal(acceptAllChangesOnSuccess, true, cancelToken);
        }

        public async Task<int> ExecuteInternal(bool acceptAllChangesOnSuccess, bool async, CancellationToken cancelToken = default)
        {
            Exception exception = null;

            await using (await DoExecute())
            {
                try
                {
                    return async
                        ? await _ctx.SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancelToken)
                        : _ctx.SaveChangesCore(acceptAllChangesOnSuccess);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
            }

            async Task<IAsyncDisposable> DoExecute()
            {
                var autoDetectChanges = _ctx.ChangeTracker.AutoDetectChangesEnabled;

                // Suppress implicit DetectChanges() calls by EF,
                // e.g. called by SaveChanges(), ChangeTracker.Entries() etc.
                _ctx.ChangeTracker.AutoDetectChangesEnabled = false;

                // Get all attached entries implementing IMergedData,
                // we need to ignore merge on them. Otherwise
                // EF's change detection may think that properties has changed
                // where they actually didn't.
                var mergeableEntities = _ctx.ChangeTracker.GetMergeableEntities().ToArray();

                // Now ignore merged data, otherwise merged data will be saved to database
                IgnoreMergedData(mergeableEntities, true);

                // We must detect changes earlier in the process
                // before hooks are executed. Therefore we suppressed the
                // implicit DetectChanges() call by EF and call it here explicitly.
                _ctx.ChangeTracker.DetectChanges();

                // Now get changed entries
                _changedEntries = GetChangedEntries();

                // pre
                var preResult = await PreExecuteAsync(cancelToken);

                return new AsyncActionDisposable(EndExecute);

                async ValueTask EndExecute()
                {
                    try
                    {
                        // Post
                        if (exception == null)
                        {
                            // Post execute only on successful commit
                            await PostExecuteAsync(preResult.Entries, cancelToken);
                        }
                    }
                    finally
                    {
                        _ctx.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;

                        IgnoreMergedData(mergeableEntities, false);
                        ClearHookData(_changedEntries);
                    }
                }
            }
        }

        private async Task<DbSavingChangesResult> PreExecuteAsync(CancellationToken cancelToken)
        {
            var result = DbSavingChangesResult.Empty;

            // hooking is meaningless without hookable entries
            var enableHooks = _changedEntries.Any();

            if (enableHooks)
            {
                var contextType = _ctx.GetType();

                var entries = _changedEntries
                    .Where(x => IsHookableEntityType(x.Entity))
                    .Select(x => new HookedEntity(x))
                    .ToArray();

                // Regardless of validation (possible fixing validation errors too)
                result = await _ctx.DbHookHandler.SavingChangesAsync(entries, _ctx.MinHookImportance, cancelToken);

                if (result.ProcessedHooks.Any() && entries.Any(x => x.State == EntityState.Modified))
                {
                    // Because at least one pre action hook has been processed,
                    // we must assume that entity properties has been changed.
                    // We need to call DetectChanges() again.
                    _ctx.ChangeTracker.DetectChanges();
                }
            }

            if (result.AnyStateChanged)
            {
                // because the state of at least one entity has been changed during pre hooking
                // we have to further reduce the set of hookable entities (for the POST hooks)
                result.Entries = result.Entries.Where(x => x.InitialState > EntityState.Unchanged).ToArray();
            }

            return result;
        }

        private async Task<DbSaveChangesResult> PostExecuteAsync(IHookedEntity[] changedHookEntries, CancellationToken cancelToken)
        {
            if (changedHookEntries == null || changedHookEntries.Length == 0)
            {
                return DbSaveChangesResult.Empty;
            }

            // The existence of hook entries actually implies that hooking is enabled.

            Stage = DbSaveStage.PostSave;

            // EfCache invalidation
            if (_dbCache != null)
            {
                _dbCache.Invalidate(changedHookEntries.Select(x => x.EntityType).ToArray());
            }

            return await _ctx.DbHookHandler.SavedChangesAsync(changedHookEntries, _ctx.MinHookImportance, cancelToken);
        }

        private IEnumerable<EntityEntry> GetChangedEntries()
        {
            return _ctx.ChangeTracker.Entries().Where(x => x.State > EfState.Unchanged);
        }

        private static void IgnoreMergedData(IMergedData[] entries, bool ignore)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i].MergedDataIgnore = ignore;
            }
        }

        private static void ClearHookData(IEnumerable<EntityEntry> entries)
        {
            foreach (var entity in entries.Select(x => x.Entity).OfType<BaseEntity>())
            {
                entity.ClearHookState();
            }
        }

        private static bool IsHookableEntityType(object instance)
        {
            // Property bags and intermediate entities (do not inherit from BaseEntity) are not hookable.
            if (instance is not BaseEntity)
            {
                return false;
            }

            var isHookable = _hookableEntities.GetOrAdd(instance.GetType(), t =>
            {
                var attr = t.GetAttribute<HookableAttribute>(true);
                if (attr != null)
                {
                    return attr.IsHookable;
                }

                // Entities are hookable by default
                return true;
            });

            return isHookable;
        }

        public void Dispose()
        {
            _ctx = null;
            _changedEntries = null;
        }
    }
}
