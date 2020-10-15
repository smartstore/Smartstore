using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Data.Hooks
{
    public class DbSavingChangesResult : DbSaveChangesResult
    {
        public static readonly new DbSavingChangesResult Empty = new DbSavingChangesResult(Enumerable.Empty<IDbSaveHook>(), false);

        public DbSavingChangesResult(IEnumerable<IDbSaveHook> processedHooks, bool anyStateChanged)
            : base(processedHooks)
        {
            AnyStateChanged = anyStateChanged;
        }

        public IEnumerable<IHookedEntity> Entries { get; set; } = Enumerable.Empty<IHookedEntity>();
        public bool AnyStateChanged { get; }
    }

    public class DbSaveChangesResult
    {
        public static readonly DbSaveChangesResult Empty = new DbSaveChangesResult(Enumerable.Empty<IDbSaveHook>());

        public DbSaveChangesResult(IEnumerable<IDbSaveHook> processedHooks)
        {
            ProcessedHooks = processedHooks;
        }

        public IEnumerable<IDbSaveHook> ProcessedHooks { get; }
    }
    
    /// <summary>
    /// Responsible for executing all discovered hooks on data commit.
    /// </summary>
    public interface IDbHookHandler
    {
        bool HasImportantSaveHooks();

        /// <summary>
        /// Triggers all pre action hooks
        /// </summary>
        /// <param name="entries">Entries</param>
        /// <param name="importantHooksOnly">Whether to trigger only hooks marked with the <see cref="ImportantAttribute"/> attribute</param>
        /// <returns>The list of actually processed hook instances and a value indicating whether the state of at least one entity has changed.</returns>
        Task<DbSavingChangesResult> SavingChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken);

        /// <summary>
        /// Triggers all post action hooks
        /// </summary>
        /// <param name="entries">Entries</param>
        /// <param name="importantHooksOnly">Whether to trigger only hooks marked with the <see cref="ImportantAttribute"/> attribute</param>
        /// <returns>The list of actually processed hook instances</returns>
        Task<DbSaveChangesResult> SavedChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken);
    }

    public sealed class NullDbHookHandler : IDbHookHandler
    {
        public static IDbHookHandler Instance { get; } = new NullDbHookHandler();

        public bool HasImportantSaveHooks() => false;

        public Task<DbSavingChangesResult> SavingChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken)
        {
            return Task.FromResult(DbSavingChangesResult.Empty);
        }

        public Task<DbSaveChangesResult> SavedChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken)
        {
            return Task.FromResult(DbSaveChangesResult.Empty);
        }
    }
}
