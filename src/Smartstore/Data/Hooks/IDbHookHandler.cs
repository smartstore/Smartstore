namespace Smartstore.Data.Hooks
{
    public class DbSavingChangesResult : DbSaveChangesResult
    {
        public static readonly new DbSavingChangesResult Empty = new(Array.Empty<IDbSaveHook>(), false);

        public DbSavingChangesResult(IEnumerable<IDbSaveHook> processedHooks, bool anyStateChanged)
            : base(processedHooks)
        {
            AnyStateChanged = anyStateChanged;
        }

        public IHookedEntity[] Entries { get; set; } = Array.Empty<IHookedEntity>();
        public bool AnyStateChanged { get; }
    }

    public class DbSaveChangesResult
    {
        public static readonly DbSaveChangesResult Empty = new(Array.Empty<IDbSaveHook>());

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
        /// <summary>
        /// Triggers all pre action hooks
        /// </summary>
        /// <param name="entries">Entries</param>
        /// <param name="minHookImportance">
        /// The minimum importance level of executable hooks. Only hooks with level equal or higher than the given value will be executed.
        /// </param>
        /// <returns>The list of actually processed hook instances and a value indicating whether the state of at least one entity has changed.</returns>
        Task<DbSavingChangesResult> SavingChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Triggers all post action hooks
        /// </summary>
        /// <param name="entries">Entries</param>
        /// <param name="minHookImportance">
        /// The minimum importance level of executable hooks. Only hooks with level equal or higher than the given value will be executed.
        /// </param>
        /// <returns>The list of actually processed hook instances</returns>
        Task<DbSaveChangesResult> SavedChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default);
    }

    public sealed class NullDbHookHandler : IDbHookHandler
    {
        public static IDbHookHandler Instance { get; } = new NullDbHookHandler();

        public Task<DbSavingChangesResult> SavingChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            return Task.FromResult(DbSavingChangesResult.Empty);
        }

        public Task<DbSaveChangesResult> SavedChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            return Task.FromResult(DbSaveChangesResult.Empty);
        }
    }
}
