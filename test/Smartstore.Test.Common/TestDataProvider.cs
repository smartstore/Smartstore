using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override DbParameter CreateParameter()
            => throw new NotImplementedException();

        public override string[] GetTableNames()
            => Array.Empty<string>();

        public override Task<string[]> GetTableNamesAsync()
            => Task.FromResult(Array.Empty<string>());
    }
}
