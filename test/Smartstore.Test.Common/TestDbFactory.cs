using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data;
using Smartstore.Data.Providers;

namespace Smartstore.Test.Common
{
    public class TestDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.Unknown;

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => throw new NotImplementedException();

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userName,
            string password)
            => throw new NotImplementedException();

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new TestDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase("Test")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder
                .UseInMemoryDatabase("Test")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
    }
}
