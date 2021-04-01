using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

namespace Smartstore.Data.SqlServer
{
    internal class SqlServerDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.SqlServer;

        public override Type SmartDbContextType => typeof(SqlServerSmartDbContext);

        public override DataProvider CreateDataProvider(DatabaseFacade database)
        {
            return new SqlServerDataProvider(database);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString, IApplicationContext appContext)
        {
            //// Add-Migration Initial -Context SqlServerSmartDbContext -Project Smartstore.Data.SqlServer
            var appConfig = appContext.AppConfiguration;

            return builder.UseSqlServer(connectionString, sql =>
            {
                if (appConfig.DbCommandTimeout.HasValue)
                {
                    sql.CommandTimeout(appConfig.DbCommandTimeout.Value);
                }

                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }
    }
}
