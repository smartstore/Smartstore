using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

// Add-Migration Initial -Context MySqlSmartDbContext -Project Smartstore.Data.MySql

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
            //// Add-Migration Initial -Context MySqlSmartDbContext -Project Smartstore.Data.MySql
            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySql =>
            {
                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }
    }
}
