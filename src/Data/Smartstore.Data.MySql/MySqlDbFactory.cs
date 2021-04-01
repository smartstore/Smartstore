using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

namespace Smartstore.Data.SqlServer
{
    internal class MySqlDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.MySql;

        public override Type SmartDbContextType => typeof(MySqlSmartDbContext);

        public override DataProvider CreateDataProvider(DatabaseFacade database)
        {
            return new MySqlDataProvider(database);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString, IApplicationContext appContext)
        {
            var appConfig = appContext.AppConfiguration;

            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySql =>
            {
                if (appConfig.DbCommandTimeout.HasValue)
                {
                    mySql.CommandTimeout(appConfig.DbCommandTimeout.Value);
                }

                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }
    }
}
