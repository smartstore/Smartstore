using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

namespace Smartstore.Data.SqlServer
{
    internal class SqlServerDbFactory : IDbFactory
    {
        public DbSystemType DbSystem { get; } = DbSystemType.SqlServer;
        
        public DataProvider CreateDataProvider(DatabaseFacade database)
        {
            return new SqlServerDataProvider(database);
        }

        public DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString, IApplicationContext appContext)
        {
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
