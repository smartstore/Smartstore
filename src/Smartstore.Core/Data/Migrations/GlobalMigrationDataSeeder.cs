using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Data.Migrations
{
    internal class GlobalMigrationDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public Task SeedAsync(SmartDbContext context)
        {
            return Task.CompletedTask;
        }
    }
}
