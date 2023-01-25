using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data;
using Smartstore.Data.Providers;

namespace Smartstore.Test.Common
{
    public class TestDataProvider : DataProvider
    {
        public TestDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        public override DbSystemType ProviderType
            => DbSystemType.Unknown;

        public override bool MARSEnabled
            => true;

        public override string ApplyPaging(string sql, int skip, int take) 
            => sql;

        public override DbParameter CreateParameter() 
            => throw new NotImplementedException();

        public override string EncloseIdentifier(string identifier)
            => identifier;

        public override bool IsTransientException(Exception ex)
            => false;

        public override bool IsUniquenessViolationException(DbUpdateException ex)
            => false;

        protected override ValueTask<string[]> GetTableNamesCore(bool async)
            => ValueTask.FromResult(Array.Empty<string>());

        protected override ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async)
            => ValueTask.FromResult(false);

        protected override ValueTask<bool> HasDatabaseCore(string databaseName, bool async)
            => ValueTask.FromResult(false);

        protected override ValueTask<bool> HasTableCore(string tableName, bool async)
            => ValueTask.FromResult(false);

        protected override Task<int> InsertIntoCore(string sql, bool async, params object[] parameters)
            => Task.FromResult(0);

        protected override Task<int> TruncateTableCore(string tableName, bool async)
            => Task.FromResult(0);
    }
}
