using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Core.Data.Setup
{
    public interface IDataSeeder<TContext> where TContext : DbContext
    {
        /// <summary>
        /// Seeds data
        /// </summary>
        Task SeedAsync(TContext context);

        /// <summary>
        /// Gets a value indicating whether migration should be completely rolled back
        /// when an error occurs during migration seeding.
        /// </summary>
        bool RollbackOnFailure { get; }
    }
}
