using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data
{
    public interface IDbQueryFilters<TContext>
        where TContext: DbContext
    {
        TContext Context { get; }
    }

    public class DbQueryFilters<TContext> : IDbQueryFilters<TContext>
        where TContext : DbContext
    {
        public DbQueryFilters(TContext context)
        {
            Context = context;
        }

        public TContext Context { get; set; }
    }
}
