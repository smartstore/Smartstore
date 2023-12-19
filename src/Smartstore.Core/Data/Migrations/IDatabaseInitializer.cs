namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Responsible for initializing all migratable <see cref="DbContext"/> instances on app startup.
    /// </summary>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initializes / migrates all discovered migratable <see cref="DbContext"/> instances.
        /// </summary>
        Task InitializeDatabasesAsync(CancellationToken cancelToken);

        /// <summary>
        /// Runs any pending (long running) data seeders that have not been run at app start.
        /// </summary>
        Task RunPendingSeedersAsync(CancellationToken cancelToken);
    }
}
