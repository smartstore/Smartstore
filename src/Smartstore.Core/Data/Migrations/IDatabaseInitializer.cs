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
        /// Initializes / migrates the given <paramref name="dbContextType"/>.
        /// </summary>
        Task InitializeDatabaseAsync(Type dbContextType, CancellationToken cancelToken);
    }

    public static class IDatabaseInitializerExtensions
    {
        /// <summary>
        /// Initializes / migrates the given <typeparamref name="TContext"/> type.
        /// </summary>
        public static Task InitializeDatabaseAsync<TContext>(this IDatabaseInitializer initializer, CancellationToken cancelToken = default)
            => initializer.InitializeDatabaseAsync(typeof(TContext), cancelToken);
    }
}
