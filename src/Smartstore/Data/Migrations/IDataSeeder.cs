using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Migrations
{
    /// <summary>
    /// Specifies the data seeder execution stages.
    /// </summary>
    public enum DataSeederStage
    {
        /// <summary>
        /// The seeder should run early during app startup
        /// </summary>
        Early,

        /// <summary>
        /// The seeder should run late (after app start, during the very first request).
        /// Always define <c>Late</c> for potentially long running seeders 
        /// to avoid a possible timeout during the app start.
        /// </summary>
        Late
    }
    
    /// <summary>
    /// Data seeder interface. This interface is usually applied to auto-generated migration classes.
    /// </summary>
    /// <typeparam name="TContext">Concrete type of <see cref="DbContext"/> that the seeder can provide data to.</typeparam>
    public interface IDataSeeder<in TContext> where TContext : HookingDbContext
    {
        /// <summary>
        /// Seeds data
        /// </summary>
        Task SeedAsync(TContext context, CancellationToken cancelToken = default);

        /// <summary>
        /// Defines the stage in which the seeder is to be executed.
        /// </summary>
        DataSeederStage Stage { get; }

        /// <summary>
        /// Any unhandled exception should rollback the corresponding database migration (if in early stage)
        /// or raise the exception (if in late stage).
        /// </summary>
        bool AbortOnFailure { get; }
    }
}
