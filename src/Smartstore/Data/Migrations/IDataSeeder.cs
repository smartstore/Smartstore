using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Migrations
{
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
        /// Gets a value indicating whether migration should be completely rolled back
        /// when an error occurs during migration seeding.
        /// </summary>
        bool RollbackOnFailure { get; }
    }
}
