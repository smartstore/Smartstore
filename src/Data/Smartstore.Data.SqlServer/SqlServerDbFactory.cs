using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

// Add-Migration Initial -Context SqlServerSmartDbContext -Project Smartstore.Data.SqlServer

namespace Smartstore.Data.SqlServer
{
    // TODO: (core) Find a way to deploy provider projects unreferenced.
    
    internal class SqlServerDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.SqlServer;

        public override Type SmartDbContextType => typeof(SqlServerSmartDbContext);

        public override DataProvider CreateDataProvider(DatabaseFacade database)
        {
            return new SqlServerDataProvider(database);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(
            DbContextOptionsBuilder builder, 
            string connectionString, 
            IApplicationContext appContext)
        {
            return builder.UseSqlServer(connectionString, sql =>
            {
                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }
    }
}
