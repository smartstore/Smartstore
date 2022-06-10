using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data
{
    public interface IDbContextConfigurationSource<TContext>
        where TContext : DbContext
    {
        void Configure(IServiceProvider services, DbContextOptionsBuilder builder);
    }
}
