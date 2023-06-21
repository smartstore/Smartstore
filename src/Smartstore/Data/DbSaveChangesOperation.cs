using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Data
{
    internal enum DbSaveStage
    {
        PreSave,
        PostSave
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Perf")]
    internal class DbSaveChangesOperation : IDisposable
    {
        private readonly static ConcurrentDictionary<Type, bool> _hookableEntities = new();

        private IEnumerable<EntityEntry> _changedEntries;
        private HookingDbContext _ctx;

        private readonly IDbHookProcessor _hookProcessor;
        private readonly IDbCache _dbCache;
        private readonly bool _isNestedOperation;
        private readonly HookImportance _minHookImportance;

        public DbSaveChangesOperation(HookingDbContext ctx, IDbHookProcessor hookProcessor)
        {
            _ctx = ctx;
            _hookProcessor = hookProcessor;
            _dbCache = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetService<IDbCache>();
            _minHookImportance = _ctx.MinHookImportance;
        }

        public DbSaveChangesOperation(DbSaveChangesOperation parent)
        {
            _ctx = parent._ctx;
            _hookProcessor = parent._hookProcessor;
            _dbCache = parent._dbCache;
            _isNestedOperation = true;
            _minHookImportance = HookImportance.Essential;
        }

        public DbSaveStage Stage { get; private set; }

        public IEnumerable<EntityEntry> ChangedEntries => _changedEntries;

        public int Execute(bool acceptAllChangesOnSuccess)
        {
            return ExecuteInternal(acceptAllChangesOnSuccess, false).GetAwaiter().GetResult();
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
                    if (async)
                    {
                        return await _ctx.SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancelToken);
                    }
                    else
                    {
                        return _ctx.SaveChangesCore(acceptAllChangesOnSuccess);
                    }
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
                var mergeableEntities = !_isNestedOperation 
                    ? _ctx.ChangeTracker.GetMergeableEntities().ToArray() 
                    : Array.Empty<IMergedData>();

                // Now ignore merged data, otherwise merged data will be saved to database
                IgnoreMergedData(mergeableEntities, true);

                // We must detect changes earlier in the process
                // before hooks are executed. Therefore we suppressed the
                // implicit DetectChanges() call by EF and call it here explicitly.
                _ctx.ChangeTracker.DetectChanges();

                // Now get changed entries
                _changedEntries = GetChangedEntries().ToArray();

                // pre
                var preResult = await PreExecuteAsync(cancelToken);

                return new AsyncActionDisposable(EndExecute);

                async ValueTask EndExecute()
                {
                    if (_isNestedOperation)
                    {
                        return;
                    }
                    
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
                        ClearHookData(_ctx.ChangeTracker.Entries());
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

                if (entries.Length > 0)
                {
                    // Regardless of validation (possible fixing validation errors too)
                    result = await _hookProcessor.SavingChangesAsync(entries, _minHookImportance, cancelToken);

                    if (result.ProcessedHooks.Any() && entries.Any(x => x.State == EntityState.Modified))
                    {
                        // Because at least one pre action hook has been processed,
                        // we must assume that entity properties has been changed.
                        // We need to call DetectChanges() again.
                        _ctx.ChangeTracker.DetectChanges();
                    }
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
            _dbCache?.Invalidate(changedHookEntries.Select(x => x.EntityType).ToArray());

            return await _hookProcessor.SavedChangesAsync(changedHookEntries, _minHookImportance, cancelToken);
        }

        private IEnumerable<EntityEntry> GetChangedEntries()
        {
            var entries = _ctx.ChangeTracker.Entries();
            var manyToManyMappings = (List<EntityEntry>)null;

            foreach (var entry in entries)
            {
                if (entry.State > EfState.Unchanged)
                {
                    if (entry.Entity is IDictionary<string, object>)
                    {
                        manyToManyMappings ??= new List<EntityEntry>();
                        manyToManyMappings.Add(entry);
                    }
                    else
                    {
                        yield return entry;
                    }
                }
            }

            if (manyToManyMappings != null)
            {
                var principals = new HashSet<(string, object)>();
                foreach (var mapping in manyToManyMappings)
                {
                    var principalEntry = FindPrincipalEntry(mapping, principals);
                    if (principalEntry != null)
                    {
                        yield return principalEntry;
                    }
                }
            }
        }

        private EntityEntry FindPrincipalEntry(EntityEntry entry, HashSet<(string, object)> principals)
        {
            if (entry.Metadata is IEntityType entityType)
            {
                // e.g. "PaymentMethod_Id" and "RuleSetEntity_Id"
                if (entityType.GetForeignKeys().Count() != 2)
                {
                    return null;
                }

                var entity = entry.Entity as IDictionary<string, object>;
                var kvp = entity.FirstOrDefault();
                if (kvp.Key == null || kvp.Value == null)
                {
                    return null;
                }

                // Find the foreign key for the first dict entry (which is our principal, e.g. "PaymentMethod_Id")
                //var foreignKey = entityType.GetForeignKeys().FirstOrDefault();
                var foreignKey = entityType.GetForeignKeys()
                    .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == kvp.Key));

                if (foreignKey == null)
                {
                    return null;
                }

                if (principals.Contains((kvp.Key, kvp.Value)))
                {
                    // Entry already fetched before
                    return null;
                }

                var stateManager = _ctx.GetDependencies().StateManager;
                var principalEntry = stateManager
                    .TryGetEntry(foreignKey.PrincipalKey, new object[] { kvp.Value })?
                    .ToEntityEntry();

                if (principalEntry != null)
                {
                    principals.Add((kvp.Key, kvp.Value));
                    if (principalEntry.State == EfState.Unchanged)
                    {
                        // Changed entries are already in the result set.
                        return principalEntry;
                    }
                }
            }

            return null;
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
